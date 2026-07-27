# CAD geometry and PBR workflow

The robot source geometry is STL. STL stores triangles only: it has no UVs,
smoothing groups, tangents, material slots, or reliable distinction between a
render edge and a collision edge. The realism workflow therefore keeps source
STLs unchanged and generates render-only derivatives.

## One-time setup

Build the material library from Unity:

`CAD Realism > Build or refresh PBR material library`

This creates painted metal, bare aluminum, stainless steel, black plastic,
rubber, glass, cable, and grease/worn-contact materials. It also upgrades the
existing URDF color materials to physically plausible values. Paint is a
dielectric (`Metallic = 0`); grey/silver CAD materials are treated as bare
metal. Fine detail textures vary roughness and micro-normal response without
turning the robot into a scratched prop.

## True render bevels

From the project root:

```bash
blender --background --python Tools/cad_bevel_pipeline.py -- \
  --urdf Assets/PM_Robot/pm_robot_unity.urdf \
  --output Assets/PM_Robot/RenderOptimized/FBX \
  --profile core
```

Use `--profile all` after visually reviewing the core set. The script:

- welds coincident vertices;
- removes degenerate and duplicate faces;
- recalculates face orientation;
- dissolves near-coplanar CAD tessellation that cannot change the silhouette;
- applies a 0.25–1.5 mm, two-segment housing bevel;
- applies a 0.04–0.35 mm, one-segment machined-part bevel;
- smooths curved tessellation and splits edges over 48 degrees;
- generates box UVs for the UV-less STL geometry;
- exports FBX without changing source or collision STL files.

Back in Unity, run:

`CAD Realism > Apply generated FBX meshes to main scene`

Only descendants of URDF `Visuals` objects are replaced. `Collisions`,
`MeshCollider` geometry, articulation bodies, transforms, and source STL
prefabs are not modified.

The repository also includes `Assets/main_scene_cad_realism.unity`, generated
from the current main scene with the reviewed core FBX set already applied. It
is kept separate so an open or locally modified `main_scene.unity` is never
overwritten by the batch workflow.

## Individual/problem meshes

Select a visual hierarchy and use:

- `Audit selected render hierarchy` to report degenerate/duplicate faces and
  missing UV/tangent data.
- `Optimize selected visual meshes` for an in-Unity render derivative with
  angle-weighted normals, hard-edge preservation, box UVs, and tangents.

The in-Unity pass improves shading but does not change the silhouette; use the
Blender pass when an edge needs a real highlight band.

## Review checklist

- Inspect bevels from the normal production camera, not an extreme close-up.
- Keep housings softer than precision jaws, chucks, and alignment features.
- Do not smooth across seams between separately machined components.
- Use glass only on actual lenses/windows; transparent paint is non-physical.
- Treat AO as geometry-specific. The generic fine occlusion supplied here is
  deliberately weak and is used only for molded/rubber microtexture.
- Remove hidden internal components in the source CAD when ownership and
  serviceability are known; automatic deletion cannot reliably distinguish a
  hidden fastener from a temporarily visible service part.
