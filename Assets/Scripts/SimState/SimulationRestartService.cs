using System.Threading;
using UnityEngine;
using ROS2;

using RestartReq = pm_msgs.srv.RestartSimulation_Request;
using RestartResp = pm_msgs.srv.RestartSimulation_Response;

// =============================================================================
// /unity/restart_simulation  pm_msgs/srv/RestartSimulation
//
// Restarts Play Mode so the scene loads the bringup configuration again (and
// LaunchOpcUaServer starts a fresh OPC UA server). Used by
// pm_robot_sim_state's load_robot_state when a saved state needs a different
// robot configuration; the robot launch has to be restarted afterwards.
//
// The response is sent first; Play Mode stops shortly after. The Editor
// script SimulationAutoRestart (Assets/Scripts/Editor) starts it again. In a
// player build Play Mode cannot be restarted, so the service answers with
// play_mode_restart = false and the caller reconfigures the running scene.
//
// The component adds itself to 'pm_robot' when a scene loads, so no scene
// change is needed.
// =============================================================================

public class SimulationRestartService : MonoBehaviour
{
    /// <summary>SessionState key telling SimulationAutoRestart to enter Play Mode again.</summary>
    public const string RestartPendingKey = "PmRobot.SimulationRestartPending";

    public string nodeName = "unity_simulation_restart";
    public string serviceName = "/unity/restart_simulation";

    [Tooltip("Time between the service response and leaving Play Mode [s], so the response reaches ROS.")]
    public float stopDelay = 0.5f;

    private ROS2UnityComponent ros2Unity;
    private ROS2Node ros2Node;
    private IService<RestartReq, RestartResp> restartService;

    private int restartRequested;
    private float stopTime = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AttachToRobot()
    {
        var robot = GameObject.Find("pm_robot");
        if (robot != null && robot.GetComponent<SimulationRestartService>() == null)
            robot.AddComponent<SimulationRestartService>();
    }

    void Start()
    {
        ros2Unity = GetComponent<ROS2UnityComponent>();
        if (ros2Unity == null)
            ros2Unity = FindObjectOfType<ROS2UnityComponent>();
    }

    void Update()
    {
        if (ros2Node == null && ros2Unity != null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode(nodeName);
            restartService = ros2Node.CreateService<RestartReq, RestartResp>(serviceName, Restart);
            Debug.Log($"SimulationRestartService: service '{serviceName}' ready.");
        }

        if (stopTime < 0f && Interlocked.Exchange(ref restartRequested, 0) == 1)
            stopTime = Time.realtimeSinceStartup + stopDelay;

        if (stopTime >= 0f && Time.realtimeSinceStartup >= stopTime)
        {
            stopTime = -1f;
            StopPlayMode();
        }
    }

    // Executor thread.
    private RestartResp Restart(RestartReq request)
    {
        var response = new RestartResp();
#if UNITY_EDITOR
        Interlocked.Exchange(ref restartRequested, 1);
        response.Success = true;
        response.Play_mode_restart = true;
        response.Message = "Unity is restarting Play Mode.";
#else
        response.Success = false;
        response.Play_mode_restart = false;
        response.Message = "Play Mode can only be restarted in the Unity Editor.";
#endif
        return response;
    }

    private static void StopPlayMode()
    {
#if UNITY_EDITOR
        Debug.Log("SimulationRestartService: restarting Play Mode to load the bringup configuration.");
        UnityEditor.SessionState.SetBool(RestartPendingKey, true);
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
