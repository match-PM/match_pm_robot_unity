using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Tools for turning UV-less, triangle-soup robot CAD into render meshes and
/// physically plausible materials without changing the collision meshes.
/// </summary>
public sealed class CadRealismWindow : EditorWindow
{
    private float smoothingAngle = 48f;
    private float textureScale = 80f;

    [MenuItem("CAD Realism/Geometry and PBR tools")]
    private static void Open()
    {
        GetWindow<CadRealismWindow>("CAD Realism");
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "STL has no UVs, smoothing groups, tangents, or material slots. " +
            "These tools create render-only derivatives; source and collision geometry stay unchanged.",
            MessageType.Info);

        smoothingAngle = EditorGUILayout.Slider("Smooth angle", smoothingAngle, 10f, 80f);
        textureScale = EditorGUILayout.FloatField("Box UV scale (per metre)", textureScale);

        if (GUILayout.Button("Build / refresh PBR material library"))
            CadRealismPipeline.BuildProjectMaterials();

        using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
        {
            if (GUILayout.Button("Audit selected render hierarchy"))
                CadRealismPipeline.AuditSelection();

            if (GUILayout.Button("Optimize selected visual meshes"))
                CadRealismPipeline.OptimizeSelection(smoothingAngle, textureScale);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "For true silhouettes and highlight bands, run Tools/cad_bevel_pipeline.py through Blender, " +
            "then use “Apply generated FBX meshes to main scene”.",
            MessageType.None);

        if (GUILayout.Button("Apply generated FBX meshes to main scene"))
            CadRealismPipeline.ApplyOptimizedMeshesToMainScene();
    }
}

public static class CadRealismPipeline
{
    private const string MaterialRoot = "Assets/PM_Robot/Materials/PBR";
    private const string TextureRoot = MaterialRoot + "/Textures";
    private const string DerivedMeshRoot = "Assets/PM_Robot/RenderOptimized/Derived";
    private const string BlenderMeshRoot = "Assets/PM_Robot/RenderOptimized/FBX";
    private const string MainScene = "Assets/main_scene.unity";

    private sealed class MaterialSpec
    {
        public string name;
        public Color color;
        public float metallic;
        public float smoothness;
        public float variation;
        public float normalStrength;
        public bool useMicroAo;
        public bool transparent;
    }

    [MenuItem("CAD Realism/Build or refresh PBR material library")]
    public static void BuildProjectMaterials()
    {
        EnsureFolder(MaterialRoot);
        EnsureFolder(TextureRoot);

        Texture2D detail = CreateDetailAlbedo(TextureRoot + "/FineRoughnessVariation.asset");
        Texture2D normal = CreateDetailNormal(TextureRoot + "/FineSurfaceNormal.asset");
        Texture2D brushedNormal = CreateBrushedNormal(TextureRoot + "/BrushedMetalNormal.asset");
        Texture2D ao = CreateMicroOcclusion(TextureRoot + "/FineSurfaceOcclusion.asset");
        Texture2D packedDielectric = CreatePackedSurface(
            TextureRoot + "/FineDielectricSmoothness.asset", 0f, 0.10f, false);
        Texture2D packedMetal = CreatePackedSurface(
            TextureRoot + "/FineMetalSmoothness.asset", 0.94f, 0.10f, true);
        Texture2D packedWorn = CreatePackedSurface(
            TextureRoot + "/WornContactSmoothness.asset", 0.34f, 0.25f, false);

        var specs = new[]
        {
            new MaterialSpec { name = "Painted Metal", color = Rgb(0.72f, 0.16f, 0.035f), metallic = 0f, smoothness = 0.58f, variation = 0.10f, normalStrength = 0.16f },
            new MaterialSpec { name = "Bare Aluminum", color = Rgb(0.68f, 0.70f, 0.72f), metallic = 0.92f, smoothness = 0.62f, variation = 0.12f, normalStrength = 0.12f },
            new MaterialSpec { name = "Stainless Steel", color = Rgb(0.56f, 0.58f, 0.60f), metallic = 0.96f, smoothness = 0.72f, variation = 0.09f, normalStrength = 0.08f },
            new MaterialSpec { name = "Black Plastic", color = Rgb(0.018f, 0.022f, 0.025f), metallic = 0f, smoothness = 0.36f, variation = 0.13f, normalStrength = 0.24f, useMicroAo = true },
            new MaterialSpec { name = "Rubber", color = Rgb(0.012f, 0.014f, 0.016f), metallic = 0f, smoothness = 0.16f, variation = 0.16f, normalStrength = 0.32f, useMicroAo = true },
            new MaterialSpec { name = "Glass", color = new Color(0.70f, 0.84f, 0.90f, 0.24f), metallic = 0f, smoothness = 0.94f, variation = 0.025f, normalStrength = 0.025f, transparent = true },
            new MaterialSpec { name = "Cables", color = Rgb(0.014f, 0.016f, 0.019f), metallic = 0f, smoothness = 0.28f, variation = 0.12f, normalStrength = 0.26f, useMicroAo = true },
            new MaterialSpec { name = "Grease Worn Contact", color = Rgb(0.12f, 0.105f, 0.085f), metallic = 0.34f, smoothness = 0.44f, variation = 0.24f, normalStrength = 0.18f, useMicroAo = true },
        };

        foreach (MaterialSpec spec in specs)
            CreateOrUpdateMaterial(
                MaterialRoot + "/" + spec.name + ".mat", spec, detail, normal, brushedNormal, ao,
                packedDielectric, packedMetal, packedWorn);

        UpgradeCadPalette(
            detail, normal, brushedNormal, ao, packedDielectric, packedMetal, packedWorn);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CAD Realism: PBR library created and the robot CAD palette upgraded.");
    }

