using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Robotics.UrdfImporter;
using Link = Unity.Robotics.UrdfImporter.Link;
using Joint = Unity.Robotics.UrdfImporter.Joint;

/// <summary>
/// Editor window ("Config Tools/Delta Import Robot Parts") that imports only
/// the robot parts that exist in the generated combined URDF + manifest but
/// are missing from the scene robot. Existing objects are never touched, so
/// all manual modifications (ArticulationBody tuning, attached scripts,
/// sensors) survive. Typical workflow after adding a tool/chuck/tip in ROS:
///   1. colcon build, run generate_unity_urdf.py (pm_robot_description)
///   2. here: Scan, then Import missing parts
///   3. Play (configureRobot activates the selected items from the manifest)
/// </summary>
public class DeltaUrdfImporter : EditorWindow
{
    private string urdfPath = "";
    private string manifestPath = "";
    private string meshSourceDir = "";
    private string robotRootName = "pm_robot";
    private GameObject robotRoot;

    private Vector2 scrollPos;
    private List<ScanResult> scanResults;
    private string scanSummary;
    private List<GameObject> rootCandidates = new List<GameObject>();

    private class ScanResult
    {
        public ManifestSection section;
        public ManifestItem item;
        public List<string> missingLinks;   // topologically ordered, parents first
        public List<string> meshesToCopy;
        public string targetParentPath;     // where the item's first root link lands
        public string blockedReason;        // null when importable
    }

    [MenuItem("Config Tools/Delta Import Robot Parts")]
    public static void ShowWindow()
    {
        GetWindow<DeltaUrdfImporter>("Delta Import Robot Parts");
    }

    void OnEnable()
    {
        if (string.IsNullOrEmpty(urdfPath))
        {
            // Same resolution as configureRobot uses at runtime: read the
            // installed files so Unity and ROS stay in lockstep.
            string pkg = configureRobot.GetROS2PackagePath("pm_robot_description");
            if (!string.IsNullOrEmpty(pkg))
            {
                string urdfDir = Path.Combine(pkg, "share", "pm_robot_description", "urdf");
                urdfPath = Path.Combine(urdfDir, "pm_robot_unity.urdf");
                manifestPath = Path.Combine(urdfDir, "pm_robot_unity_manifest.json");
                meshSourceDir = Path.Combine(pkg, "share", "pm_robot_description");
            }
        }
        RefreshRootCandidates();
    }

    /// <summary>Collects every plausible robot root in the open scene. A scene
    /// can easily end up with more than one (e.g. 'pm_robot' plus a leftover
    /// 'pm_robot (1)' from an earlier full import); silently picking one would
    /// import parts into the wrong robot, so the choice is made explicit.</summary>
    private void RefreshRootCandidates()
    {
        rootCandidates = EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .Where(go => go.name == robotRootName ||
                         go.name.StartsWith(robotRootName + " ("))   // "pm_robot (1)"
            .ToList();

        if (robotRoot == null && rootCandidates.Count == 1)
            robotRoot = rootCandidates[0];
    }

    void OnGUI()
    {
        GUILayout.Label("Delta Import Robot Parts", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Imports links that exist in the generated URDF/manifest but not in the scene robot. " +
            "Existing objects are never modified.", MessageType.Info);

        urdfPath = EditorGUILayout.TextField("Combined URDF", urdfPath);
        manifestPath = EditorGUILayout.TextField("Manifest", manifestPath);
        meshSourceDir = EditorGUILayout.TextField("ROS mesh source dir", meshSourceDir);

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            robotRoot = (GameObject)EditorGUILayout.ObjectField(
                "Robot root", robotRoot, typeof(GameObject), true);
            if (GUILayout.Button("Find", GUILayout.Width(50)))
                RefreshRootCandidates();
        }

