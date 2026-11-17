using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using ROS2;

using GetStateReq = pm_msgs.srv.GetSimRobotState_Request;
using GetStateResp = pm_msgs.srv.GetSimRobotState_Response;
using SetStateReq = pm_msgs.srv.SetSimRobotState_Request;
using SetStateResp = pm_msgs.srv.SetSimRobotState_Response;

// =============================================================================
// Reads and sets the complete joint state of the simulation, for the robot
// state services of pm_robot_sim_state (save_robot_state / load_robot_state).
//
//   /unity/get_sim_robot_state  pm_msgs/srv/GetSimRobotState
//   /unity/set_sim_robot_state  pm_msgs/srv/SetSimRobotState
//
// "Joints" are all active single-DOF ArticulationBodies in the scene, keyed by
// their GameObject (URDF link) name; a name that occurs more than once is
// keyed by its hierarchy path instead. Positions are jointPosition, [m] for
// prismatic and [rad] for revolute joints.
//
// A set places every joint directly (no simulated motion): it sets the joint
// position, zeroes the velocity, and sets the drive target to the
// same position so the drive holds it. This covers the robot axes, gonios,
// pneumatic cylinders, gripper jaws and the UV cart: their controllers only
// change a drive target when a new command arrives, so they keep the new
// position. The OPC UA axes also get the new position written as target and
// actual position (RobotAxisControl.OnTeleported). The SmarPod is kinematic
// and is placed through Smarpod.Teleport instead.
//
// The ros2_control motion controllers must be inactive during a set, or they
// command the old positions again; pm_robot_sim_state takes care of that.
//
// The component adds itself to 'pm_robot' when a scene loads, so no scene
// change is needed.
// =============================================================================

public class RobotStateTeleporter : MonoBehaviour
{
    [Header("ROS")]
    public string nodeName = "unity_robot_state";
    public string getServiceName = "/unity/get_sim_robot_state";
    public string setServiceName = "/unity/set_sim_robot_state";

    [Tooltip("How long a service call waits for the next physics step [s]. " +
             "When Unity is paused the call fails after this time without changing anything.")]
    public float mainThreadTimeout = 5f;

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<GetStateReq, GetStateResp> getService;
    private IService<SetStateReq, SetStateResp> setService;
    private chooseMode modeSelection;

