using System;
using System.Collections.Concurrent;
using UnityEngine;
using ROS2;

using GripperMoveReq = pm_msgs.srv.GripperMove_Request;
using GripperMoveResp = pm_msgs.srv.GripperMove_Response;
using GripperMoveRelReq = pm_msgs.srv.GripperMoveRel_Request;
using GripperMoveRelResp = pm_msgs.srv.GripperMoveRel_Response;
using GripperGetPositionReq = pm_msgs.srv.GripperGetPosition_Request;
using GripperGetPositionResp = pm_msgs.srv.GripperGetPosition_Response;
using GripperSetVelReq = pm_msgs.srv.GripperSetVel_Request;
using GripperSetVelResp = pm_msgs.srv.GripperSetVel_Response;
using GripperGetVelReq = pm_msgs.srv.GripperGetVel_Request;
using GripperGetVelResp = pm_msgs.srv.GripperGetVel_Response;

public class ParallelGripperServiceController : MonoBehaviour
{
    [Header("Scene References")]
    public ROS2UnityComponent ros2Unity;
    public bool searchParentsForRos2Unity = true;
    public ArticulationBody movingJaw;
    public string movingJawObjectName = "SmarAct_Gripper_Jaw_flat";
    public bool autoResolveMovingJaw = true;

    [Header("Jaw Limits")]
    public float physicalLowerLimit = 0f;
    public float physicalUpperLimit = 0.03f; // realistic jaw opening 3 cm

    [Header("Debug Position Control")]
    [Tooltip("When enabled, the Inspector slider drives the jaw target every frame and overrides Move/MoveRel commands.")]
    public bool enableDebugPositionControl = false;
    [Tooltip("Use the debug lower/upper limit fields while debugging.")]
    public bool useDebugLimitInputs = true;
    public float debugLowerLimit = 0f;
    public float debugUpperLimit = 0.03f;
    public float appliedLowerLimit;
    public float appliedUpperLimit;
    [Range(0f, 1f)]
    [Tooltip("0 = lower limit, 1 = upper limit.")]
    public float debugPosition01 = 0f;
    public float debugTargetPosition;
    [Tooltip("When enabled, debug mode forces a usable target drive setup if the articulation drive is not configured for position control.")]
    public bool autoConfigureDriveForDebug = true;
    [HideInInspector] public float debugDriveStiffness = 20000f;
    [HideInInspector] public float debugDriveDamping = 200f;
    [HideInInspector] public float debugDriveForceLimit = 1000f;

    [Header("ROS Services")]
    public string nodeName = "unity_parallel_gripper_service";
    public string serviceNamespace = "/pm_parallel_gripper_jaw_controller";

    private ROS2Node ros2Node;
    private IService<GripperMoveReq, GripperMoveResp> srvMove;
    private IService<GripperMoveRelReq, GripperMoveRelResp> srvMoveRel;
    private IService<GripperGetPositionReq, GripperGetPositionResp> srvGetPosition;
    private IService<GripperSetVelReq, GripperSetVelResp> srvSetVel;
    private IService<GripperGetVelReq, GripperGetVelResp> srvGetVel;

    private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
    private readonly object stateLock = new object();

    private float latestPosition;
    private double latestVelocity;
    private float cachedLowerLimit;
    private float cachedUpperLimit;
    private float cachedSimulationLowerLimit;
    private float cachedSimulationUpperLimit;
    private bool jawReady;
    private bool loggedMissingRos2Unity;
    private bool loggedMissingJaw;

    void Start()
    {
        ResolveRos2Unity();
        ResolveMovingJaw();
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action?.Invoke();

        ResolveRos2Unity();
        ResolveMovingJaw();
        ApplyJawLimitsToDrive();
        ApplyDebugPositionControl();
        UpdateCachedState();

        if (ros2Node == null && ros2Unity != null && ros2Unity.Ok())
        {
            ros2Node = ros2Unity.CreateNode(nodeName);
            srvMove = ros2Node.CreateService<GripperMoveReq, GripperMoveResp>(BuildServiceName("Move"), Move);
            srvMoveRel = ros2Node.CreateService<GripperMoveRelReq, GripperMoveRelResp>(BuildServiceName("MoveRel"), MoveRel);
            srvGetPosition = ros2Node.CreateService<GripperGetPositionReq, GripperGetPositionResp>(BuildServiceName("GetPosition"), GetPosition);
            srvSetVel = ros2Node.CreateService<GripperSetVelReq, GripperSetVelResp>(BuildServiceName("SetVel"), SetVel);
            srvGetVel = ros2Node.CreateService<GripperGetVelReq, GripperGetVelResp>(BuildServiceName("GetVel"), GetVel);
            /////
            /// // Add more services here as needed, following the same pattern.
            /// Debug logs
            Debug.Log(
                $"ParallelGripperServiceController: Registered services under '{NormalizeServiceNamespace()}' " +
                $"on fixed gripper object '{gameObject.name}', driving '{(movingJaw != null ? movingJaw.name : "unresolved")}'.");
        }
    }

