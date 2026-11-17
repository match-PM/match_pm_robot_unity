using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Assigns every visible part of the scene a segmentation id and a unique color.
///
/// Granularity is as fine as the scene semantics allow: every URDF link (each
/// ArticulationBody: axes, tools, tips, UV lamps, gonio stages, chucks, ...) and every
/// spawned assembly component or adhesive dot is its own segment. Grouping into
/// coarser classes is meant to happen downstream using the published label table.
///
/// The owner of a renderer is the nearest GameObject above it (inclusive) that has a
/// SegmentationLabel, SpawnGameObject, AdhesiveDot, ArticulationBody or Rigidbody;
/// renderers without such an owner become a segment of their own.
///
/// Ids are keyed by the owner's hierarchy path, so an object that is destroyed and
/// re-created at the same place (e.g. a spawner rebuild) keeps its id and color.
/// Ids of the scene that exists at the first scan are assigned in path order, so they
/// are stable between runs as long as the scene does not change. Id 0 is background.
/// Shared by all cameras so ids are consistent between them.
/// </summary>
public static class SegmentationRegistry
{
    public class Label
    {
        public ushort id;
        public string name;      // URDF/ROS name where known
        public string unityName; // GameObject name in Unity
        public string kind;      // robot_link, assembly_component, adhesive, object, custom
        public string path;
        public Color32 color;
        public Vector4 shaderColor; // exact byte values / 255, no color space conversion
    }

    public class Entry
    {
        public Renderer renderer;
        public MeshFilter meshFilter;
        public SkinnedMeshRenderer skinned;
        public Label label; // null = ignored

        public Mesh Mesh => skinned != null ? skinned.sharedMesh
                          : meshFilter != null ? meshFilter.sharedMesh : null;
    }

    private static readonly Dictionary<string, Label> labelsByPath = new Dictionary<string, Label>();
    private static readonly List<Label> labels = new List<Label>();
    private static readonly HashSet<int> usedColors = new HashSet<int>();
    private static readonly Dictionary<int, Entry> entriesByRenderer = new Dictionary<int, Entry>();
    private static readonly List<Entry> entries = new List<Entry>();
    private static Dictionary<string, string> unityToUrdfName;
    private static bool initialScanDone;
    private static int lastResetFrame = -1;

    /// <summary>Incremented whenever a label is added.</summary>
    public static int Version { get; private set; }

    public static IReadOnlyList<Entry> Entries => entries;
    public static IReadOnlyList<Label> Labels => labels;

