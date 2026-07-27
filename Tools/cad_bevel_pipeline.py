#!/usr/bin/env python3
"""Create render-only beveled FBX meshes from visual STL references in a URDF.

Run with Blender, not regular Python:
  blender --background --python Tools/cad_bevel_pipeline.py -- \
    --urdf Assets/PM_Robot/pm_robot_unity.urdf \
    --output Assets/PM_Robot/RenderOptimized/FBX \
    --profile core

The STL sources are never modified. Collision geometry keeps referencing STL.
"""

import argparse
import math
import sys
from pathlib import Path
from xml.etree import ElementTree

import bmesh
import bpy


CORE_NAMES = {
    "housing", "housing_hl", "housing_hr", "housing_vl", "housing_vr",
    "pm_robot_base_link", "axis_base", "X_Axis", "Y_Axis", "Z_Axis",
    "t_axis_tool", "Gripper_Rot_Plate", "Camera_Station",
    "Gonio_Left_Base", "Gonio_Base_Right", "Smarpod_Station",
    "Smarpod_Top_Plate", "1K_Dispenser", "2K_Glue_Cartridge",
}


def arguments():
    raw = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--urdf", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--profile", choices=("core", "all"), default="core")
    return parser.parse_args(raw)


def visual_stls(urdf_path, profile):
    root = ElementTree.parse(urdf_path).getroot()
    result = []
    for visual in root.findall(".//visual"):
        mesh = visual.find("./geometry/mesh")
        if mesh is None:
            continue
        relative = Path(mesh.attrib.get("filename", ""))
        if relative.suffix.lower() != ".stl" or "_coll" in relative.stem.lower():
            continue
        if profile == "core" and relative.stem not in CORE_NAMES:
            continue
        source = urdf_path.parent / relative
        if source.exists():
            result.append((relative, source))
    return list(dict.fromkeys(result))


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def clean_mesh(obj):
    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    bmesh.ops.dissolve_degenerate(bm, dist=1e-9, edges=bm.edges)

    seen = set()
    duplicate_faces = []
    for face in bm.faces:
        key = tuple(sorted(vertex.index for vertex in face.verts))
        if key in seen:
            duplicate_faces.append(face)
        else:
            seen.add(key)
    if duplicate_faces:
        bmesh.ops.delete(bm, geom=duplicate_faces, context="FACES")

    # CAD tessellators commonly split perfectly planar patches into hundreds
    # of triangles. Remove only near-coplanar internal edges; curved profiles
    # and manufactured breaks remain because their face angle exceeds 0.15°.
    bmesh.ops.dissolve_limit(
        bm,
        angle_limit=math.radians(0.15),
        use_dissolve_boundaries=False,
        verts=bm.verts,
        edges=bm.edges,
        delimit={"MATERIAL", "SEAM", "SHARP"},
    )
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()


def dimensions(obj):
    return sorted(value for value in obj.dimensions if value > 1e-8)


def bevel_width(relative, obj):
    dims = dimensions(obj)
    minimum = dims[0] if dims else 0.01
    is_housing = "housing" in relative.stem.lower()
    if is_housing:
        return min(0.0015, max(0.00025, minimum * 0.008)), 2
    return min(0.00035, max(0.00004, minimum * 0.004)), 1


def add_bevel_and_hard_edges(obj, relative):
    width, segments = bevel_width(relative, obj)
    bevel = obj.modifiers.new("Manufactured edge bevel", "BEVEL")
    bevel.width = width
    bevel.segments = segments
    bevel.limit_method = "ANGLE"
    bevel.angle_limit = math.radians(28.0)
    bevel.affect = "EDGES"
    bevel.use_clamp_overlap = True
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)

    for polygon in obj.data.polygons:
        polygon.use_smooth = True

    # Split only genuine manufactured breaks. Curved tessellation remains smooth.
    edge_split = obj.modifiers.new("Machined hard edges", "EDGE_SPLIT")
    edge_split.split_angle = math.radians(48.0)
    edge_split.use_edge_angle = True
    bpy.ops.object.modifier_apply(modifier=edge_split.name)


def add_box_uv(obj):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    size = max(max(obj.dimensions), 0.001)
    bpy.ops.uv.cube_project(cube_size=max(size * 0.15, 0.001), correct_aspect=True)
    bpy.ops.object.mode_set(mode="OBJECT")


def triangulate_for_unity(obj):
    # Bevel can leave perfectly renderable Blender n-gons that Unity's FBX
    # importer classifies as self-intersecting. Freeze an explicit triangle
    # topology before export so no importer has to guess the diagonal.
    triangulate = obj.modifiers.new("Unity-safe triangulation", "TRIANGULATE")
    triangulate.quad_method = "BEAUTY"
    triangulate.ngon_method = "BEAUTY"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=triangulate.name)


def process(relative, source, output_root):
    reset_scene()
    bpy.ops.wm.stl_import(filepath=str(source.resolve()))
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh imported from {source}")

    bpy.context.view_layer.objects.active = meshes[0]
    for obj in meshes:
        obj.select_set(True)
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    obj.name = relative.stem
    obj.data.name = relative.stem

    clean_mesh(obj)
    add_bevel_and_hard_edges(obj, relative)
    add_box_uv(obj)
    triangulate_for_unity(obj)

    destination = output_root / relative.with_suffix(".fbx")
    destination.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(destination.resolve()),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=False,
        bake_space_transform=False,
        add_leaf_bones=False,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        path_mode="AUTO",
        axis_forward="-Z",
        axis_up="Y",
    )
    print(f"CAD_REALISM {relative} -> {destination} bevel={bevel_width(relative, obj)[0]:.6f}m")


def main():
    args = arguments()
    args.urdf = args.urdf.resolve()
    args.output = args.output.resolve()
    jobs = visual_stls(args.urdf, args.profile)
    print(f"CAD_REALISM processing {len(jobs)} visual STL files ({args.profile})")
    failures = []
    for relative, source in jobs:
        try:
            process(relative, source, args.output)
        except Exception as error:
            failures.append((relative, error))
            print(f"CAD_REALISM ERROR {relative}: {error}", file=sys.stderr)
    if failures:
        raise RuntimeError(f"{len(failures)} meshes failed: {failures}")


if __name__ == "__main__":
    main()