    [MenuItem("CAD Realism/Audit selected render hierarchy")]
    public static void AuditSelection()
    {
        var meshes = Selection.gameObjects
            .SelectMany(go => go.GetComponentsInChildren<MeshFilter>(true))
            .Where(mf => mf.sharedMesh != null && !IsUnderCollision(mf.transform))
            .Select(mf => mf.sharedMesh)
            .Distinct()
            .ToArray();

        var report = new StringBuilder();
        report.AppendLine("# CAD mesh audit");
        report.AppendLine();
        report.AppendLine("| Mesh | Vertices | Triangles | Degenerate | Duplicate | Missing UV | Missing tangents |");
        report.AppendLine("|---|---:|---:|---:|---:|:---:|:---:|");

        long vertices = 0;
        long triangles = 0;
        long degenerate = 0;
        long duplicates = 0;

        foreach (Mesh mesh in meshes)
        {
            MeshAudit result = Audit(mesh);
            vertices += mesh.vertexCount;
            triangles += result.triangles;
            degenerate += result.degenerate;
            duplicates += result.duplicate;
            report.AppendLine(
                $"| {mesh.name} | {mesh.vertexCount} | {result.triangles} | {result.degenerate} | " +
                $"{result.duplicate} | {(mesh.uv == null || mesh.uv.Length != mesh.vertexCount ? "yes" : "no")} | " +
                $"{(mesh.tangents == null || mesh.tangents.Length != mesh.vertexCount ? "yes" : "no")} |");
        }

        report.AppendLine();
        report.AppendLine($"Audited {meshes.Length} render meshes: {vertices:N0} vertices, {triangles:N0} triangles, " +
                          $"{degenerate:N0} degenerate and {duplicates:N0} duplicate faces.");

        string reportPath = "Assets/PM_Robot/CAD_MESH_AUDIT.md";
        File.WriteAllText(Path.GetFullPath(reportPath), report.ToString());
        AssetDatabase.ImportAsset(reportPath);
        Debug.Log($"CAD Realism: audit written to {reportPath}");
    }