    // Service callbacks run on the ROS2ForUnity executor thread; scene access
    // has to happen on the main thread, right before a physics step.
    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToRobot()
    {
        var robot = GameObject.Find("pm_robot");
        if (robot != null && robot.GetComponent<RobotStateTeleporter>() == null)
            robot.AddComponent<RobotStateTeleporter>();
    }

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity == null)
            ros2Unity = FindObjectOfType<ROS2UnityComponent>();
        modeSelection = GetComponent<chooseMode>();
        if (modeSelection == null)
            modeSelection = FindObjectOfType<chooseMode>();
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity != null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode(nodeName);
            getService = ros2Node.CreateService<GetStateReq, GetStateResp>(getServiceName, GetState);
            setService = ros2Node.CreateService<SetStateReq, SetStateResp>(setServiceName, SetState);
            Debug.Log($"RobotStateTeleporter: services '{getServiceName}' and '{setServiceName}' ready.");
        }
    }

    void FixedUpdate()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action?.Invoke();
    }

    // =========================================================================
    // Service callbacks (executor thread)
    // =========================================================================

    private GetStateResp GetState(GetStateReq request)
    {
        var response = new GetStateResp
        {
            Joint_names = new string[0],
            Joint_positions = new double[0],
        };
        string error = RunOnMainThread(() => ReadState(response));
        response.Success = error == null;
        response.Message = error ?? $"Read {response.Joint_names.Length} joints.";
        return response;
    }

    private SetStateResp SetState(SetStateReq request)
    {
        var response = new SetStateResp();
        string summary = null;
        string error = RunOnMainThread(() => ApplyState(request, out summary));
        response.Success = error == null;
        response.Message = error ?? summary;
        if (error != null)
            Debug.LogWarning($"RobotStateTeleporter: set failed: {error}");
        return response;
    }

    // =========================================================================
    // Main-thread work
    // =========================================================================

    private string ReadState(GetStateResp response)
    {
        var joints = CollectJoints(out string error);
        if (joints == null)
            return error;

        response.Joint_names = joints.Keys.ToArray();
        response.Joint_positions = joints.Values.Select(body => (double)body.jointPosition[0]).ToArray();

        var smarpod = FindObjectOfType<Smarpod>();
        response.Has_smarpod = smarpod != null;
        if (smarpod != null)
        {
            var pose = new double[6];
            smarpod.GetSavedState(pose, out double speed);
            Array.Copy(pose, response.Smarpod_pose, 6);
            response.Smarpod_speed = speed;
        }
        return null;
    }

    /// <summary>Places the requested joints; returns an error, or null and a summary.</summary>
    private string ApplyState(SetStateReq request, out string summary)
    {
        summary = null;
        if (modeSelection != null && modeSelection.mode != chooseMode.Mode.Simulation)
            return "Unity is in Visualization mode; the state can only be set in Simulation mode.";

        string[] names = request.Joint_names ?? new string[0];
        double[] positions = request.Joint_positions ?? new double[0];
        if (names.Length != positions.Length)
            return $"{names.Length} joint names but {positions.Length} positions.";

        var joints = CollectJoints(out string error);
        if (joints == null)
            return error;

        // Validate everything before changing anything.
        var unknown = names.Where(name => !joints.ContainsKey(name)).ToList();
        if (unknown.Count > 0)
            return "Unknown joints: " + string.Join(", ", unknown);
        var invalid = names.Where((name, i) => double.IsNaN(positions[i]) || double.IsInfinity(positions[i])).ToList();
        if (invalid.Count > 0)
            return "Non-finite positions for: " + string.Join(", ", invalid);

        Smarpod smarpod = null;
        if (request.Set_smarpod)
        {
            smarpod = FindObjectOfType<Smarpod>();
            if (smarpod == null)
                return "A SmarPod pose was given, but the SmarPod is not active.";
        }

        if (smarpod != null && !smarpod.Teleport(request.Smarpod_pose, out string smarpodError))
            return smarpodError;

        for (int i = 0; i < names.Length; i++)
            PlaceJoint(joints[names[i]], (float)positions[i]);

        summary = $"Placed {names.Length} joints" + (smarpod != null ? " and the SmarPod." : ".");
        return null;
    }

    private static void PlaceJoint(ArticulationBody body, float position)
    {
        body.jointPosition = new ArticulationReducedSpace(position);
        body.jointVelocity = new ArticulationReducedSpace(0f);

        // Drive targets are in [m] for prismatic and [deg] for revolute joints.
        var drive = body.xDrive;
        drive.target = body.jointType == ArticulationJointType.RevoluteJoint
            ? position * Mathf.Rad2Deg
            : position;
        body.xDrive = drive;

        var axisControl = body.GetComponent<RobotAxisControl>();
        if (axisControl != null)
            axisControl.OnTeleported(position);
    }

    /// <summary>Active single-DOF joints by name, without the kinematic SmarPod
    /// links. Duplicate names are replaced by hierarchy paths.</summary>
    private static SortedDictionary<string, ArticulationBody> CollectJoints(out string error)
    {
        error = null;
        var smarpod = FindObjectOfType<Smarpod>();
        var bodies = FindObjectsOfType<ArticulationBody>()
            .Where(body => body.dofCount == 1 && (smarpod == null || !smarpod.OwnsBody(body)))
            .ToList();

        var duplicates = new HashSet<string>(
            bodies.GroupBy(body => body.name).Where(group => group.Count() > 1).Select(group => group.Key));

        var joints = new SortedDictionary<string, ArticulationBody>(StringComparer.Ordinal);
        foreach (var body in bodies)
        {
            string key = duplicates.Contains(body.name) ? HierarchyPath(body.transform) : body.name;
            if (joints.ContainsKey(key))
            {
                error = $"Two active joints have the same hierarchy path '{key}'.";
                return null;
            }
            joints[key] = body;
        }
        return joints;
    }

    private static string HierarchyPath(Transform transform)
    {
        var parts = new List<string>();
        for (var current = transform; current != null; current = current.parent)
            parts.Add(current.name);
        parts.Reverse();
        return string.Join("/", parts);
    }

    // =========================================================================
    // Executor thread -> main thread
    // =========================================================================

    private const int Pending = 0, Running = 1, Cancelled = 2;

    /// <summary>Runs `work` in the next FixedUpdate and waits for its result.
    /// Returns the result of `work`, or an error if it did not run in time; work
    /// that timed out is cancelled, so it can never be applied late.</summary>
    private string RunOnMainThread(Func<string> work)
    {
        int state = Pending;
        string result = null;
        using (var done = new ManualResetEventSlim(false))
        {
            mainThreadActions.Enqueue(() =>
            {
                if (Interlocked.CompareExchange(ref state, Running, Pending) != Pending)
                    return;
                try
                {
                    result = work();
                }
                catch (Exception e)
                {
                    result = $"Unity error: {e.Message}";
                    Debug.LogException(e);
                }
                finally
                {
                    done.Set();
                }
            });

            if (done.Wait(TimeSpan.FromSeconds(mainThreadTimeout)))
                return result;
            if (Interlocked.CompareExchange(ref state, Cancelled, Pending) == Pending)
                return $"Unity did not run a physics step within {mainThreadTimeout:0.#} s (paused?). Nothing was changed.";
            // Already running: it finishes within this physics step.
            done.Wait();
            return result;
        }
    }
}