    private void ResolveRos2Unity()
    {
        if (ros2Unity != null)
            return;

        ros2Unity = GetComponent<ROS2UnityComponent>();

        if (ros2Unity == null && searchParentsForRos2Unity)
            ros2Unity = GetComponentInParent<ROS2UnityComponent>();

        if (ros2Unity == null)
            ros2Unity = FindObjectOfType<ROS2UnityComponent>();

        if (ros2Unity == null && !loggedMissingRos2Unity)
        {
            Debug.LogWarning(
                "ParallelGripperServiceController: No ROS2UnityComponent found. " +
                "Assign one in the Inspector or keep a ROS2UnityComponent on a parent/root object.");
            loggedMissingRos2Unity = true;
            return;
        }

        if (ros2Unity != null)
            loggedMissingRos2Unity = false;
    }

    private void ResolveMovingJaw()
    {
        if (movingJaw != null && movingJaw.dofCount > 0)
        {
            loggedMissingJaw = false;
            return;
        }

        if (!autoResolveMovingJaw)
            return;

        foreach (var articulation in GetComponentsInChildren<ArticulationBody>(true))
        {
            if (articulation == null || articulation.dofCount == 0)
                continue;

            if (articulation.name == movingJawObjectName)
            {
                movingJaw = articulation;
                loggedMissingJaw = false;
                return;
            }
        }

        if (!loggedMissingJaw)
        {
            Debug.LogWarning(
                $"ParallelGripperServiceController: Could not resolve moving jaw '{movingJawObjectName}' under '{gameObject.name}'.");
            loggedMissingJaw = true;
        }
    }

    private void ApplyJawLimitsToDrive()
    {
        if (movingJaw == null || movingJaw.dofCount == 0)
        {
            appliedLowerLimit = 0f;
            appliedUpperLimit = 0f;
            return;
        }

        bool useDebugLimits = useDebugLimitInputs || enableDebugPositionControl;
        float requestedLower = useDebugLimits ? debugLowerLimit : physicalLowerLimit;
        float requestedUpper = useDebugLimits ? debugUpperLimit : physicalUpperLimit;
        float lowerLimit = Mathf.Min(requestedLower, requestedUpper);
        float upperLimit = Mathf.Max(requestedLower, requestedUpper);

        appliedLowerLimit = lowerLimit;
        appliedUpperLimit = upperLimit;

        var drive = movingJaw.xDrive;
        bool changed = false;

        if (enableDebugPositionControl && autoConfigureDriveForDebug)
        {
            if (drive.driveType != ArticulationDriveType.Target)
            {
                drive.driveType = ArticulationDriveType.Target;
                changed = true;
            }

            if (drive.stiffness <= 0f)
            {
                drive.stiffness = debugDriveStiffness;
                changed = true;
            }

            if (drive.damping <= 0f)
            {
                drive.damping = debugDriveDamping;
                changed = true;
            }

            if (drive.forceLimit <= 0f)
            {
                drive.forceLimit = debugDriveForceLimit;
                changed = true;
            }
        }

        if (!Mathf.Approximately(drive.lowerLimit, lowerLimit))
        {
            drive.lowerLimit = lowerLimit;
            changed = true;
        }

        if (!Mathf.Approximately(drive.upperLimit, upperLimit))
        {
            drive.upperLimit = upperLimit;
            changed = true;
        }

        float clampedTarget = ClampTarget(drive.target, lowerLimit, upperLimit);
        if (!Mathf.Approximately(drive.target, clampedTarget))
        {
            drive.target = clampedTarget;
            changed = true;
        }

        if (changed)
            movingJaw.xDrive = drive;
    }

    private void ApplyDebugPositionControl()
    {
        if (movingJaw == null || movingJaw.dofCount == 0)
        {
            debugTargetPosition = 0f;
            return;
        }

        var drive = movingJaw.xDrive;
        debugTargetPosition = Mathf.Lerp(drive.lowerLimit, drive.upperLimit, Mathf.Clamp01(debugPosition01));

        if (!enableDebugPositionControl)
            return;

        float clampedTarget = ClampTarget(debugTargetPosition, drive.lowerLimit, drive.upperLimit);
        if (Mathf.Approximately(drive.target, clampedTarget))
            return;

        drive.target = clampedTarget;
        movingJaw.xDrive = drive;
    }

    private void UpdateCachedState()
    {
        if (movingJaw == null || movingJaw.dofCount == 0)
        {
            lock (stateLock)
            {
                jawReady = false;
            }
            return;
        }

        var drive = movingJaw.xDrive;

        lock (stateLock)
        {
            latestPosition = movingJaw.jointPosition[0];
            latestVelocity = movingJaw.jointVelocity[0];
            cachedLowerLimit = drive.lowerLimit;
            cachedUpperLimit = drive.upperLimit;
            cachedSimulationLowerLimit = drive.lowerLimit;
            cachedSimulationUpperLimit = drive.upperLimit;
            jawReady = true;
        }
    }

    private string BuildServiceName(string serviceLeaf)
    {
        string prefix = NormalizeServiceNamespace();
        return string.IsNullOrEmpty(prefix) ? $"~/{serviceLeaf}" : $"{prefix}/{serviceLeaf}";
    }

