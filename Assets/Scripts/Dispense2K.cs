using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;
using ROS2;

using Dispense2KReq = pm_msgs.srv.EmptyWithSuccess_Request;
using Dispense2KResp = pm_msgs.srv.EmptyWithSuccess_Response;

public class Dispense2K : MonoBehaviour
{
    public ArticulationBody AxisX;
    public ArticulationBody AxisY;
    public ArticulationBody AxisZ;
    public ArticulationBody Dispenser;

    // G-code values are in mm; Unity uses meters
    public float mmToUnity = 0.001f;
    // Default movement speed in m/s (used when F=0 in G-code)
    public float speed = 0.01f;
    // Tolerance in Unity units to consider an axis at its target
    public float positionTolerance = 0.0005f;

    [Header("Adhesive Bead Visualization")]
    public Transform DispenserTip;
    public Material adhesiveMaterial;
    public float beadRadius = 0.001f;
    public float beadSampleDistance = 0.0005f;
    public int tubeSegments = 8;

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<Dispense2KReq, Dispense2KResp> srvDispense2K;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    // Target interpolation state
    private float currentTargetX, currentTargetY, currentTargetZ;
    private float goalTargetX, goalTargetY, goalTargetZ;
    private float interpolationSpeed;
    private bool isInterpolating = false;

    // Bead visualization state
    private readonly List<Vector3> beadPoints = new List<Vector3>();
    private Mesh beadMesh;
    private MeshFilter beadMeshFilter;
    private bool isDispensing = false;

    private struct GCodeCommand
    {
        public int type; // 10 = move with dispenser on, 20 = move only
        public float x, y, z;
        public float f; // feed rate in mm/min (0 = use default speed)
    }

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();

        if (DispenserTip == null && Dispenser != null)
        {
            DispenserTip = Dispenser.transform;
            Debug.LogWarning("Dispense2K: DispenserTip not assigned — using Dispenser transform as fallback.");
        }
        if (DispenserTip == null)
            Debug.LogError("Dispense2K: No DispenserTip and no Dispenser assigned. Adhesive bead will not work.");

        var beadObj = new GameObject("AdhesiveBead");
        beadObj.transform.position = Vector3.zero;
        beadObj.transform.rotation = Quaternion.identity;

        beadMeshFilter = beadObj.AddComponent<MeshFilter>();
        var meshRenderer = beadObj.AddComponent<MeshRenderer>();

        if (adhesiveMaterial != null)
        {
            meshRenderer.material = adhesiveMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            meshRenderer.material = new Material(shader) { color = new Color(0.9f, 0.6f, 0.1f, 1f) };
        }