    public static void OptimizeSelection(float smoothingAngle = 48f, float textureScale = 80f)
    {
        EnsureFolder("Assets/PM_Robot/RenderOptimized");
        EnsureFolder(DerivedMeshRoot);

        var cache = new Dictionary<Mesh, Mesh>();
        int rendererCount = 0;
        foreach (GameObject selected in Selection.gameObjects)
        {
            foreach (MeshFilter filter in selected.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null || IsUnderCollision(filter.transform))
                    continue;

                Mesh optimized;
                if (!cache.TryGetValue(filter.sharedMesh, out optimized))
                {
                    optimized = CreateOptimizedMesh(filter.sharedMesh, smoothingAngle, textureScale);
                    cache.Add(filter.sharedMesh, optimized);
                }

                Undo.RecordObject(filter, "Use optimized CAD render mesh");
                filter.sharedMesh = optimized;
                EditorUtility.SetDirty(filter);
                rendererCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"CAD Realism: optimized {cache.Count} meshes on {rendererCount} renderers. Collision meshes were skipped.");
    }

    [MenuItem("CAD Realism/Apply generated FBX meshes to main scene")]
    public static void ApplyOptimizedMeshesToMainScene()
    {
        if (!AssetDatabase.IsValidFolder(BlenderMeshRoot))
        {
            Debug.LogError($"CAD Realism: no Blender output found at {BlenderMeshRoot}");
            return;
        }

        var generated = new Dictionary<string, Mesh>(StringComparer.OrdinalIgnoreCase);
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { BlenderMeshRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
                generated[NormalizeMeshName(mesh.name)] = mesh;
        }

        var scene = EditorSceneManager.OpenScene(MainScene, OpenSceneMode.Single);
        int replaced = 0;
        int hiddenChunks = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform visual in root.GetComponentsInChildren<Transform>(true)
                         .Where(t => t.name == "Visuals"))
            {
                MeshFilter[] filters = visual.GetComponentsInChildren<MeshFilter>(true)
                    .Where(mf => mf.sharedMesh != null)
                    .ToArray();
                if (filters.Length == 0)
                    continue;

                string key = NormalizeMeshName(filters[0].sharedMesh.name);
                Mesh replacement;
                if (!generated.TryGetValue(key, out replacement))
                    continue;

                Undo.RecordObject(filters[0], "Use beveled CAD render mesh");
                filters[0].sharedMesh = replacement;
                EditorUtility.SetDirty(filters[0]);
                replaced++;

                for (int i = 1; i < filters.Length; i++)
                {
                    Renderer renderer = filters[i].GetComponent<Renderer>();
                    if (renderer == null || !renderer.enabled)
                        continue;
                    Undo.RecordObject(renderer, "Hide superseded STL mesh chunk");
                    renderer.enabled = false;
                    EditorUtility.SetDirty(renderer);
                    hiddenChunks++;
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"CAD Realism: applied {replaced} beveled FBX render meshes and hid {hiddenChunks} superseded STL chunks.");
    }