    private string NormalizeServiceNamespace()
    {
        if (string.IsNullOrWhiteSpace(serviceNamespace))
            return string.Empty;

        string normalized = serviceNamespace.Trim();
        if (!normalized.StartsWith("/"))
            normalized = "/" + normalized;

        return normalized.TrimEnd('/');
    }

    private bool TryGetCachedJawState(
        out float position,
        out float lowerLimit,
        out float upperLimit,
        out float simulationLowerLimit,
        out float simulationUpperLimit,
        out string errorMessage)
    {
        lock (stateLock)
        {
            position = latestPosition;
            lowerLimit = cachedLowerLimit;
            upperLimit = cachedUpperLimit;
            simulationLowerLimit = cachedSimulationLowerLimit;
            simulationUpperLimit = cachedSimulationUpperLimit;

            if (!jawReady)
            {
                errorMessage = "Moving gripper jaw ArticulationBody is missing or not ready.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    private bool TryValidateCachedJaw(out string errorMessage)
    {
        lock (stateLock)
        {
            if (!jawReady)
            {
                errorMessage = "Moving gripper jaw ArticulationBody is missing or not ready.";
                return false;
            }
        }

        errorMessage = null;
        return true;
    }

    private static float ClampTarget(float requestedPosition, float lowerLimit, float upperLimit)
    {
        return Mathf.Clamp(requestedPosition, lowerLimit, upperLimit);
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private void EnqueueOnMainThread(Action action)
    {
        mainThreadActions.Enqueue(action);
    }

    private GripperMoveResp Move(GripperMoveReq request)
    {
        var response = new GripperMoveResp();

        try
        {
            if (!TryGetCachedJawState(
                    out _,
                    out float lowerLimit,
                    out float upperLimit,
                    out float simulationLowerLimit,
                    out float simulationUpperLimit,
                    out string errorMessage))
            {
                response.Success = false;
                response.Error_msg = errorMessage;
                return response;
            }

            if (!IsFinite(request.Target_position))
            {
                response.Success = false;
                response.Error_msg = "Target_position must be finite.";
                return response;
            }

            float target = ClampTarget((float)request.Target_position, lowerLimit, upperLimit);
            float simulationTarget = ClampTarget(target, simulationLowerLimit, simulationUpperLimit);
            EnqueueOnMainThread(() =>
            {
                if (movingJaw == null || movingJaw.dofCount == 0)
                    return;

                var drive = movingJaw.xDrive;
                drive.target = simulationTarget;
                movingJaw.xDrive = drive;
            });

            response.Success = true;
            response.Error_msg = string.Empty;
            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error_msg = ex.Message;
            return response;
        }
    }

    private GripperMoveRelResp MoveRel(GripperMoveRelReq request)
    {
        var response = new GripperMoveRelResp();

        try
        {
            if (!TryGetCachedJawState(
                    out float position,
                    out float lowerLimit,
                    out float upperLimit,
                    out float simulationLowerLimit,
                    out float simulationUpperLimit,
                    out string errorMessage))
            {
                response.Success = false;
                response.Error_msg = errorMessage;
                return response;
            }

            if (!IsFinite(request.Offset))
            {
                response.Success = false;
                response.Error_msg = "Offset must be finite.";
                return response;
            }

            float target = ClampTarget(position + (float)request.Offset, lowerLimit, upperLimit);
            float simulationTarget = ClampTarget(target, simulationLowerLimit, simulationUpperLimit);
            EnqueueOnMainThread(() =>
            {
                if (movingJaw == null || movingJaw.dofCount == 0)
                    return;

                var drive = movingJaw.xDrive;
                drive.target = simulationTarget;
                movingJaw.xDrive = drive;
            });

            response.Success = true;
            response.Error_msg = "moved by " + request.Offset;
            return response;
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Error_msg = ex.Message;
            return response;
        }
    }

    private GripperGetPositionResp GetPosition(GripperGetPositionReq _)
    {
        var response = new GripperGetPositionResp();

        lock (stateLock)
        {
            response.Success = jawReady;
            response.Position = latestPosition;
        }

        return response;
    }

    private GripperSetVelResp SetVel(GripperSetVelReq request)
    {
        var response = new GripperSetVelResp();

        try
        {
            if (!TryValidateCachedJaw(out _))
            {
                response.Success = false;
                return response;
            }

            if (!IsFinite(request.Target_velocity))
            {
                response.Success = false;
                return response;
            }

            float simulationVelocity = (float)request.Target_velocity;
            EnqueueOnMainThread(() =>
            {
                if (movingJaw == null || movingJaw.dofCount == 0)
                    return;

                var drive = movingJaw.xDrive;
                drive.targetVelocity = simulationVelocity;
                movingJaw.xDrive = drive;
            });

            response.Success = true;
            return response;
        }
        catch
        {
            response.Success = false;
            return response;
        }
    }

    private GripperGetVelResp GetVel(GripperGetVelReq _)
    {
        var response = new GripperGetVelResp();

        lock (stateLock)
        {
            response.Current_velocity = latestVelocity;
        }

        return response;
    }
}
