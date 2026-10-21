using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using Unity.Robotics.UrdfImporter;
using Link = Unity.Robotics.UrdfImporter.Link;
using Joint = Unity.Robotics.UrdfImporter.Joint;

/// <summary>
/// One-time replacement of the scene's Smarpod Station base (station, axes,
/// stages, top plate) by a fresh import from the generated combined URDF
/// ("Config Tools/Reimport Smarpod Base (one-time)"). The base in older scenes
/// predates the current smaract_hexapod_description: it lacks the
/// calibration/pivot frames and chains the rotation axes A-B-C instead of
/// C-B-A. Chucks and part spawn frames are manifest items and are left out;
/// "Config Tools/Delta Import Robot Parts" imports them afterwards.
///
/// Survives the rebuild: custom components on the station links (Smarpod,
/// ROS2UnityComponent, ...), non-URDF child objects, the active state, and
/// every scene reference into the subtree, re-pointed by object name.
/// </summary>
public static class SmarpodBaseReimporter
{
    private const string StationLink = "Smarpod_Station";
    private const string UndoName = "Reimport Smarpod base";

    // Smarpod field -> URDF link, wired when the field is unassigned. Towers
    // 1/2/3 are the A/B/C stage stacks (Smarpod.GetAJoints/GetBJoints/
    // GetCJoints), their X/Y/Z drives stages 1/2/3.
    private static readonly Dictionary<string, string> SmarpodWiring = new Dictionary<string, string>
    {
        { "PlateXAxis", "SP_X_Axis" }, { "PlateYAxis", "SP_Y_Axis" }, { "PlateZAxis", "SP_Z_Axis" },
        { "PlateAAxis", "SP_A_Axis" }, { "PlateBAxis", "SP_B_Axis" }, { "PlateCAxis", "SP_C_Axis" },
        { "Tower1X", "Smarpod_A_Stage_1" }, { "Tower1Y", "Smarpod_A_Stage_2" }, { "Tower1Z", "Smarpod_A_Stage_3" },
        { "Tower2X", "Smarpod_B_Stage_1" }, { "Tower2Y", "Smarpod_B_Stage_2" }, { "Tower2Z", "Smarpod_B_Stage_3" },
        { "Tower3X", "Smarpod_C_Stage_1" }, { "Tower3Y", "Smarpod_C_Stage_2" }, { "Tower3Z", "Smarpod_C_Stage_3" },
    };

    private class SceneRef
    {
        public Component owner;
        public string propertyPath;
        public string targetName;
        public Type targetType;
    }

    [MenuItem("Config Tools/Reimport Smarpod Base (one-time)")]
    public static void Run()
    {
        // --- Inputs ----------------------------------------------------------
        string pkg = configureRobot.GetROS2PackagePath("pm_robot_description");
        if (string.IsNullOrEmpty(pkg))
        {
            Fail("ROS package 'pm_robot_description' not found.");
            return;
        }
        string urdfDir = Path.Combine(pkg, "share", "pm_robot_description", "urdf");
        string urdfPath = Path.Combine(urdfDir, "pm_robot_unity.urdf");
        var manifest = RobotUnityManifest.Load(Path.Combine(urdfDir, "pm_robot_unity_manifest.json"));
        if (manifest == null || !File.Exists(urdfPath))
        {
            Fail($"Combined URDF or manifest missing in {urdfDir}. Run generate_unity_urdf.py first.");
            return;
        }

        DeltaUrdfImporter.LoadUrdf(urdfPath, out var linkElements, out var jointsByChild);
        if (!linkElements.ContainsKey(StationLink))
        {
            Fail($"'{StationLink}' is not in {urdfPath}. Regenerate it with generate_unity_urdf.py " +
                 "(smaract_hexapod_description must be built).");
            return;
        }

        var robotRoots = EditorSceneManager.GetActiveScene().GetRootGameObjects()
            .Where(go => go.name == "pm_robot" || go.name.StartsWith("pm_robot ("))
            .ToList();
        GameObject robotRoot = robotRoots.Count == 1
            ? robotRoots[0]
            : robotRoots.FirstOrDefault(go => Selection.activeTransform != null &&
                                              Selection.activeTransform.root == go.transform);
        if (robotRoot == null)
        {
            Fail($"Found {robotRoots.Count} robot roots. Select the robot to update in the Hierarchy and retry.");
            return;
        }

        Transform oldStation = robotRoot.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == StationLink && t.GetComponent<UrdfLink>() != null);
        XElement stationJoint = jointsByChild.TryGetValue(StationLink, out var sj) ? sj[0] : null;
        string stationParent = (string)stationJoint?.Element("parent")?.Attribute("link");
        if (oldStation == null || oldStation.parent == null || oldStation.parent.name != stationParent)
        {
            Fail($"No '{StationLink}' link below '{stationParent}' in '{robotRoot.name}'.");
            return;
        }