        if (rootCandidates.Count > 1)
            EditorGUILayout.HelpBox(
                $"The scene contains {rootCandidates.Count} robot roots: " +
                string.Join(", ", rootCandidates.Select(g => g.name)) +
                ". Parts are imported into the one assigned above - pick the robot you actually use " +
                "(and consider deleting the leftover one).", MessageType.Warning);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(robotRoot == null))
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan (dry run)"))
                Scan();

            using (new EditorGUI.DisabledScope(
                scanResults == null || !scanResults.Any(r => r.blockedReason == null)))
            {
                if (GUILayout.Button("Import missing parts"))
                    Import();
            }
        }
        if (robotRoot == null)
            EditorGUILayout.HelpBox("Assign the robot root object to scan/import.", MessageType.Info);

        if (scanSummary != null)
            EditorGUILayout.HelpBox(scanSummary, MessageType.None);

        if (scanResults == null)
            return;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var r in scanResults)
        {
            string title = $"{r.section.yamlPath}: {string.Join(" / ", r.item.yamlNames)}";
            if (r.blockedReason != null)
                EditorGUILayout.HelpBox($"{title}\nBLOCKED: {r.blockedReason}", MessageType.Warning);
            else
                EditorGUILayout.HelpBox(
                    $"{title}\nparent: {r.targetParentPath}" +
                    $"\nlinks: {string.Join(", ", r.missingLinks)}" +
                    (r.meshesToCopy.Count > 0 ? $"\nmeshes to copy: {string.Join(", ", r.meshesToCopy)}" : ""),
                    MessageType.None);
        }
        EditorGUILayout.EndScrollView();
    }

    // =========================================================================
    // Scan
    // =========================================================================

    private void Scan()
    {
        scanResults = null;
        scanSummary = null;

        var manifest = RobotUnityManifest.Load(manifestPath);
        if (manifest == null)
        {
            scanSummary = $"Could not load manifest: {manifestPath}";
            return;
        }
        if (!File.Exists(urdfPath))
        {
            scanSummary = $"URDF not found: {urdfPath}";
            return;
        }

        if (robotRoot == null)
        {
            scanSummary = "No robot root assigned.";
            return;
        }

        XDocument urdf = XDocument.Load(urdfPath);
        Dictionary<string, XElement> linkElements = urdf.Root.Elements("link")
            .GroupBy(e => (string)e.Attribute("name"))
            .ToDictionary(g => g.Key, g => g.First());
        var jointsByChild = new Dictionary<string, List<XElement>>();
        foreach (var j in urdf.Root.Elements("joint"))
        {
            string child = (string)j.Element("child")?.Attribute("link");
            if (child == null) continue;
            if (!jointsByChild.TryGetValue(child, out var list))
                jointsByChild[child] = list = new List<XElement>();
            list.Add(j);
        }

        Dictionary<string, Transform> sceneIndex = BuildSceneIndex(robotRoot.transform);
        Dictionary<string, string> opcUaMap = LoadOpcUaMap();

        var results = new List<ScanResult>();
        foreach (var section in manifest.sections)
        {
            foreach (var item in section.items)
            {
                var missing = item.links.Where(l => !sceneIndex.ContainsKey(l)).ToList();
                if (missing.Count == 0)
                    continue;

                var result = new ScanResult { section = section, item = item, missingLinks = new List<string>() };

                // Topological order: repeatedly place links whose parent is in
                // the scene or already placed.
                var remaining = new List<string>(missing);
                while (remaining.Count > 0)
                {
                    string next = remaining.FirstOrDefault(l =>
                    {
                        string parent = ResolveParentLink(l, item, jointsByChild);
                        return parent != null &&
                               (ResolveSceneTransform(parent, sceneIndex, opcUaMap) != null ||
                                result.missingLinks.Contains(parent));
                    });
                    if (next == null)
                    {
                        string first = remaining[0];
                        string parent = ResolveParentLink(first, item, jointsByChild) ?? "<no joint in URDF>";
                        result.blockedReason = $"parent '{parent}' of link '{first}' not found in scene or URDF. " +
                                               "Import/repair the parent first (or check OpcUaAxisNames.json).";
                        break;
                    }
                    result.missingLinks.Add(next);
                    remaining.Remove(next);
                }

                result.meshesToCopy = (item.meshes ?? Array.Empty<string>())
                    .Where(m => !File.Exists(Path.Combine(Application.dataPath, "PM_Robot", m)))
                    .ToList();

                if (result.blockedReason == null && result.missingLinks.Count > 0)
                {
                    string firstParent = ResolveParentLink(result.missingLinks[0], item, jointsByChild);
                    Transform p = ResolveSceneTransform(firstParent, sceneIndex, opcUaMap);
                    result.targetParentPath = p != null ? PathOf(p) : firstParent;
                }
                results.Add(result);
            }
        }

        scanResults = results;
        int importable = results.Count(r => r.blockedReason == null);
        int blocked = results.Count - importable;
        scanSummary = results.Count == 0
            ? "Scene is up to date: every manifest link exists."
            : $"{importable} item(s) missing and importable" + (blocked > 0 ? $", {blocked} blocked." : ".");
    }

    // =========================================================================
    // Import
    // =========================================================================

    private void Import()
    {
        if (robotRoot == null || scanResults == null)
            return;

        // 1. Copy missing meshes into the project so the URDF-Importer's STL
        //    postprocessor generates the mesh prefabs, exactly like the
        //    original whole-robot import did.
        int copied = 0;
        foreach (var mesh in scanResults.Where(r => r.blockedReason == null).SelectMany(r => r.meshesToCopy).Distinct())
        {
            string src = Path.Combine(meshSourceDir, mesh);
            string dst = Path.Combine(Application.dataPath, "PM_Robot", mesh);
            if (!File.Exists(src))
            {
                Debug.LogError($"DeltaUrdfImporter: mesh not found in ROS package: {src}");
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, false);
            copied++;
        }
        if (copied > 0)
            AssetDatabase.Refresh();

        // 2. Recreate the context a whole-robot import would have.
        var settings = ImportSettings.DefaultSettings();
        settings.convexMethod = ImportSettings.convexDecomposer.unity; // colliders get disabled anyway
        UrdfRobotExtensions.importsettings = settings;
        UrdfAssetPathHandler.SetPackageRoot("Assets/PM_Robot");

        XDocument urdf = XDocument.Load(urdfPath);
        Dictionary<string, XElement> linkElements = urdf.Root.Elements("link")
            .GroupBy(e => (string)e.Attribute("name"))
            .ToDictionary(g => g.Key, g => g.First());
        var jointsByChild = new Dictionary<string, List<XElement>>();
        foreach (var j in urdf.Root.Elements("joint"))
        {
            string child = (string)j.Element("child")?.Attribute("link");
            if (child == null) continue;
            if (!jointsByChild.TryGetValue(child, out var list))
                jointsByChild[child] = list = new List<XElement>();
            list.Add(j);
        }

        Dictionary<string, Transform> sceneIndex = BuildSceneIndex(robotRoot.transform);
        Dictionary<string, string> opcUaMap = LoadOpcUaMap();

        int createdLinks = 0, importedItems = 0;
        var report = new List<string>();
        foreach (var r in scanResults.Where(r => r.blockedReason == null))
        {
            var createdThisItem = new List<GameObject>();
            foreach (var linkName in r.missingLinks)
            {
                if (!linkElements.TryGetValue(linkName, out var linkEl))
                {
                    Debug.LogError($"DeltaUrdfImporter: link '{linkName}' not found in URDF, skipping.");
                    continue;
                }
                string parentName = ResolveParentLink(linkName, r.item, jointsByChild);
                Transform parent = ResolveSceneTransform(parentName, sceneIndex, opcUaMap);
                if (parent == null)
                {
                    Debug.LogError($"DeltaUrdfImporter: parent '{parentName}' of '{linkName}' not found, skipping.");
                    continue;
                }

                XElement jointEl = SelectJoint(linkName, r.item, jointsByChild);
                GameObject go = UrdfLinkExtensions.Create(parent, new Link(linkEl), new Joint(jointEl));
                Undo.RegisterCreatedObjectUndo(go, "Delta import robot part");
                sceneIndex[go.name] = go.transform;
                createdThisItem.Add(go);
                createdLinks++;
            }

            // 3. Scene conventions (mirrors ApplyGeneralSettings): colliders
            //    off, ArticulationBodies off (only the OPC-UA axes and
            //    controller-driven jaws are enabled, and those are existing
            //    objects). New items start deactivated; configureRobot
            //    activates the selected one at Play.
            foreach (var go in createdThisItem)
            {
                foreach (var col in go.GetComponentsInChildren<MeshCollider>(true))
                    col.enabled = false;
                foreach (var ab in go.GetComponentsInChildren<ArticulationBody>(true))
                    ab.enabled = false;
            }
            foreach (var rootLink in r.item.rootLinks)
            {
                var go = createdThisItem.FirstOrDefault(g => g.name == rootLink);
                go?.SetActive(false);
            }
            if (createdThisItem.Count > 0)
            {
                importedItems++;
                report.Add($"  {r.section.yamlPath} [{string.Join(" / ", r.item.yamlNames)}]: " +
                           string.Join(", ", createdThisItem.Select(g => PathOf(g.transform))));
            }
        }

        EditorSceneManager.MarkSceneDirty(robotRoot.scene);
        Debug.Log($"DeltaUrdfImporter: imported {importedItems} item(s), {createdLinks} link(s), " +
                  $"copied {copied} mesh(es) into '{PathOf(robotRoot.transform)}'. New parts start " +
                  "deactivated; press Play or call the configure_robot service to activate the " +
                  "selected configuration.\n" + string.Join("\n", report));

        Scan(); // refresh the view; should now report everything present
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static string PathOf(Transform t)
    {
        var parts = new List<string>();
        for (var c = t; c != null; c = c.parent)
            parts.Add(c.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static Dictionary<string, Transform> BuildSceneIndex(Transform robotRoot)
    {
        var index = new Dictionary<string, Transform>();
        foreach (var t in robotRoot.GetComponentsInChildren<Transform>(true))
            if (!index.ContainsKey(t.name))
                index[t.name] = t;
        return index;
    }

    /// <summary>URDF link names of moved axes differ from the scene (OPC-UA
    /// renames, e.g. Gripper_Rot_Plate -> RobotAxisT); apply the mapping when
    /// a parent is looked up.</summary>
    private static Dictionary<string, string> LoadOpcUaMap()
    {
        var map = new Dictionary<string, string>();
        string path = Path.Combine(Application.dataPath, "PM_Robot", "Configs", "OpcUaAxisNames.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"DeltaUrdfImporter: {path} not found; URDF axis names are used as-is.");
            return map;
        }
        var mappings = JsonUtility.FromJson<LinkMappings>(File.ReadAllText(path));
        foreach (var m in mappings?.mappings ?? Array.Empty<LinkMapping>())
            if (!string.IsNullOrEmpty(m.original) && !string.IsNullOrEmpty(m.newName))
                map[m.original] = m.newName;
        return map;
    }

    private static Transform ResolveSceneTransform(string linkName,
        Dictionary<string, Transform> sceneIndex, Dictionary<string, string> opcUaMap)
    {
        if (linkName == null)
            return null;
        // OPC-UA-renamed axis links (e.g. Gripper_Rot_Plate -> RobotAxisT) are
        // checked first: the renamed object is the one actually driven by the
        // robot's kinematic chain. Checking the literal URDF name first would
        // wrongly match a stray leftover GameObject that still carries the
        // original link name (e.g. an orphaned duplicate mesh/prefab) instead
        // of falling through to the real axis object.
        if (opcUaMap.TryGetValue(linkName, out var renamed) && sceneIndex.TryGetValue(renamed, out var t))
            return t;
        if (sceneIndex.TryGetValue(linkName, out t))
            return t;
        return null;
    }

    private static string ResolveParentLink(string linkName, ManifestItem item,
        Dictionary<string, List<XElement>> jointsByChild)
    {
        var joint = SelectJoint(linkName, item, jointsByChild);
        return joint == null ? null : (string)joint.Element("parent")?.Attribute("link");
    }

    /// <summary>Picks the joint for a link. Manifest links have exactly one
    /// joint by construction; shared base links can carry several joints in
    /// the combined URDF, in which case the one whose parent belongs to the
    /// same item wins.</summary>
    private static XElement SelectJoint(string linkName, ManifestItem item,
        Dictionary<string, List<XElement>> jointsByChild)
    {
        if (!jointsByChild.TryGetValue(linkName, out var joints) || joints.Count == 0)
            return null;
        if (joints.Count == 1)
            return joints[0];
        var ownLinks = new HashSet<string>(item.links);
        return joints.FirstOrDefault(j => ownLinks.Contains((string)j.Element("parent")?.Attribute("link")))
               ?? joints[0];
    }

}