    [MenuItem("CAD Realism/Validate generated FBX meshes")]
    public static void ValidateGeneratedMeshes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Mesh", new[] { BlenderMeshRoot });
        var paths = new HashSet<string>();
        long vertices = 0;
        long triangles = 0;
        var issues = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            paths.Add(path);
            foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
            {
                vertices += mesh.vertexCount;
                for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                    triangles += (long)mesh.GetIndexCount(subMesh) / 3L;

                if (!mesh.HasVertexAttribute(VertexAttribute.Normal))
                    issues.Add(mesh.name + ": missing normals");
                if (!mesh.HasVertexAttribute(VertexAttribute.Tangent))
                    issues.Add(mesh.name + ": missing tangents");
                if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0))
                    issues.Add(mesh.name + ": missing UV0");
                if (mesh.bounds.size.sqrMagnitude <= 1e-12f)
                    issues.Add(mesh.name + ": empty bounds");
            }
        }

        if (issues.Count > 0)
            throw new InvalidOperationException(
                "CAD Realism FBX validation failed:\n" + string.Join("\n", issues));

        Debug.Log(
            $"CAD Realism: validated {paths.Count} FBX files ({guids.Length} meshes), " +
            $"{vertices:N0} vertices and {triangles:N0} triangles; normals, tangents, UV0 and bounds are present.");
    }

    private static Mesh CreateOptimizedMesh(Mesh source, float smoothingAngle, float textureScale)
    {
        Mesh mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = source.name + "_Render";
        mesh.indexFormat = source.vertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;

        RemoveInvalidAndDuplicateFaces(mesh);
        RecalculateAngleWeightedNormals(mesh, smoothingAngle);
        GenerateBoxUvs(mesh, textureScale);
        try
        {
            mesh.RecalculateTangents();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"CAD Realism: tangents could not be calculated for {source.name}: {exception.Message}");
        }
        mesh.RecalculateBounds();

        string sourcePath = AssetDatabase.GetAssetPath(source);
        string stem = string.IsNullOrEmpty(sourcePath)
            ? source.name
            : Path.GetFileNameWithoutExtension(sourcePath);
        string path = AssetDatabase.GenerateUniqueAssetPath(
            DerivedMeshRoot + "/" + Sanitize(stem) + "_Render.asset");
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void RecalculateAngleWeightedNormals(Mesh mesh, float angle)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        var incident = new Vector3[vertices.Length];

        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];
            Vector3 weighted = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            incident[a] += weighted;
            incident[b] += weighted;
            incident[c] += weighted;
        }

        var groups = new Dictionary<PositionKey, List<int>>(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            PositionKey key = new PositionKey(vertices[i]);
            List<int> list;
            if (!groups.TryGetValue(key, out list))
            {
                list = new List<int>(4);
                groups.Add(key, list);
            }
            list.Add(i);
        }

        float threshold = Mathf.Cos(angle * Mathf.Deg2Rad);
        var normals = new Vector3[vertices.Length];
        foreach (List<int> group in groups.Values)
        {
            foreach (int index in group)
            {
                Vector3 reference = incident[index].sqrMagnitude > 0f ? incident[index].normalized : Vector3.up;
                Vector3 sum = Vector3.zero;
                foreach (int candidate in group)
                {
                    Vector3 candidateNormal = incident[candidate];
                    if (candidateNormal.sqrMagnitude > 0f &&
                        Vector3.Dot(reference, candidateNormal.normalized) >= threshold)
                        sum += candidateNormal;
                }
                normals[index] = sum.sqrMagnitude > 0f ? sum.normalized : reference;
            }
        }
        mesh.normals = normals;
    }

    private static void GenerateBoxUvs(Mesh mesh, float scale)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i + 2 < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];
            Vector3 normal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).normalized;
            Vector3 abs = new Vector3(Mathf.Abs(normal.x), Mathf.Abs(normal.y), Mathf.Abs(normal.z));
            uv[a] = Project(vertices[a], abs, scale);
            uv[b] = Project(vertices[b], abs, scale);
            uv[c] = Project(vertices[c], abs, scale);
        }
        mesh.uv = uv;
    }

    private static Vector2 Project(Vector3 value, Vector3 normal, float scale)
    {
        if (normal.x >= normal.y && normal.x >= normal.z)
            return new Vector2(value.z, value.y) * scale;
        if (normal.y >= normal.z)
            return new Vector2(value.x, value.z) * scale;
        return new Vector2(value.x, value.y) * scale;
    }

    private static void RemoveInvalidAndDuplicateFaces(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
        {
            int[] input = mesh.GetTriangles(subMesh);
            var output = new List<int>(input.Length);
            var seen = new HashSet<TriangleKey>();
            for (int i = 0; i + 2 < input.Length; i += 3)
            {
                int a = input[i];
                int b = input[i + 1];
                int c = input[i + 2];
                if (a == b || b == c || c == a)
                    continue;
                if (Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude < 1e-18f)
                    continue;
                if (!seen.Add(new TriangleKey(vertices[a], vertices[b], vertices[c])))
                    continue;
                output.Add(a);
                output.Add(b);
                output.Add(c);
            }
            mesh.SetTriangles(output, subMesh, false);
        }
    }

    private struct MeshAudit
    {
        public int triangles;
        public int degenerate;
        public int duplicate;
    }

    private static MeshAudit Audit(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;
        var result = new MeshAudit { triangles = indices.Length / 3 };
        var seen = new HashSet<TriangleKey>();
        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            int a = indices[i];
            int b = indices[i + 1];
            int c = indices[i + 2];
            if (a == b || b == c || c == a ||
                Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]).sqrMagnitude < 1e-18f)
                result.degenerate++;
            if (!seen.Add(new TriangleKey(vertices[a], vertices[b], vertices[c])))
                result.duplicate++;
        }
        return result;
    }

    private static void UpgradeCadPalette(
        Texture2D detail, Texture2D normal, Texture2D brushedNormal, Texture2D ao,
        Texture2D packedDielectric, Texture2D packedMetal, Texture2D packedWorn)
    {
        string root = "Assets/PM_Robot/Materials";
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith(MaterialRoot, StringComparison.Ordinal))
                continue;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
                continue;

            string name = material.name.ToLowerInvariant();
            Color color = material.HasProperty("_Color") ? material.color : Color.white;
            var spec = new MaterialSpec
            {
                name = material.name,
                color = color,
                metallic = IsBareMetal(name) ? 0.90f : 0f,
                smoothness = IsBareMetal(name) ? 0.61f : (name == "black" ? 0.35f : 0.56f),
                variation = IsBareMetal(name) ? 0.11f : 0.10f,
                normalStrength = name == "black" ? 0.23f : 0.14f,
                useMicroAo = name == "black"
            };
            ConfigureMaterial(
                material, spec, detail, normal, brushedNormal, ao,
                packedDielectric, packedMetal, packedWorn);
            EditorUtility.SetDirty(material);
        }
    }

    private static bool IsBareMetal(string name)
    {
        return name == "grey" || name.StartsWith("rgba-0.75", StringComparison.Ordinal) ||
               name.StartsWith("rgba-0.78", StringComparison.Ordinal);
    }

    private static void CreateOrUpdateMaterial(
        string path, MaterialSpec spec, Texture2D detail, Texture2D normal,
        Texture2D brushedNormal, Texture2D ao,
        Texture2D packedDielectric, Texture2D packedMetal, Texture2D packedWorn)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard")) { name = spec.name };
            AssetDatabase.CreateAsset(material, path);
        }
        ConfigureMaterial(
            material, spec, detail, normal, brushedNormal, ao,
            packedDielectric, packedMetal, packedWorn);
        EditorUtility.SetDirty(material);
    }

    private static void ConfigureMaterial(
        Material material, MaterialSpec spec, Texture2D detail, Texture2D normal,
        Texture2D brushedNormal, Texture2D ao,
        Texture2D packedDielectric, Texture2D packedMetal, Texture2D packedWorn)
    {
        bool wornSurface = spec.metallic > 0.05f && spec.metallic < 0.7f;
        Texture2D packed = spec.metallic >= 0.7f
            ? packedMetal
            : (wornSurface ? packedWorn : packedDielectric);
        float packedAverage = wornSurface ? 0.875f : 0.95f;

        material.shader = Shader.Find("Standard");
        material.SetColor("_Color", spec.color);
        material.SetFloat("_Metallic", spec.metallic);
        material.SetFloat("_Glossiness", spec.smoothness);
        material.SetFloat(
            "_GlossMapScale", spec.transparent ? spec.smoothness : spec.smoothness / packedAverage);
        material.SetFloat("_BumpScale", spec.normalStrength);
        material.SetFloat("_DetailNormalMapScale", spec.normalStrength);
        material.SetTexture("_DetailAlbedoMap", detail);
        material.SetTexture("_DetailNormalMap", spec.metallic >= 0.7f ? brushedNormal : normal);
        material.SetTexture("_MetallicGlossMap", spec.transparent ? null : packed);
        material.SetTextureScale("_DetailAlbedoMap", Vector2.one * Mathf.Lerp(7f, 18f, spec.variation));
        material.SetTextureScale("_DetailNormalMap", Vector2.one * Mathf.Lerp(8f, 22f, spec.normalStrength));
        material.EnableKeyword("_DETAIL_MULX2");
        material.EnableKeyword("_NORMALMAP");
        if (spec.transparent)
            material.DisableKeyword("_METALLICGLOSSMAP");
        else
            material.EnableKeyword("_METALLICGLOSSMAP");
        material.enableInstancing = true;

        if (spec.useMicroAo)
        {
            material.SetTexture("_OcclusionMap", ao);
            material.SetFloat("_OcclusionStrength", 0.28f);
        }
        else
        {
            material.SetTexture("_OcclusionMap", null);
            material.SetFloat("_OcclusionStrength", 1f);
        }

        if (spec.transparent)
        {
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.SetOverrideTag("RenderType", string.Empty);
            material.renderQueue = -1;
        }
    }

    private static Texture2D CreateDetailAlbedo(string path)
    {
        return CreateOrReplaceTexture(path, "Fine roughness variation", (x, y, size) =>
        {
            float noise = FractalNoise(x, y, size);
            float value = 0.5f + (noise - 0.5f) * 0.10f;
            return new Color(value, value, value, 1f);
        });
    }

    private static Texture2D CreateDetailNormal(string path)
    {
        return CreateOrReplaceTexture(path, "Fine surface normal", (x, y, size) =>
        {
            float left = FractalNoise((x - 1 + size) % size, y, size);
            float right = FractalNoise((x + 1) % size, y, size);
            float down = FractalNoise(x, (y - 1 + size) % size, size);
            float up = FractalNoise(x, (y + 1) % size, size);
            float nx = Mathf.Clamp((left - right) * 1.8f, -1f, 1f);
            float ny = Mathf.Clamp((down - up) * 1.8f, -1f, 1f);
            // Both RGB and DXT5nm-compatible A/G channels are populated.
            return new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, 1f, nx * 0.5f + 0.5f);
        });
    }

    private static Texture2D CreateMicroOcclusion(string path)
    {
        return CreateOrReplaceTexture(path, "Fine surface occlusion", (x, y, size) =>
        {
            float value = Mathf.Lerp(0.90f, 1f, FractalNoise(x, y, size));
            return new Color(value, value, value, 1f);
        });
    }

    private static Texture2D CreateBrushedNormal(string path)
    {
        return CreateOrReplaceTexture(path, "Brushed metal normal", (x, y, size) =>
        {
            float left = BrushedHeight((x - 1 + size) % size, y, size);
            float right = BrushedHeight((x + 1) % size, y, size);
            float down = BrushedHeight(x, (y - 1 + size) % size, size);
            float up = BrushedHeight(x, (y + 1) % size, size);
            float nx = Mathf.Clamp((left - right) * 0.75f, -1f, 1f);
            float ny = Mathf.Clamp((down - up) * 1.5f, -1f, 1f);
            return new Color(nx * 0.5f + 0.5f, ny * 0.5f + 0.5f, 1f, nx * 0.5f + 0.5f);
        });
    }

    private static Texture2D CreatePackedSurface(
        string path, float metallic, float roughnessVariation, bool brushed)
    {
        return CreateOrReplaceTexture(path, "Fine metallic smoothness", (x, y, size) =>
        {
            // Standard reads metallic from R and smoothness from A.
            float noise = brushed ? BrushedNoise(x, y, size) : FractalNoise(x, y, size);
            float smoothness = Mathf.Lerp(
                1f - roughnessVariation, 1f, noise);
            return new Color(metallic, 0f, 0f, smoothness);
        });
    }

    private static Texture2D CreateOrReplaceTexture(
        string path, string name, Func<int, int, int, Color> pixel)
    {
        const int size = 128;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
        {
            texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            AssetDatabase.CreateAsset(texture, path);
        }
        // Unity expects a main asset's object name to match its filename.
        // Keep the descriptive name in the caller for readability/debugging,
        // but use the filename here to avoid a reimport warning.
        texture.name = Path.GetFileNameWithoutExtension(path);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 4;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            pixels[y * size + x] = pixel(x, y, size);
        texture.SetPixels(pixels);
        texture.Apply(true, false);
        EditorUtility.SetDirty(texture);
        return texture;
    }

    private static float FractalNoise(int x, int y, int size)
    {
        float value = 0f;
        float weight = 0f;
        for (int octave = 0; octave < 4; octave++)
        {
            int frequency = 1 << octave;
            float sample = Mathf.PerlinNoise(
                (x * frequency + 17.3f) / size,
                (y * frequency + 41.7f) / size);
            float amplitude = 1f / frequency;
            value += sample * amplitude;
            weight += amplitude;
        }
        return value / weight;
    }

    private static float BrushedNoise(int x, int y, int size)
    {
        float longGrain = Mathf.PerlinNoise((x + 11.3f) / size * 0.7f, (y + 5.1f) / size * 22f);
        return Mathf.Clamp01(longGrain * 0.72f + FractalNoise(x, y, size) * 0.28f);
    }

    private static float BrushedHeight(int x, int y, int size)
    {
        float phase = Mathf.PerlinNoise((x + 7.1f) / size * 0.8f, 0.31f) * Mathf.PI * 2f;
        float fineLine = Mathf.Sin((float)y / size * Mathf.PI * 72f + phase) * 0.5f + 0.5f;
        return fineLine * 0.35f + BrushedNoise(x, y, size) * 0.65f;
    }

    private static bool IsUnderCollision(Transform transform)
    {
        for (Transform current = transform; current != null; current = current.parent)
            if (current.name.Equals("Collisions", StringComparison.OrdinalIgnoreCase) ||
                current.name.IndexOf("_coll", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }

    private static string NormalizeMeshName(string name)
    {
        name = Regex.Replace(name, @"_Render$", string.Empty, RegexOptions.IgnoreCase);
        return Regex.Replace(name, @"_\d+$", string.Empty);
    }

    private static string Sanitize(string value)
    {
        return Regex.Replace(value, @"[^A-Za-z0-9_.-]", "_");
    }

    private static Color Rgb(float r, float g, float b)
    {
        return new Color(r, g, b, 1f);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private readonly struct PositionKey : IEquatable<PositionKey>, IComparable<PositionKey>
    {
        private const float Precision = 1000000f;
        private readonly long x;
        private readonly long y;
        private readonly long z;

        public PositionKey(Vector3 value)
        {
            x = (long)Math.Round(value.x * Precision);
            y = (long)Math.Round(value.y * Precision);
            z = (long)Math.Round(value.z * Precision);
        }

        public bool Equals(PositionKey other) => x == other.x && y == other.y && z == other.z;
        public override bool Equals(object obj) => obj is PositionKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = x.GetHashCode();
                hash = hash * 397 ^ y.GetHashCode();
                return hash * 397 ^ z.GetHashCode();
            }
        }

        public int CompareTo(PositionKey other)
        {
            int result = x.CompareTo(other.x);
            if (result != 0) return result;
            result = y.CompareTo(other.y);
            return result != 0 ? result : z.CompareTo(other.z);
        }
    }

    private readonly struct TriangleKey : IEquatable<TriangleKey>
    {
        private readonly PositionKey a;
        private readonly PositionKey b;
        private readonly PositionKey c;

        public TriangleKey(Vector3 first, Vector3 second, Vector3 third)
        {
            var keys = new[] { new PositionKey(first), new PositionKey(second), new PositionKey(third) };
            Array.Sort(keys);
            a = keys[0];
            b = keys[1];
            c = keys[2];
        }

        public bool Equals(TriangleKey other) => a.Equals(other.a) && b.Equals(other.b) && c.Equals(other.c);
        public override bool Equals(object obj) => obj is TriangleKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = a.GetHashCode();
                hash = hash * 397 ^ b.GetHashCode();
                return hash * 397 ^ c.GetHashCode();
            }
        }
    }
}

/// <summary>
/// FBX/OBJ/DAE assets can use Unity's real ModelImporter. STL cannot, and is
/// handled by CadRealismPipeline instead.
/// </summary>
public sealed class CadModelImportPostprocessor : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith("Assets/PM_Robot/", StringComparison.Ordinal) ||
            assetPath.IndexOf("/RenderOptimized/", StringComparison.Ordinal) < 0)
            return;

        var importer = (ModelImporter)assetImporter;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.weldVertices = true;
        importer.optimizeMeshPolygons = true;
        importer.optimizeMeshVertices = true;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.isReadable = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
    }
}
