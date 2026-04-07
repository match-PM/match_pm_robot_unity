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

    // G-code positions are in mm; Unity uses meters
    public float mmToUnity = 0.001f;
    // Default movement speed in m/s (used when per-axis speed is 0)
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

    // Per-axis interpolation state
    private float currentTargetX, currentTargetY, currentTargetZ;
    private float goalTargetX, goalTargetY, goalTargetZ;
    private float speedX, speedY, speedZ; // m/s per axis
    private bool isInterpolating = false;

    // Bead visualization state
    private readonly List<Vector3> beadPoints = new List<Vector3>();
    private Mesh beadMesh;
    private MeshFilter beadMeshFilter;
    private bool isDispensing = false;

    private struct GCodeCommand
    {
        public int type;             // 10=Move, 20=Dispenser, 30=Dip, 40=Wait, 50=SetDispenser
        public float x, y, z;       // target position (mm)
        public float fx, fy, fz;    // move/dispenser speeds (mm/s)
        public float fxd, fyd, fzd; // G30 down speeds (mm/s)
        public float fxu, fyu, fzu; // G30 up speeds (mm/s)
        public float moveTime;      // G20: total move duration (ms)
        public float turnOffTime;   // G20: turn off dispenser this many ms before end
        public float t;             // G30: dip rest time (ms), G40: wait time (ms)
        public float a;             // G50: dispenser activation (0=off, 1=on)
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

        bool xDone = StepAxis(ref currentTargetX, goalTargetX, speedX, dt);
        bool yDone = StepAxis(ref currentTargetY, goalTargetY, speedY, dt);
        bool zDone = StepAxis(ref currentTargetZ, goalTargetZ, speedZ, dt);

        if (AxisX != null) AxisX.SetDriveTarget(ArticulationDriveAxis.X, currentTargetX);
        if (AxisY != null) AxisY.SetDriveTarget(ArticulationDriveAxis.X, currentTargetY);
        if (AxisZ != null) AxisZ.SetDriveTarget(ArticulationDriveAxis.X, currentTargetZ);

        if (xDone && yDone && zDone)
            isInterpolating = false;

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

    // Returns true when the axis has reached its goal.
    private bool StepAxis(ref float current, float goal, float axisSpeed, float dt)
    {
        float diff = goal - current;
        if (Mathf.Abs(diff) < 1e-7f)
        {
            current = goal;
            return true;
        }
        float step = axisSpeed * dt;
        if (step >= Mathf.Abs(diff))
        {
            current = goal;
            return true;
        }
        current += Mathf.Sign(diff) * step;
        return false;
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
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";")) continue;

            string[] parts = trimmed.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) continue;

            int type;
            if      (parts[0] == "G10") type = 10;
            else if (parts[0] == "G20") type = 20;
            else if (parts[0] == "G30") type = 30;
            else if (parts[0] == "G40") type = 40;
            else if (parts[0] == "G50") type = 50;
            else continue;

            var cmd = new GCodeCommand { type = type };

            foreach (string part in parts)
            {
                if (part.Length < 2) continue;

                // Resolve multi-character keys (longest match first)
                if (part.Length > 3 && TryMatchKey(part, 3, out string key, out string valueStr)) { }
                else if (part.Length > 2 && TryMatchKey(part, 2, out key, out valueStr)) { }
                else { key = part.Substring(0, 1).ToUpperInvariant(); valueStr = part.Substring(1); }

                if (!float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                    continue;

                switch (key)
                {
                    case "X":   cmd.x          = val; break;
                    case "Y":   cmd.y          = val; break;
                    case "Z":   cmd.z          = val; break;
                    case "FX":  cmd.fx         = val; break;
                    case "FY":  cmd.fy         = val; break;
                    case "FZ":  cmd.fz         = val; break;
                    case "FXD": cmd.fxd        = val; break;
                    case "FYD": cmd.fyd        = val; break;
                    case "FZD": cmd.fzd        = val; break;
                    case "FXU": cmd.fxu        = val; break;
                    case "FYU": cmd.fyu        = val; break;
                    case "FZU": cmd.fzu        = val; break;
                    case "MT":  cmd.moveTime   = val; break;
                    case "TF":  cmd.turnOffTime= val; break;
                    case "T":   cmd.t          = val; break;
                    case "A":   cmd.a          = val; break;
                }
            }

            commands.Add(cmd);
        }

        return commands;
    }

    private static bool TryMatchKey(string part, int keyLen, out string key, out string valueStr)
    {
        key = part.Substring(0, keyLen).ToUpperInvariant();
        valueStr = part.Substring(keyLen);

        if (keyLen == 3)
            return key == "FXD" || key == "FYD" || key == "FZD" || key == "FXU" || key == "FYU" || key == "FZU";
        if (keyLen == 2)
            return key == "FX" || key == "FY" || key == "FZ" || key == "MT" || key == "TF";

        return false;
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
            switch (cmd.type)
            {
                case 10: // G10 - Move
                    Debug.Log($"Dispense2K: G10 → X={cmd.x} Y={cmd.y} Z={cmd.z} FX={cmd.fx} FY={cmd.fy} FZ={cmd.fz} mm/s");
                    yield return StartCoroutine(DoMove(cmd.x, cmd.y, cmd.z, cmd.fx, cmd.fy, cmd.fz));
                    break;

                case 20: // G20 - Dispenser (move while dispensing)
                    Debug.Log($"Dispense2K: G20 → X={cmd.x} Y={cmd.y} Z={cmd.z} FX={cmd.fx} FY={cmd.fy} FZ={cmd.fz} MT={cmd.moveTime} TF={cmd.turnOffTime}");
                    yield return StartCoroutine(DoDispenseMove(cmd));
                    break;

                case 30: // G30 - Dip
                    Debug.Log($"Dispense2K: G30 → X={cmd.x} Y={cmd.y} Z={cmd.z} T={cmd.t}ms");
                    yield return StartCoroutine(DoDip(cmd));
                    break;

                case 40: // G40 - Wait
                    Debug.Log($"Dispense2K: G40 → wait {cmd.t} ms");
                    yield return new WaitForSeconds(cmd.t / 1000f);
                    break;

                case 50: // G50 - Set Dispenser (A0=off, A1=on)
                    isDispensing = cmd.a != 0f;
                    Debug.Log($"Dispense2K: G50 A{cmd.a} → dispenser {(isDispensing ? "ON" : "OFF")}");
                    break;
            }
        }

        isDispensing = false;
        Debug.Log("Dispense2K: G-code execution complete.");
    }

    // G10 - Move to target, no dispensing.
    private IEnumerator DoMove(float x, float y, float z, float fx, float fy, float fz)
    {
        BeginMove(x, y, z, fx, fy, fz);
        yield return new WaitUntil(() => !isInterpolating && AxesAtTarget(goalTargetX, goalTargetY, goalTargetZ));
    }

    // G20 - Move to target while dispensing. Turn off dispenser (moveTime - turnOffTime) ms into the move.
    private IEnumerator DoDispenseMove(GCodeCommand cmd)
    {
        isDispensing = true;
        BeginMove(cmd.x, cmd.y, cmd.z, cmd.fx, cmd.fy, cmd.fz);

        // Turn off dispenser early if timing parameters are provided
        if (cmd.moveTime > 0f)
        {
            float waitSeconds = (cmd.moveTime - cmd.turnOffTime) / 1000f;
            if (waitSeconds > 0f)
                yield return new WaitForSeconds(waitSeconds);
        }

        isDispensing = false;

        // Wait for axes to reach target
        yield return new WaitUntil(() => !isInterpolating && AxesAtTarget(goalTargetX, goalTargetY, goalTargetZ));
    }

    // G30 - Dip: move down to target, wait rest time, return to start position.
    private IEnumerator DoDip(GCodeCommand cmd)
    {
        float startX = currentTargetX;
        float startY = currentTargetY;
        float startZ = currentTargetZ;

        // Move down with down speeds
        BeginMove(cmd.x, cmd.y, cmd.z, cmd.fxd, cmd.fyd, cmd.fzd);
        yield return new WaitUntil(() => !isInterpolating && AxesAtTarget(goalTargetX, goalTargetY, goalTargetZ));

        // Dip rest
        if (cmd.t > 0f)
            yield return new WaitForSeconds(cmd.t / 1000f);

        // Move back up with up speeds (in Unity units — already converted)
        goalTargetX = startX;
        goalTargetY = startY;
        goalTargetZ = startZ;
        speedX = cmd.fxu > 0f ? cmd.fxu * mmToUnity : speed;
        speedY = cmd.fyu > 0f ? cmd.fyu * mmToUnity : speed;
        speedZ = cmd.fzu > 0f ? cmd.fzu * mmToUnity : speed;
        isInterpolating = true;

        yield return new WaitUntil(() => !isInterpolating && AxesAtTarget(startX, startY, startZ));
    }

    // Set goal and per-axis speeds, start interpolation. Speeds in mm/s (0 = use default).
    private void BeginMove(float x, float y, float z, float fx, float fy, float fz)
    {
        goalTargetX = x * mmToUnity;
        goalTargetY = y * mmToUnity;
        goalTargetZ = z * mmToUnity;
        speedX = fx > 0f ? fx * mmToUnity : speed;
        speedY = fy > 0f ? fy * mmToUnity : speed;
        speedZ = fz > 0f ? fz * mmToUnity : speed;
        isInterpolating = true;
    }

    private bool AxesAtTarget(float tx, float ty, float tz)
    {
        bool xOk = AxisX == null || Mathf.Abs(AxisX.jointPosition[0] - tx) < positionTolerance;
        bool yOk = AxisY == null || Mathf.Abs(AxisY.jointPosition[0] - ty) < positionTolerance;
        bool zOk = AxisZ == null || Mathf.Abs(AxisZ.jointPosition[0] - tz) < positionTolerance;
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
            Vector3 fwd;
            if (i < n - 1)
                fwd = (beadPoints[i + 1] - beadPoints[i]).normalized;
            else
                fwd = (beadPoints[i] - beadPoints[i - 1]).normalized;

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