    // Static state survives leaving Play mode when domain reload is disabled.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        labelsByPath.Clear();
        labels.Clear();
        usedColors.Clear();
        entriesByRenderer.Clear();
        entries.Clear();
        initialScanDone = false;
        lastResetFrame = -1;
        unityToUrdfName = null;
        Version = 0;
    }

    /// <summary>Picks up new renderers and drops destroyed ones. Main thread only.</summary>
    public static void Rescan()
    {
        // Several cameras may call this in the same frame.
        if (lastResetFrame == Time.frameCount)
            return;
        lastResetFrame = Time.frameCount;

        unityToUrdfName ??= LoadUrdfNameMap();

        entries.RemoveAll(e => e.renderer == null);
        if (entries.Count != entriesByRenderer.Count)
        {
            entriesByRenderer.Clear();
            foreach (var e in entries)
                entriesByRenderer[e.renderer.GetInstanceID()] = e;
        }

        // Inactive objects too, so ids do not depend on what happens to be active.
        var renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        var fresh = new List<(Renderer renderer, Transform owner, string path)>();
        foreach (var r in renderers)
        {
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                continue;
            if (entriesByRenderer.ContainsKey(r.GetInstanceID()))
                continue;
            Transform owner = FindOwner(r.transform);
            fresh.Add((r, owner, PathOf(owner)));
        }

        if (!initialScanDone)
        {
            fresh.Sort((a, b) => string.CompareOrdinal(a.path, b.path));
            initialScanDone = true;
        }

        foreach (var (r, owner, path) in fresh)
        {
            var entry = new Entry
            {
                renderer = r,
                meshFilter = r.GetComponent<MeshFilter>(),
                skinned = r as SkinnedMeshRenderer,
                label = GetOrCreateLabel(owner, path),
            };
            entriesByRenderer[r.GetInstanceID()] = entry;
            entries.Add(entry);
        }
    }

    private static Transform FindOwner(Transform t)
    {
        for (Transform p = t; p != null; p = p.parent)
        {
            if (p.GetComponent<SegmentationLabel>() != null ||
                p.GetComponent<SpawnGameObject>() != null ||
                p.GetComponent<AdhesiveDot>() != null ||
                p.GetComponent<ArticulationBody>() != null ||
                p.GetComponent<Rigidbody>() != null)
                return p;
        }
        return t;
    }

    private static Label GetOrCreateLabel(Transform owner, string path)
    {
        var custom = owner.GetComponent<SegmentationLabel>();
        if (custom != null && custom.ignore)
            return null;

        if (labelsByPath.TryGetValue(path, out var existing))
            return existing;

        if (labels.Count >= ushort.MaxValue)
        {
            Debug.LogWarning("SegmentationRegistry: more than 65535 segments, ignoring the rest.");
            return null;
        }

        string kind;
        string name = owner.name;
        if (custom != null)
        {
            kind = string.IsNullOrEmpty(custom.kind) ? "custom" : custom.kind;
            if (!string.IsNullOrEmpty(custom.labelName))
                name = custom.labelName;
        }
        else if (owner.GetComponent<SpawnGameObject>() != null)
            kind = "assembly_component";
        else if (owner.GetComponent<AdhesiveDot>() != null)
            kind = "adhesive";
        else if (owner.GetComponent<ArticulationBody>() != null)
        {
            kind = "robot_link";
            if (unityToUrdfName.TryGetValue(owner.name, out var urdfName))
                name = urdfName;
        }
        else
            kind = "object";

        var label = new Label
        {
            id = (ushort)(labels.Count + 1),
            name = name,
            unityName = owner.name,
            kind = kind,
            path = path,
        };
        label.color = UniqueColor(label.id);
        label.shaderColor = new Vector4(label.color.r / 255f, label.color.g / 255f, label.color.b / 255f, 1f);

        labelsByPath[path] = label;
        labels.Add(label);
        Version++;
        return label;
    }

    /// <summary>
    /// Well spread, never black (black = background) and unique color per id:
    /// golden-ratio hue steps with alternating saturation/brightness levels.
    /// </summary>
    private static Color32 UniqueColor(int id)
    {
        float hue = (id * 0.618033988749895f) % 1f;
        float[] saturations = { 0.9f, 0.55f, 1f };
        float[] values = { 1f, 0.7f, 0.45f };
        float s = saturations[(id / 3) % saturations.Length];
        float v = values[id % values.Length];

        for (int attempt = 0; attempt < 1000; attempt++)
        {
            Color32 c = Color.HSVToRGB((hue + attempt * 0.0013f) % 1f, s, v);
            int key = (c.r << 16) | (c.g << 8) | c.b;
            if (key != 0 && usedColors.Add(key))
                return c;
        }
        // Practically unreachable; fall back to encoding the id directly.
        return new Color32((byte)(id >> 8), (byte)id, 255, 255);
    }

    private static string PathOf(Transform t)
    {
        var sb = new StringBuilder(t.name);
        for (Transform p = t.parent; p != null; p = p.parent)
            sb.Insert(0, p.name + "/");
        return sb.ToString();
    }

    [Serializable] private class NameMapping { public string original; public string newName; }
    [Serializable] private class NameMappings { public NameMapping[] mappings; }

    // Some URDF links were renamed in Unity for the OPC UA interface (e.g. Z_Axis ->
    // RobotAxisZ); the label table reports the URDF name so it matches ROS.
    private static Dictionary<string, string> LoadUrdfNameMap()
    {
        var map = new Dictionary<string, string>();
        string path = Path.Combine(Application.dataPath, "PM_Robot", "Configs", "OpcUaAxisNames.json");
        try
        {
            if (File.Exists(path))
            {
                var parsed = JsonUtility.FromJson<NameMappings>(File.ReadAllText(path));
                foreach (var m in parsed?.mappings ?? Array.Empty<NameMapping>())
                    if (!string.IsNullOrEmpty(m.original) && !string.IsNullOrEmpty(m.newName))
                        map[m.newName] = m.original;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SegmentationRegistry: could not read {path}: {e.Message}");
        }
        return map;
    }

    /// <summary>The label table as JSON: id, names, kind, path and RGB color per segment.</summary>
    public static string LabelsToJson()
    {
        var sb = new StringBuilder();
        sb.Append("{\"background\":{\"id\":0,\"color\":[0,0,0]},\"labels\":[");
        for (int i = 0; i < labels.Count; i++)
        {
            var l = labels[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"id\":").Append(l.id.ToString(CultureInfo.InvariantCulture))
              .Append(",\"name\":").Append(Quote(l.name))
              .Append(",\"unity_name\":").Append(Quote(l.unityName))
              .Append(",\"kind\":").Append(Quote(l.kind))
              .Append(",\"path\":").Append(Quote(l.path))
              .Append(",\"color\":[").Append(l.color.r).Append(',').Append(l.color.g).Append(',').Append(l.color.b)
              .Append("]}");
        }
        sb.Append("]}");
        return sb.ToString();
    }

    private static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s ?? "")
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        return sb.Append('"').ToString();
    }
}