        // --- Links to create: the station subtree minus manifest items ------
        // Chucks, their spawn frames and the plain spawn frames of the
        // 'empty' chucks belong to the delta importer.
        var excluded = new HashSet<string>();
        foreach (var item in manifest.sections.SelectMany(s => s.items ?? Array.Empty<ManifestItem>()))
        {
            excluded.UnionWith(item.links ?? Array.Empty<string>());
            foreach (var spawnPoint in item.spawnPoints ?? Array.Empty<ManifestSpawnPoint>())
            {
                excluded.Add(spawnPoint.sourceLink);
                excluded.Add(spawnPoint.name);
            }
        }

        var childJoints = new Dictionary<string, List<XElement>>();
        foreach (var joint in jointsByChild.Values.SelectMany(j => j))
        {
            string parent = (string)joint.Element("parent")?.Attribute("link");
            if (parent == null) continue;
            if (!childJoints.TryGetValue(parent, out var list))
                childJoints[parent] = list = new List<XElement>();
            list.Add(joint);
        }

        var order = new List<(string link, XElement joint)> { (StationLink, stationJoint) };
        var planned = new HashSet<string> { StationLink };
        for (int i = 0; i < order.Count; i++)
        {
            foreach (var joint in childJoints.TryGetValue(order[i].link, out var js) ? js : new List<XElement>())
            {
                string child = (string)joint.Element("child")?.Attribute("link");
                if (child != null && !excluded.Contains(child) && planned.Add(child))
                    order.Add((child, joint));
            }
        }

        // --- What has to survive ---------------------------------------------
        var oldTransforms = new HashSet<Transform>(oldStation.GetComponentsInChildren<Transform>(true));
        var oldLinks = oldTransforms.Where(t => t.GetComponent<UrdfLink>() != null).ToList();

        var customComponents = oldLinks
            .SelectMany(t => t.GetComponents<MonoBehaviour>())
            .Where(c => c != null && c.GetType().Namespace != typeof(UrdfLink).Namespace)
            .ToList();
        var customChildren = oldLinks
            .SelectMany(t => t.Cast<Transform>())
            .Where(c => c.GetComponent<UrdfLink>() == null &&
                        c.GetComponent<UrdfVisuals>() == null &&
                        c.GetComponent<UrdfCollisions>() == null)
            .ToList();

        var orphans = customComponents
            .Where(c => !planned.Contains(c.name))
            .Select(c => $"{c.GetType().Name} on {c.name}")
            .Concat(customChildren
                .Where(c => !planned.Contains(c.parent.name))
                .Select(c => $"child '{c.name}' of {c.parent.name}"))
            .ToList();
        if (orphans.Count > 0)
        {
            Fail("These would be lost because their link is not part of the new base:\n" +
                 string.Join("\n", orphans));
            return;
        }

        var sceneRefs = new List<SceneRef>();
        foreach (var go in EditorSceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var comp in go.GetComponentsInChildren<Component>(true))
                if (comp != null && !(comp is Transform) && !oldTransforms.Contains(comp.transform))
                    CollectRefs(comp, oldTransforms, sceneRefs);

        if (!EditorUtility.DisplayDialog(UndoName,
                $"Replace '{DeltaUrdfImporter.PathOf(oldStation)}' ({oldLinks.Count} links) by " +
                $"{order.Count} links from\n{urdfPath}?\n\n" +
                $"Kept: {customComponents.Count} custom component(s), {customChildren.Count} custom " +
                $"child object(s), {sceneRefs.Count} scene reference(s). Undo is available.",
                "Reimport", "Cancel"))
            return;