        beadMesh = new Mesh { name = "AdhesiveBeadMesh" };
        beadMeshFilter.mesh = beadMesh;
    }

    void FixedUpdate()
    {
        if (!isInterpolating) return;

        float dt = Time.fixedDeltaTime;

        float dx = goalTargetX - currentTargetX;
        float dy = goalTargetY - currentTargetY;
        float dz = goalTargetZ - currentTargetZ;
        float totalDistance = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

        if (totalDistance < 1e-7f)
        {
            currentTargetX = goalTargetX;
            currentTargetY = goalTargetY;
            currentTargetZ = goalTargetZ;
            isInterpolating = false;
        }
        else
        {
            float stepDistance = interpolationSpeed * dt;
            if (stepDistance >= totalDistance)
            {
                currentTargetX = goalTargetX;
                currentTargetY = goalTargetY;
                currentTargetZ = goalTargetZ;
                isInterpolating = false;
            }
            else
            {
                float ratio = stepDistance / totalDistance;
                currentTargetX += dx * ratio;
                currentTargetY += dy * ratio;
                currentTargetZ += dz * ratio;
            }
        }

        if (AxisX != null) AxisX.SetDriveTarget(ArticulationDriveAxis.X, currentTargetX);
        if (AxisY != null) AxisY.SetDriveTarget(ArticulationDriveAxis.X, currentTargetY);
        if (AxisZ != null) AxisZ.SetDriveTarget(ArticulationDriveAxis.X, currentTargetZ);

        if (isDispensing && DispenserTip != null)
        {
            Vector3 pos = DispenserTip.position;
            if (beadPoints.Count == 0 || Vector3.Distance(pos, beadPoints[beadPoints.Count - 1]) >= beadSampleDistance)
            {
                beadPoints.Add(pos);
                RebuildBeadMesh();
            }
        }
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action?.Invoke();

        if (ros2Unity != null && ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("unity_skills");
                srvDispense2K = ros2Node.CreateService<Dispense2KReq, Dispense2KResp>(
                    "~/dispense_2k_unity", Dispense2KServiceCallback);
                Debug.Log("Dispense2K: ROS2 service '~/dispense_2k_unity' registered.");
            }
        }
    }

    private Dispense2KResp Dispense2KServiceCallback(Dispense2KReq _)
    {
        string packagePath = configureRobot.GetROS2PackagePath("pm_skills");
        if (string.IsNullOrEmpty(packagePath))
        {
            Debug.LogError("Dispense2K: Could not resolve package path for 'pm_skills'.");
            return new Dispense2KResp { Success = false };
        }

        string filePath = Path.Combine(packagePath, "share", "pm_skills", "example_g_code.pmdispp.g");
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Dispense2K: G-code file not found at '{filePath}'.");
            return new Dispense2KResp { Success = false };
        }

        string[] lines = File.ReadAllLines(filePath);
        List<GCodeCommand> commands = ParseGCode(lines);
        Debug.Log($"Dispense2K: Loaded {commands.Count} commands from '{filePath}'.");

        mainThreadActions.Enqueue(() => StartCoroutine(ExecuteGCode(commands)));

        return new Dispense2KResp { Success = true };
    }

    private List<GCodeCommand> ParseGCode(string[] lines)
    {
        var commands = new List<GCodeCommand>();

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            string[] parts = trimmed.Split(' ');
            int type;
            if      (parts[0] == "G20") type = 20;
            else if (parts[0] == "G10") type = 10;
            else if (parts[0] == "G30") type = 30;
            else continue;

            float x = 0f, y = 0f, z = 0f, f = 0f;
            foreach (string part in parts)
            {
                if (part.Length < 2) continue;
                if (!float.TryParse(part.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    continue;

                switch (part[0])
                {
                    case 'X': x = val; break;
                    case 'Y': y = val; break;
                    case 'Z': z = val; break;
                    case 'F': f = val; break;
                }
            }

            commands.Add(new GCodeCommand { type = type, x = x, y = y, z = z, f = f });
        }

        return commands;
    }

    private IEnumerator ExecuteGCode(List<GCodeCommand> commands)
    {
        if (AxisX != null) currentTargetX = AxisX.jointPosition[0];
        if (AxisY != null) currentTargetY = AxisY.jointPosition[0];
        if (AxisZ != null) currentTargetZ = AxisZ.jointPosition[0];

        beadPoints.Clear();
        if (beadMesh != null) beadMesh.Clear();

        foreach (var cmd in commands)
        {
            isDispensing = cmd.type == 10;

            float moveSpeed = cmd.f > 0f ? cmd.f / 60000f : speed; // mm/min → m/s
            // if (Dispenser != null) Dispenser.SetDriveTarget(ArticulationDriveAxis.X, isDispensing ? 1f : 0f);

            goalTargetX = cmd.x * mmToUnity;
            goalTargetY = cmd.y * mmToUnity;
            goalTargetZ = cmd.z * mmToUnity;
            interpolationSpeed = moveSpeed;
            isInterpolating = true;

            Debug.Log($"Dispense2K: G{cmd.type} → X={cmd.x} Y={cmd.y} Z={cmd.z} F={moveSpeed * 60000f} mm/min dispense={isDispensing}");

            yield return new WaitUntil(() => !isInterpolating && AxesAtTarget(cmd));
        }

        isDispensing = false;
        // if (Dispenser != null) Dispenser.SetDriveTarget(ArticulationDriveAxis.X, 0f);

        Debug.Log("Dispense2K: G-code execution complete.");
    }

    private bool AxesAtTarget(GCodeCommand cmd)
    {
        bool xOk = AxisX == null || Mathf.Abs(AxisX.jointPosition[0] - cmd.x * mmToUnity) < positionTolerance;
        bool yOk = AxisY == null || Mathf.Abs(AxisY.jointPosition[0] - cmd.y * mmToUnity) < positionTolerance;
        bool zOk = AxisZ == null || Mathf.Abs(AxisZ.jointPosition[0] - cmd.z * mmToUnity) < positionTolerance;
        return xOk && yOk && zOk;
    }

    private void RebuildBeadMesh()
    {
        if (beadMesh == null || beadPoints.Count < 2) return;

        int n = beadPoints.Count;
        int seg = tubeSegments;
        var vertices = new Vector3[n * seg];
        var triangles = new int[(n - 1) * seg * 6];

        for (int i = 0; i < n; i++)
        {
            // Direction along the path
            Vector3 fwd;
            if (i < n - 1)
                fwd = (beadPoints[i + 1] - beadPoints[i]).normalized;
            else
                fwd = (beadPoints[i] - beadPoints[i - 1]).normalized;

            // Build a perpendicular frame
            Vector3 side = Vector3.Cross(fwd, Vector3.up);
            if (side.sqrMagnitude < 0.001f)
                side = Vector3.Cross(fwd, Vector3.right);
            side.Normalize();
            Vector3 up = Vector3.Cross(side, fwd).normalized;

            for (int j = 0; j < seg; j++)
            {
                float angle = (float)j / seg * Mathf.PI * 2f;
                Vector3 offset = (Mathf.Cos(angle) * side + Mathf.Sin(angle) * up) * beadRadius;
                vertices[i * seg + j] = beadPoints[i] + offset;
            }
        }

        int ti = 0;
        for (int i = 0; i < n - 1; i++)
        {
            for (int j = 0; j < seg; j++)
            {
                int c0 = i * seg + j;
                int c1 = i * seg + (j + 1) % seg;
                int n0 = (i + 1) * seg + j;
                int n1 = (i + 1) * seg + (j + 1) % seg;

                triangles[ti++] = c0;
                triangles[ti++] = n0;
                triangles[ti++] = c1;

                triangles[ti++] = c1;
                triangles[ti++] = n0;
                triangles[ti++] = n1;
            }
        }

        beadMesh.Clear();
        beadMesh.vertices = vertices;
        beadMesh.triangles = triangles;
        beadMesh.RecalculateNormals();
    }
}
