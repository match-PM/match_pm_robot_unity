using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Threading;
using UnityEngine;
using ROS2;

using Dispense2KReq = pm_skills_interfaces.srv.DispensePathUnity_Request;
using Dispense2KResp = pm_skills_interfaces.srv.DispensePathUnity_Response;

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
    private IPublisher<std_msgs.msg.Bool> donePublisher;

    // Name of the frame the dispense path starts at, taken from the service request.
    // When the run finishes, the adhesive bead is re-parented to this frame's parent
    // (the component) so it travels with it. Written from the ROS2 executor thread
    // (service callback), read on the Unity main thread; a plain reference assignment
    // is atomic and we only need the latest value.
    private volatile string attachFrameName;

    // Set to 1 while a G-code run is in progress. Used to reject overlapping
    // requests and to gate the completion signal. Accessed from both the ROS2
    // executor thread (service callback) and the Unity main thread (coroutine),
    // so it is guarded with Interlocked.
    private int gCodeRunning = 0;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    // Per-axis interpolation state
    private float currentTargetX, currentTargetY, currentTargetZ;
    private float goalTargetX, goalTargetY, goalTargetZ;
    private float speedX, speedY, speedZ; // m/s per axis
    private bool isInterpolating = false;

    // Bead visualization state. Each dispense run builds its OWN bead GameObject +
    // mesh (created in StartNewBead) so previously dispensed beads are left in the
    // scene attached to their objects instead of being overwritten. These fields only
    // ever refer to the bead of the run currently in progress.
    private readonly List<Vector3> beadPoints = new List<Vector3>();
    private Mesh beadMesh;
    private MeshFilter beadMeshFilter;
    private GameObject beadObj;
    private bool isDispensing = false;

    // Shared material instance for all beads, resolved once in Start().
    private Material beadMaterialInstance;
    // Incrementing counter to give each bead a unique, identifiable name.
    private int beadRunCounter = 0;

    // Every bead GameObject ever created this session, so they can all be destroyed
    // when the assembly scene is cleared. Beads deliberately live at the scene root
    // (outside the "spawned" hierarchy) so they survive gripping/re-parenting; nothing
    // else tears them down, so we track them here and clear them on a true scene clear.
    private static readonly List<GameObject> AllBeads = new List<GameObject>();

    // Assembly scene subscription used solely to detect a scene clear. clear_scene
    // publishes an empty objects_in_scene; gripping/re-parenting never empties it, so an
    // empty list is a reliable "wipe everything" signal (same one UnityComponentSpawner
    // uses). Set from the ROS2 executor thread, consumed on the Unity main thread.
    private const string ObjectSceneTopic = "assembly_manager/scene";
    private ISubscription<assembly_manager_interfaces.msg.ObjectScene> objectSceneSub;
    private volatile bool clearBeadsRequested = false;

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

        // Resolve the bead material once. Each run's bead GameObject shares this
        // instance; the actual GameObjects are created per-run in StartNewBead().
        if (adhesiveMaterial != null)
        {
            beadMaterialInstance = adhesiveMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            beadMaterialInstance = new Material(shader) { color = new Color(0.9f, 0.6f, 0.1f, 1f) };
        }
    }

    // Create a brand-new bead GameObject + mesh for the run about to start. The
    // previous run's bead (if any) is left untouched in the scene, attached to its
    // object, so multiple adhesive beads accumulate instead of overwriting each other.
    private void StartNewBead()
    {
        beadRunCounter++;
        beadObj = new GameObject($"AdhesiveBead_{beadRunCounter}");
        beadObj.transform.position = Vector3.zero;
        beadObj.transform.rotation = Quaternion.identity;
        beadObj.transform.localScale = Vector3.one;

        beadMeshFilter = beadObj.AddComponent<MeshFilter>();
        var meshRenderer = beadObj.AddComponent<MeshRenderer>();
        meshRenderer.material = beadMaterialInstance;

        beadMesh = new Mesh { name = $"AdhesiveBeadMesh_{beadRunCounter}" };
        beadMeshFilter.mesh = beadMesh;

        // Track the bead so ClearAllBeads() can destroy it on a scene clear.
        AllBeads.Add(beadObj);

        beadPoints.Clear();
    }

    // Destroy every bead in the scene. Called on the Unity main thread when the
    // assembly scene is cleared, so previously dispensed beads do not linger after
    // cleanup. Also resets the in-progress run's references (the mesh may have just
    // been destroyed); RebuildBeadMesh guards against a null mesh.
    private void ClearAllBeads()
    {
        foreach (GameObject bead in AllBeads)
        {
            if (bead != null)
                Destroy(bead);
        }
        AllBeads.Clear();

        beadObj = null;
        beadMesh = null;
        beadMeshFilter = null;
        beadPoints.Clear();
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

        // Clear beads on a true scene clear (empty objects_in_scene). Done here on the
        // main thread since the flag is set from the ROS2 executor thread.
        if (clearBeadsRequested)
        {
            clearBeadsRequested = false;
            ClearAllBeads();
            Debug.Log("Dispense2K: assembly scene cleared — removed all adhesive beads.");
        }

        if (ros2Unity != null && ros2Unity.Ok())
        {
            if (ros2Node == null)
            {
                ros2Node = ros2Unity.CreateNode("unity_skills");
                srvDispense2K = ros2Node.CreateService<Dispense2KReq, Dispense2KResp>(
                    "~/dispense_2k_unity", Dispense2KServiceCallback);
                donePublisher = ros2Node.CreatePublisher<std_msgs.msg.Bool>("~/dispense_2k_done");
                // Watch the assembly scene so beads are torn down when it is cleared.
                objectSceneSub = ros2Node.CreateSubscription<assembly_manager_interfaces.msg.ObjectScene>(
                    ObjectSceneTopic, ObjectSceneCallback);
                Debug.Log("Dispense2K: ROS2 service '~/dispense_2k_unity' and topic '~/dispense_2k_done' registered.");
            }
        }
    }

    // Runs on the ROS2 executor thread. An empty objects_in_scene means the scene was
    // cleared (gripping/re-parenting keeps the objects, only clear_scene empties the
    // list), so flag the beads for removal on the next main-thread Update().
    private void ObjectSceneCallback(assembly_manager_interfaces.msg.ObjectScene msg)
    {
        if (msg.Objects_in_scene.Length == 0)
            clearBeadsRequested = true;
    }

    // Runs on the ROS2 executor thread. It must return quickly — blocking here
    // stalls the whole executor (it holds the spin mutex) and deadlocks Unity.
    // So this only validates and kicks off the coroutine, then returns an
    // "accepted" ack immediately. Actual completion is signalled asynchronously
    // by publishing 'true' on '~/dispense_2k_done' when the motion finishes.
    private Dispense2KResp Dispense2KServiceCallback(Dispense2KReq req)
    {
        // Reject if a run is already in progress (atomic 0 -> 1 transition).
        if (Interlocked.CompareExchange(ref gCodeRunning, 1, 0) != 0)
        {
            Debug.LogWarning("Dispense2K: dispense already in progress — request rejected.");
            return new Dispense2KResp { Success = false };
        }

        // Frame the dispense path starts at. The bead is attached to this frame's
        // parent component when the run finishes (see AttachBeadToFrameParent).
        attachFrameName = req.Start_frame;

        string packagePath = configureRobot.GetROS2PackagePath("pm_skills");
        if (string.IsNullOrEmpty(packagePath))
        {
            Debug.LogError("Dispense2K: Could not resolve package path for 'pm_skills'.");
            Interlocked.Exchange(ref gCodeRunning, 0);
            return new Dispense2KResp { Success = false };
        }

        string filePath = Path.Combine(packagePath, "share", "pm_skills", "example_g_code.pmdispp.g");
        if (!File.Exists(filePath))
        {
            Debug.LogError($"Dispense2K: G-code file not found at '{filePath}'.");
            Interlocked.Exchange(ref gCodeRunning, 0);
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

        // Create a fresh bead GameObject + mesh for this run. The previous run's bead
        // stays in the scene attached to its object. The new bead sits at world
        // origin/identity while it is drawn, because its mesh is built in world coords.
        StartNewBead();

        try
        {
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

        // Attach the finished bead to the component that owns the start frame, so it
        // moves with the component from here on. pm_skills sends the start frame name;
        // in Unity that frame is a child GameObject of its component, so we parent the
        // bead to the frame's parent. worldPositionStays=true keeps the world-space
        // mesh in place while binding it to the component's transform.
        AttachBeadToFrameParent();
        }
        finally
        {
            // Runs on the Unity main thread when the coroutine ends (normally,
            // via exception, or if stopped). Signal completion to the caller and
            // clear the running flag so the next request is accepted.
            isDispensing = false;
            Interlocked.Exchange(ref gCodeRunning, 0);
            donePublisher?.Publish(new std_msgs.msg.Bool { Data = true });
        }
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
        BeginMove(cmd.x, cmd.y, cmd.z, cmd.fx, cmd.fy, cmd.fz, cmd.moveTime);

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

        // Move back up with up speeds. startX/Y/Z are already in Unity units.
        float reqXu = cmd.fxu != 0f ? Mathf.Abs(cmd.fxu) * mmToUnity : speed;
        float reqYu = cmd.fyu != 0f ? Mathf.Abs(cmd.fyu) * mmToUnity : speed;
        float reqZu = cmd.fzu != 0f ? Mathf.Abs(cmd.fzu) * mmToUnity : speed;
        StartSyncMove(startX, startY, startZ, reqXu, reqYu, reqZu, 0f);

        yield return new WaitUntil(() => !isInterpolating && AxesAtTarget(startX, startY, startZ));
    }

    // Set goal (mm) and per-axis feed rates (mm/s, signed — only magnitude matters;
    // 0 = use default), then start a synchronized move. If moveTimeMs > 0 it forces
    // the total move duration (e.g. G20 MT), otherwise the slowest axis sets the pace.
    private void BeginMove(float x, float y, float z, float fx, float fy, float fz, float moveTimeMs = 0f)
    {
        float reqX = fx != 0f ? Mathf.Abs(fx) * mmToUnity : speed;
        float reqY = fy != 0f ? Mathf.Abs(fy) * mmToUnity : speed;
        float reqZ = fz != 0f ? Mathf.Abs(fz) * mmToUnity : speed;
        StartSyncMove(x * mmToUnity, y * mmToUnity, z * mmToUnity, reqX, reqY, reqZ, moveTimeMs / 1000f);
    }

    // Set goal (Unity units) and per-axis requested speeds (Unity units/s), then rescale
    // every axis so they all reach their goal at the same instant — this keeps the axes
    // synchronized. If durationSec > 0 it forces the total move time; otherwise the axis
    // that would take longest at its requested speed dictates the shared duration and the
    // others are slowed to match (no axis ever exceeds its requested speed).
    private void StartSyncMove(float goalX, float goalY, float goalZ,
                               float reqX, float reqY, float reqZ, float durationSec)
    {
        goalTargetX = goalX;
        goalTargetY = goalY;
        goalTargetZ = goalZ;

        float distX = Mathf.Abs(goalTargetX - currentTargetX);
        float distY = Mathf.Abs(goalTargetY - currentTargetY);
        float distZ = Mathf.Abs(goalTargetZ - currentTargetZ);

        float duration = durationSec;
        if (duration <= 0f)
        {
            float tX = reqX > 0f ? distX / reqX : 0f;
            float tY = reqY > 0f ? distY / reqY : 0f;
            float tZ = reqZ > 0f ? distZ / reqZ : 0f;
            duration = Mathf.Max(tX, Mathf.Max(tY, tZ));
        }

        if (duration > 1e-6f)
        {
            speedX = distX / duration;
            speedY = distY / duration;
            speedZ = distZ / duration;
        }
        else
        {
            speedX = speedY = speedZ = 0f;
        }

        isInterpolating = true;
    }

    private bool AxesAtTarget(float tx, float ty, float tz)
    {
        bool xOk = AxisX == null || Mathf.Abs(AxisX.jointPosition[0] - tx) < positionTolerance;
        bool yOk = AxisY == null || Mathf.Abs(AxisY.jointPosition[0] - ty) < positionTolerance;
        bool zOk = AxisZ == null || Mathf.Abs(AxisZ.jointPosition[0] - tz) < positionTolerance;
        return xOk && yOk && zOk;
    }

    // Parent the adhesive bead to the component that owns the start frame. Runs on the
    // Unity main thread (from the coroutine) so GameObject.Find is safe.
    private void AttachBeadToFrameParent()
    {
        if (beadObj == null) return;

        string frameName = attachFrameName;
        if (string.IsNullOrEmpty(frameName))
        {
            Debug.LogWarning("Dispense2K: no start frame received — adhesive bead left in world space.");
            return;
        }

        GameObject frameObj = GameObject.Find(frameName);
        if (frameObj == null)
        {
            Debug.LogWarning($"Dispense2K: start frame '{frameName}' not found in scene — adhesive bead left in world space.");
            return;
        }

        // The frame is a child of its component; follow that component. Fall back to
        // the frame itself if it has no parent.
        Transform target = frameObj.transform.parent != null ? frameObj.transform.parent : frameObj.transform;

        // Do NOT parent the bead into the spawned-object hierarchy. UnityComponentSpawner
        // destroys and recreates every "spawned" object whenever the assembly scene
        // changes (e.g. when the component is gripped and re-parented), which would
        // destroy a child bead along with it. Instead the bead stays at the scene root
        // and follows the target by name, re-binding after each rebuild. See BeadFollower.
        var follower = beadObj.GetComponent<BeadFollower>() ?? beadObj.AddComponent<BeadFollower>();
        follower.targetName = target.name;
        follower.localPosition = target.InverseTransformPoint(beadObj.transform.position);
        follower.localRotation = Quaternion.Inverse(target.rotation) * beadObj.transform.rotation;
        Debug.Log($"Dispense2K: adhesive bead now following '{target.name}' (parent of frame '{frameName}').");
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
        // Explicitly refresh bounds so the renderer is not frustum-culled and stays
        // visible after the bead is re-parented to a moving object.
        beadMesh.RecalculateBounds();
    }
}
