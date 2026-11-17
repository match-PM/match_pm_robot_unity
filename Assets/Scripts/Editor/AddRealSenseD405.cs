using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// "PM Robot/Add RealSense D405 to Z Axis": creates a simulated D405
/// (Camera + RealSenseD405) as a child of the Z axis so it moves with the head, and
/// places it with "Place RealSense D405 to view robot head".
///
/// The placement is a placeholder until the real bracket is known: the camera is put
/// outside the robot head (everything below the Z axis), on the side facing the
/// Main Camera and slightly above, far enough that the whole head fits into the
/// D405 field of view, looking at the head's center. The depth range of the camera
/// is widened if the head would otherwise lie partly beyond it.
/// </summary>
public static class AddRealSenseD405
{
    private const string CameraName = "D405_Camera";
    // Z_Axis is renamed for the OPC UA interface (see PM_Robot/Configs/OpcUaAxisNames.json).
    private static readonly string[] ZAxisNames = { "RobotAxisZ", "Z_Axis" };
    private const float ElevationDeg = 25f; // look slightly down onto the head
    private const float Margin = 1.1f;

    [MenuItem("PM Robot/Add RealSense D405 to Z Axis")]
    public static void Add()
    {
        Transform zAxis = FindZAxis();
        if (zAxis == null)
            return;

        Transform existing = zAxis.Find(CameraName);
        if (existing != null)
        {
            Debug.Log($"AddRealSenseD405: '{CameraName}' already exists under '{zAxis.name}', re-placing it.");
            Place(existing, zAxis);
            return;
        }

        var go = new GameObject(CameraName);
        Undo.RegisterCreatedObjectUndo(go, "Add RealSense D405");
        go.transform.SetParent(zAxis, false);

        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.enabled = false; // rendered explicitly by RealSenseD405
        go.AddComponent<ROS2.RealSenseD405>();

        Place(go.transform, zAxis);
        Debug.Log($"AddRealSenseD405: created '{CameraName}' under '{zAxis.name}'. Save the scene to keep it.");
    }

    [MenuItem("PM Robot/Place RealSense D405 to view robot head")]
    public static void PlaceMenu()
    {
        Transform zAxis = FindZAxis();
        if (zAxis == null)
            return;
        Transform camera = zAxis.Find(CameraName);
        if (camera == null)
        {
            EditorUtility.DisplayDialog("Place RealSense D405",
                $"No '{CameraName}' under '{zAxis.name}'. Use 'PM Robot/Add RealSense D405 to Z Axis' first.", "OK");
            return;
        }
        Place(camera, zAxis);
    }

    private static void Place(Transform camera, Transform zAxis)
    {
        var d405 = camera.GetComponent<ROS2.RealSenseD405>();
        var cam = camera.GetComponent<Camera>();

        if (!TryGetHeadBounds(zAxis, camera, out Bounds bounds))
        {
            Debug.LogWarning("AddRealSenseD405: no visible meshes below the Z axis, camera not moved.");
            return;
        }

        // Side of the head the Main Camera looks from, horizontal only.
        Vector3 toViewer = Vector3.back;
        Camera main = Camera.main;
        if (main != null)
        {
            Vector3 v = main.transform.position - bounds.center;
            v.y = 0f;
            if (v.sqrMagnitude > 1e-6f)
                toViewer = v.normalized;
        }
        Vector3 direction = Quaternion.AngleAxis(ElevationDeg, Vector3.Cross(toViewer, Vector3.up)) * toViewer;

        // Distance so the bounding sphere fits into the smaller (vertical) FOV.
        float hfov = d405 != null ? d405.horizontalFovDeg : 87f;
        float aspect = d405 != null ? (float)d405.width / d405.height : 848f / 480f;
        float vfov = 2f * Mathf.Atan(Mathf.Tan(hfov * 0.5f * Mathf.Deg2Rad) / aspect) * Mathf.Rad2Deg;
        float radius = bounds.extents.magnitude * Margin;
        float distance = radius / Mathf.Sin(Mathf.Min(hfov, vfov) * 0.5f * Mathf.Deg2Rad);

        Undo.RecordObject(camera, "Place RealSense D405");
        camera.position = bounds.center + direction * distance;
        camera.LookAt(bounds.center, Vector3.up);

        float farthest = distance + radius;
        if (d405 != null && d405.maxDepth < farthest)
        {
            Undo.RecordObject(d405, "Place RealSense D405");
            d405.maxDepth = Mathf.Ceil(farthest * 20f) / 20f; // next 5 cm
            Debug.Log($"AddRealSenseD405: maxDepth raised to {d405.maxDepth:F2} m so the whole head has depth " +
                      "(the real D405 is specified for 7-50 cm and gets noisier beyond).");
        }
        if (cam != null)
        {
            Undo.RecordObject(cam, "Place RealSense D405");
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, farthest * 1.5f);
        }

        EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
        Selection.activeTransform = camera;
        Debug.Log($"AddRealSenseD405: placed '{camera.name}' {distance:F2} m from the robot head " +
                  $"(head size {bounds.size.x:F2} x {bounds.size.y:F2} x {bounds.size.z:F2} m), " +
                  $"local position {camera.localPosition.ToString("F3")}. Save the scene to keep it.");
    }

    /// <summary>World bounds of all visible meshes below the Z axis (the robot head).</summary>
    private static bool TryGetHeadBounds(Transform zAxis, Transform exclude, out Bounds bounds)
    {
        bounds = default;
        bool any = false;
        foreach (var r in zAxis.GetComponentsInChildren<Renderer>(false))
        {
            if (!r.enabled || r.transform.IsChildOf(exclude))
                continue;
            if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                continue;
            if (any) bounds.Encapsulate(r.bounds);
            else { bounds = r.bounds; any = true; }
        }
        return any;
    }

    private static Transform FindZAxis()
    {
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (string n in ZAxisNames)
            foreach (var t in all)
                if (t.name == n && t.gameObject.scene.IsValid())
                    return t;
        EditorUtility.DisplayDialog("RealSense D405",
            "No 'RobotAxisZ' / 'Z_Axis' GameObject found in the open scene.", "OK");
        return null;
    }
}