        // --- Rebuild ---------------------------------------------------------
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);
        int undoGroup = Undo.GetCurrentGroup();

        DeltaUrdfImporter.PrepareUrdfImport();
        var created = new Dictionary<string, GameObject>();
        foreach (var (link, joint) in order)
        {
            Transform parent = link == StationLink
                ? oldStation.parent
                : created[(string)joint.Element("parent").Attribute("link")].transform;
            created[link] = UrdfLinkExtensions.Create(parent, new Link(linkElements[link]), new Joint(joint));
        }
        GameObject newStation = created[StationLink];
        Undo.RegisterCreatedObjectUndo(newStation, UndoName);
        newStation.transform.SetSiblingIndex(oldStation.GetSiblingIndex());
        DeltaUrdfImporter.EnablePhysics(newStation);

        LogPoseChanges(oldLinks, created);

        // Custom components keep their values; references they hold into the
        // old subtree are re-pointed together with the scene references.
        foreach (var comp in customComponents)
        {
            GameObject target = created[comp.name];
            ComponentUtility.CopyComponent(comp);
            ComponentUtility.PasteComponentAsNew(target);
            CollectRefs(target.GetComponents(comp.GetType()).Last(), oldTransforms, sceneRefs);
        }
        foreach (var child in customChildren)
            Undo.SetTransformParent(child, created[child.parent.name].transform, UndoName);

        var unresolved = new List<string>();
        foreach (var r in sceneRefs)
        {
            UnityEngine.Object target = FindByName(newStation.transform, r.targetName, r.targetType);
            var so = new SerializedObject(r.owner);
            so.FindProperty(r.propertyPath).objectReferenceValue = target;
            so.ApplyModifiedProperties();
            if (target == null)
                unresolved.Add($"{DeltaUrdfImporter.PathOf(r.owner.transform)} ({r.owner.GetType().Name}." +
                               $"{r.propertyPath}) -> '{r.targetName}'");
        }

        int wired = 0;
        foreach (var smarpod in newStation.GetComponentsInChildren<ROS2.Smarpod>(true))
        {
            var so = new SerializedObject(smarpod);
            foreach (var kv in SmarpodWiring)
            {
                var prop = so.FindProperty(kv.Key);
                if (prop == null || prop.objectReferenceValue != null || !created.TryGetValue(kv.Value, out var go))
                    continue;
                prop.objectReferenceValue = go.GetComponent<ArticulationBody>();
                wired++;
            }
            so.ApplyModifiedProperties();
        }

        bool wasActive = oldStation.gameObject.activeSelf;
        Undo.DestroyObjectImmediate(oldStation.gameObject);
        newStation.SetActive(wasActive);

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(robotRoot.scene);
        Selection.activeGameObject = newStation;

        Debug.Log($"SmarpodBaseReimporter: rebuilt '{DeltaUrdfImporter.PathOf(newStation.transform)}' with " +
                  $"{created.Count} links; moved {customComponents.Count} component(s) and " +
                  $"{customChildren.Count} child object(s), re-pointed {sceneRefs.Count - unresolved.Count} " +
                  $"reference(s), wired {wired} Smarpod field(s). Next: 'Config Tools/Delta Import Robot " +
                  "Parts' -> Scan -> Import missing parts for the chucks and part spawn frames.");
        if (unresolved.Count > 0)
            Debug.LogWarning("SmarpodBaseReimporter: these references pointed to objects that no longer " +
                             "exist and are now empty (the delta importer recreates spawn frames):\n" +
                             string.Join("\n", unresolved));
    }

    private static void CollectRefs(Component comp, HashSet<Transform> oldTransforms, List<SceneRef> refs)
    {
        var so = new SerializedObject(comp);
        var it = so.GetIterator();
        while (it.Next(true))
        {
            if (it.propertyType != SerializedPropertyType.ObjectReference || it.objectReferenceValue == null)
                continue;
            UnityEngine.Object value = it.objectReferenceValue;
            Transform t = value is GameObject g ? g.transform : (value as Component)?.transform;
            if (t != null && oldTransforms.Contains(t))
                refs.Add(new SceneRef
                {
                    owner = comp,
                    propertyPath = it.propertyPath,
                    targetName = t.name,
                    targetType = value.GetType(),
                });
        }
    }

    /// <summary>Same-named object of the new subtree, links first (a link and
    /// a mesh child can share a name).</summary>
    private static UnityEngine.Object FindByName(Transform root, string name, Type type)
    {
        var candidates = root.GetComponentsInChildren<Transform>(true)
            .Where(t => t.name == name)
            .OrderBy(t => t.GetComponent<UrdfLink>() != null ? 0 : 1);
        foreach (var t in candidates)
        {
            if (type == typeof(GameObject))
                return t.gameObject;
            Component c = t.GetComponent(type);
            if (c != null)
                return c;
        }
        return null;
    }

    /// <summary>The old base came from an older URDF; report links whose
    /// world pose differs so a moved station is noticed.</summary>
    private static void LogPoseChanges(List<Transform> oldLinks, Dictionary<string, GameObject> created)
    {
        var moved = new List<string>();
        foreach (var old in oldLinks)
        {
            if (!created.TryGetValue(old.name, out var go))
                continue;
            float dp = Vector3.Distance(old.position, go.transform.position);
            float da = Quaternion.Angle(old.rotation, go.transform.rotation);
            if (dp > 1e-5f || da > 0.01f)
                moved.Add($"{old.name}: {dp * 1000f:F3} mm, {da:F3} deg");
        }
        if (moved.Count > 0)
            Debug.Log("SmarpodBaseReimporter: world pose changed vs. the old base:\n" + string.Join("\n", moved));
    }

    private static void Fail(string message)
    {
        Debug.LogError("SmarpodBaseReimporter: " + message);
        EditorUtility.DisplayDialog(UndoName, message, "OK");
    }
}
