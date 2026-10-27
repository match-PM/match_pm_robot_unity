using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace ROS2
{
    using JointState = sensor_msgs.msg.JointState;
    using Float64 = std_msgs.msg.Float64;
    using Empty = std_msgs.msg.Empty;
    using PointMsg = geometry_msgs.msg.Point;
    using SmarPodSimState = smarpod_interfaces.msg.SmarPodSimState;

    struct Point
    {
        public double x, y, z;
        public Point(double x, double y, double z)
        {
            this.x = x; this.y = y; this.z = z;
        }
    };

    struct SmarpodPose
    {
        public double position_x, position_y, position_z, rotation_x, rotation_y, rotation_z;
        public SmarpodPose(double position_x, double position_y, double position_z, double rotation_x, double rotation_y, double rotation_z)
        {
            this.position_x = position_x;
            this.position_y = position_y;
            this.position_z = position_z;
            this.rotation_x = rotation_x;
            this.rotation_y = rotation_y;
            this.rotation_z = rotation_z;
        }
    }

    // =========================================================================
    // Simulated SmarPod, the counterpart of the ros2_control hardware
    // smaract_hexapod_unity_hw_interface/SmarPodUnitySystem (selected by
    // smaract_hexapod_description/urdf/ros2_control.xacro for hardware:=unity).
    // That plugin replaces every SmarPod SDK call by one of these topics:
    //
    //   <topicPrefix>/cmd/pose           JointState SP_X..SP_C [m, rad]   Smarpod_Move
    //   <topicPrefix>/cmd/speed          Float64 [m/s]                    Smarpod_SetSpeed
    //   <topicPrefix>/cmd/stop           Empty                            Smarpod_Stop
    //   <topicPrefix>/cmd/stop_and_hold  Float64 hold time [ms]           Smarpod_StopAndHold
    //   <topicPrefix>/cmd/pivot          Point [m]                        Smarpod_SetPivot
    //   <topicPrefix>/state              SmarPodSimState (published here) Smarpod_Get*
    //
    // Moves run on a straight line in pose space, so all six axes arrive
    // together. `speed` limits the translation and, through the platform
    // radius, how fast the tower joints of a rotation move. The pivot is only
    // stored and reported back: the platform joints form the same X/Y/Z/C/B/A
    // chain as the URDF, so rotations look as they do in RViz.
    //
    // The stage is kinematic, not physically simulated: every physics step the
    // plate and tower joints are set to the commanded pose, with drives and
    // gravity switched off, and that exact pose is reported. Like the real
    // stage it reaches its target precisely and holds it without drift.
    // =========================================================================
    public class Smarpod : MonoBehaviour
    {
        private const string NodeName = "Smarpod";

        // SmarPodSimState move_status values (Smarpod_GetMoveStatus).
        private const byte StatusStopped = 0;
        private const byte StatusHolding = 1;
        private const byte StatusMoving = 2;

        // Hold time the real system passes to Smarpod_Move; this value is
        // SMARPOD_HOLDTIME_INFINITE, so the stage holds until the next command.
        private const double InfiniteHoldTimeMs = 60000.0;

        // Distance of the tower joints from the platform centre [m]
        // (58.833488 mm in the tower kinematics below).
        private const double PlatformRadius = 0.058833488;

        private const double TargetTolerance = 1e-9;

        [Header("ROS")]
        [Tooltip("Must match the topic_prefix hardware parameter in smaract_hexapod_description/urdf/ros2_control.xacro.")]
        public string topicPrefix = "/smarpod_unity";

        [Header("Motion")]
        [Tooltip("Initial translational speed [m/s] until the hardware interface sets one.")]
        public double speed = 0.001;

        [Tooltip("Maximum travel [m] of every tower stage. Move targets that would exceed " +
                 "it are rejected, and a move stops at the last pose within it.")]
        public double maxTowerStroke = 0.01;

        private const float StrokeWarningInterval = 2f;
        private float nextStrokeWarning;

        // ROS
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;
        private ISubscription<JointState> poseSubscription;
        private ISubscription<Float64> speedSubscription;
        private ISubscription<Empty> stopSubscription;
        private ISubscription<Float64> stopAndHoldSubscription;
        private ISubscription<PointMsg> pivotSubscription;
        private IPublisher<SmarPodSimState> statePublisher;

        // Subscription callbacks run on the ROS2ForUnity executor thread.
        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private void RunOnMainThread(Action action) => mainThreadActions.Enqueue(action);

        // Simulated controller state, X/Y/Z [m] and A/B/C [rad].
        private readonly double[] setpoint = new double[6];
        private readonly double[] setpointVelocity = new double[6];
        private readonly double[] target = new double[6];
        private byte moveStatus = StatusStopped;
        private double holdUntil = double.PositiveInfinity;
        private readonly double[] pivot = new double[3];

        // Articulation bodies
        public ArticulationBody PlateXAxis, PlateYAxis, PlateZAxis;
        public ArticulationBody PlateAAxis, PlateBAxis, PlateCAxis;
        public ArticulationBody Tower1X, Tower1Y, Tower1Z;
        public ArticulationBody Tower2X, Tower2Y, Tower2Z;
        public ArticulationBody Tower3X, Tower3Y, Tower3Z;

        private ArticulationBody[] PlateAxes => new[] { PlateXAxis, PlateYAxis, PlateZAxis, PlateAAxis, PlateBAxis, PlateCAxis };
        private ArticulationBody[] TowerAxes => new[] { Tower1X, Tower1Y, Tower1Z, Tower2X, Tower2Y, Tower2Z, Tower3X, Tower3Y, Tower3Z };

        void Start()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();

            foreach (var axis in PlateAxes) MakeKinematic(axis);
            foreach (var axis in TowerAxes) MakeKinematic(axis);
            IgnoreSelfCollisions();

            var pose = GetPoseInner();
            var initial = new[] { pose.position_x, pose.position_y, pose.position_z, pose.rotation_x, pose.rotation_y, pose.rotation_z };
            Array.Copy(initial, setpoint, 6);
            Array.Copy(initial, target, 6);
        }

        void Update()
        {
            if (ros2Node == null && ros2Unity != null && ros2Unity.Ok())
            {
                ros2Node = ros2Unity.CreateNode(NodeName);
                poseSubscription = ros2Node.CreateSubscription<JointState>(topicPrefix + "/cmd/pose", OnMove);
                speedSubscription = ros2Node.CreateSubscription<Float64>(topicPrefix + "/cmd/speed", OnSpeed);
                stopSubscription = ros2Node.CreateSubscription<Empty>(topicPrefix + "/cmd/stop", OnStop);
                stopAndHoldSubscription = ros2Node.CreateSubscription<Float64>(topicPrefix + "/cmd/stop_and_hold", OnStopAndHold);
                pivotSubscription = ros2Node.CreateSubscription<PointMsg>(topicPrefix + "/cmd/pivot", OnPivot);
                statePublisher = ros2Node.CreateSensorPublisher<SmarPodSimState>(topicPrefix + "/state");
            }
        }

        void FixedUpdate()
        {
            while (mainThreadActions.TryDequeue(out var action))
                action?.Invoke();

            var previous = (double[])setpoint.Clone();
            StepMotion(Time.fixedDeltaTime);
            for (int i = 0; i < 6; i++)
                setpointVelocity[i] = (setpoint[i] - previous[i]) / Time.fixedDeltaTime;

            foreach (var root in new[] { PlateXAxis, Tower1X, Tower2X, Tower3X })
                DisableGravity(root);

            var axes = PlateAxes;
            for (int i = 0; i < 6; i++)
                SetJointPosition(axes[i], setpoint[i]);

            var pose = new SmarpodPose(setpoint[0], setpoint[1], setpoint[2], setpoint[3], setpoint[4], setpoint[5]);
            UpdateTowers(pose);
            PublishState(pose);
        }

        // --- Simulated controller ---

        private void StepMotion(double dt)
        {
            if (moveStatus == StatusHolding && Time.timeAsDouble >= holdUntil)
                moveStatus = StatusStopped;

            if (moveStatus != StatusMoving)
                return;

            double translation = 0.0, rotation = 0.0;
            for (int i = 0; i < 3; i++)
            {
                translation += (target[i] - setpoint[i]) * (target[i] - setpoint[i]);
                rotation = Math.Max(rotation, Math.Abs(target[i + 3] - setpoint[i + 3]));
            }
            double duration = Math.Max(Math.Sqrt(translation), rotation * PlatformRadius) / speed;

            var next = new double[6];
            for (int i = 0; i < 6; i++)
                next[i] = duration <= dt ? target[i] : setpoint[i] + (target[i] - setpoint[i]) * dt / duration;

            // Tower stages are not linear in the rotations, so a straight move
            // between two valid poses can still leave the stroke on the way.
            if (!WithinTowerStroke(next))
            {
                WarnStroke("stopping the move at the stroke limit");
                Halt(StatusStopped, 0.0);
                return;
            }

            Array.Copy(next, setpoint, 6);
            if (duration <= dt)
            {
                moveStatus = StatusHolding;
                holdUntil = double.PositiveInfinity;
            }
        }

        private bool WithinTowerStroke(double[] pose)
        {
            var p = new SmarpodPose(pose[0], pose[1], pose[2], pose[3], pose[4], pose[5]);
            foreach (var tower in new[] { GetAJoints(p), GetBJoints(p), GetCJoints(p) })
                if (Math.Abs(tower.x) > maxTowerStroke || Math.Abs(tower.y) > maxTowerStroke || Math.Abs(tower.z) > maxTowerStroke)
                    return false;
            return true;
        }

        private void WarnStroke(string action)
        {
            if (Time.time < nextStrokeWarning)
                return;
            nextStrokeWarning = Time.time + StrokeWarningInterval;
            Debug.LogWarning($"Smarpod: a tower stage would exceed {maxTowerStroke * 1000.0:0.###} mm; {action}.");
        }

        // Smarpod_Stop / Smarpod_StopAndHold: end the move where it is.
        private void Halt(byte status, double holdTimeMs)
        {
            Array.Copy(setpoint, target, 6);
            moveStatus = status;
            holdUntil = holdTimeMs >= InfiniteHoldTimeMs
                ? double.PositiveInfinity
                : Time.timeAsDouble + holdTimeMs / 1000.0;
        }

        // --- Subscription callbacks (executor thread) ---

        private static readonly string[] PoseJointNames = { "SP_X_Joint", "SP_Y_Joint", "SP_Z_Joint", "SP_A_Joint", "SP_B_Joint", "SP_C_Joint" };

        private void OnMove(JointState msg)
        {
            var pose = new double[6];
            for (int i = 0; i < 6; i++)
            {
                int index = Array.IndexOf(msg.Name, PoseJointNames[i]);
                if (index < 0 || index >= msg.Position.Length || double.IsNaN(msg.Position[index]))
                {
                    Debug.LogWarning($"Smarpod: ignoring move without a position for {PoseJointNames[i]}.");
                    return;
                }
                pose[i] = msg.Position[index];
            }

            RunOnMainThread(() =>
            {
                // Like Smarpod_Move for an unreachable pose: the command is
                // rejected and the stage keeps its current target.
                if (!WithinTowerStroke(pose))
                {
                    WarnStroke("target rejected");
                    return;
                }

                Array.Copy(pose, target, 6);
                bool atTarget = true;
                for (int i = 0; i < 6; i++)
                    atTarget &= Math.Abs(target[i] - setpoint[i]) <= TargetTolerance;
                if (!atTarget)
                    moveStatus = StatusMoving;
                else if (moveStatus != StatusMoving)
                    Halt(StatusHolding, InfiniteHoldTimeMs);
            });
        }

        private void OnSpeed(Float64 msg)
        {
            double value = msg.Data;
            if (!(value > 0.0))
            {
                Debug.LogWarning($"Smarpod: ignoring invalid speed {value} m/s.");
                return;
            }
            RunOnMainThread(() => speed = value);
        }

        private void OnStop(Empty msg)
        {
            RunOnMainThread(() => Halt(StatusStopped, 0.0));
        }

        private void OnStopAndHold(Float64 msg)
        {
            double holdTimeMs = msg.Data;
            RunOnMainThread(() => Halt(StatusHolding, holdTimeMs));
        }

        private void OnPivot(PointMsg msg)
        {
            double x = msg.X, y = msg.Y, z = msg.Z;
            RunOnMainThread(() =>
            {
                pivot[0] = x;
                pivot[1] = y;
                pivot[2] = z;
            });
        }

        // --- State ---

        private void PublishState(SmarpodPose pose)
        {
            // ROS shuts down before the last FixedUpdate when play mode ends.
            if (statePublisher == null || !ros2Unity.Ok())
                return;

            var state = new SmarPodSimState();
            ros2Node.clock.UpdateROSClockTime(state.Stamp);
            state.Pose[0] = pose.position_x;
            state.Pose[1] = pose.position_y;
            state.Pose[2] = pose.position_z;
            state.Pose[3] = pose.rotation_x;
            state.Pose[4] = pose.rotation_y;
            state.Pose[5] = pose.rotation_z;
            for (int i = 0; i < 6; i++)
                state.Velocity[i] = setpointVelocity[i];
            state.Pivot.X = pivot[0];
            state.Pivot.Y = pivot[1];
            state.Pivot.Z = pivot[2];
            state.Move_status = moveStatus;
            state.Speed = speed;
            state.Is_referenced = true;
            statePublisher.Publish(state);
        }

        private SmarpodPose GetPoseInner()
        {
            return new SmarpodPose(
                PlateXAxis != null ? PlateXAxis.jointPosition[0] : 0f,
                PlateYAxis != null ? PlateYAxis.jointPosition[0] : 0f,
                PlateZAxis != null ? PlateZAxis.jointPosition[0] : 0f,
                PlateAAxis != null ? PlateAAxis.jointPosition[0] : 0f,
                PlateBAxis != null ? PlateBAxis.jointPosition[0] : 0f,
                PlateCAxis != null ? PlateCAxis.jointPosition[0] : 0f
            );
        }

        // --- Kinematic joints ---

        /// <summary>Switches the drive off (xDrive returns a copy, so it is read,
        /// modified and written back); the joint position is set every step.</summary>
        private static void MakeKinematic(ArticulationBody body)
        {
            if (body == null)
                return;

            var drive = body.xDrive;
            drive.stiffness = 0f;
            drive.damping = 0f;
            body.xDrive = drive;
        }

        /// <summary>The tower tips meet the top plate in the CAD, so their colliders
        /// touch. Unity only ignores contacts between directly connected links,
        /// and depenetration would push the plate up and twist it out of its
        /// pose every step. Contacts between the Smarpod's own links (base,
        /// towers, plate, chucks) are ignored; contacts with everything else,
        /// e.g. parts on the chuck, still happen.</summary>
        private void IgnoreSelfCollisions()
        {
            if (PlateXAxis == null || Tower1X == null)
                return;

            // Lowest common ancestor of the plate chain and the towers (SmarPod_Origin).
            Transform root = PlateXAxis.transform;
            while (root != null && !Tower1X.transform.IsChildOf(root))
                root = root.parent;
            if (root == null)
                return;

            var colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                for (int j = i + 1; j < colliders.Length; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j]);
            Debug.Log($"Smarpod: ignoring contacts between {colliders.Length} colliders below '{root.name}'.");
        }

        /// <summary>Gravity on any link below the plate or a tower (chucks,
        /// attached parts) would pull the chain away from its pose during the
        /// physics step, so it is switched off for the whole subtree. Checked
        /// every step to cover links attached at runtime.</summary>
        private static void DisableGravity(ArticulationBody root)
        {
            if (root == null)
                return;

            foreach (var body in root.GetComponentsInChildren<ArticulationBody>())
                if (body.useGravity)
                    body.useGravity = false;
        }

        /// <summary>Places the joint exactly at `value` (metres or radians) and
        /// at rest, so the physics step leaves it there.</summary>
        private static void SetJointPosition(ArticulationBody body, double value)
        {
            if (body == null || body.dofCount == 0)
                return;

            body.jointPosition = new ArticulationReducedSpace((float)value);
            body.jointVelocity = new ArticulationReducedSpace(0f);
        }

        private void UpdateTowers(SmarpodPose pose)
        {
            var t1 = GetAJoints(pose);
            var t2 = GetBJoints(pose);
            var t3 = GetCJoints(pose);

            SetJointPosition(Tower1X, t1.x);
            SetJointPosition(Tower1Y, t1.y);
            SetJointPosition(Tower1Z, t1.z);
            SetJointPosition(Tower2X, t2.x);
            SetJointPosition(Tower2Y, t2.y);
            SetJointPosition(Tower2Z, t2.z);
            SetJointPosition(Tower3X, t3.x);
            SetJointPosition(Tower3Y, t3.y);
            SetJointPosition(Tower3Z, t3.z);
        }

        // --- Kinematic Calculations (unchanged) ---

        private Point GetTower1JointValues(Point p)
        {
            var q2 = -p.x - 1.0000000000010001e-6;
            var q1 = -p.y - 1.7320508075688772 * p.z + 40.541721526450615;
            var q3 = 2.0 * p.z - 114.74860824873245;
            return new Point(q1 / 1000.0, q2 / 1000.0, q3 / 1000.0);
        }
        private Point GetTower2JointValues(Point p)
        {
            var q2 = 0.49999999999999933 * p.x + 0.8660254037844376 * p.y - 1.3322676295501878e-15 * p.z - 9.9999999747524271e-7;
            var q1 = -1.7320508075688772 * q2 + 2.0 * p.y - 1.7320508075688772 * p.z + 40.541719794399796;
            var q3 = 2.0 * p.z - 114.74860824873245;
            return new Point(q1 / 1000.0, q2 / 1000.0, q3 / 1000.0);
        }
        private Point GetTower3JointValues(Point p)
        {
            var q2 = 0.49999999999999933 * p.x - 0.8660254037844376 * p.y + 1.3322676295501878e-15 * p.z - 9.9999998326438799e-7;
            var q1 = 1.7320508075688772 * q2 + 2.0 * p.y - 1.7320508075688772 * p.z + 40.54172325850142;
            var q3 = 2.0 * p.z - 114.74860824873245;
            return new Point(q1 / 1000.0, q2 / 1000.0, q3 / 1000.0);
        }
        private Point GetPointA(SmarpodPose pose)
        {
            var x_pose = pose.position_x * 1000.0;
            var y_pose = pose.position_y * 1000.0;
            var z_pose = pose.position_z * 1000.0;
            var t_x = pose.rotation_x;
            var t_y = pose.rotation_y;
            var t_z = pose.rotation_z;
            var x = x_pose + 58.833488000000003 * Sin(t_z) * Cos(t_y);
            var y = y_pose + 58.833488000000003 * Sin(t_x) * Sin(t_y) * Sin(t_z) - 58.833488000000003 * Cos(t_x) * Cos(t_z);
            var z = z_pose - 58.833488000000003 * Sin(t_x) * Cos(t_z) - 58.833488000000003 * Sin(t_y) * Sin(t_z) * Cos(t_x) + 57.374304000000002;
            return new Point(x, y, z);
        }
        private Point GetPointB(SmarpodPose pose)
        {
            var x_pose = pose.position_x * 1000.0;
            var y_pose = pose.position_y * 1000.0;
            var z_pose = pose.position_z * 1000.0;
            var t_x = pose.rotation_x;
            var t_y = pose.rotation_y;
            var t_z = pose.rotation_z;
            var x = x_pose - 29.416744000000001 * Sin(t_z) * Cos(t_y) - 29.416744000000001 * Sqrt(3) * Cos(t_y) * Cos(t_z);
            var y = y_pose - 29.416744000000001 * Sqrt(3) * (Sin(t_x) * Sin(t_y) * Cos(t_z) + Sin(t_z) * Cos(t_x)) - 29.416744000000001 * Sin(t_x) * Sin(t_y) * Sin(t_z) + 29.416744000000001 * Cos(t_x) * Cos(t_z);
            var z = z_pose - 29.416744000000001 * Sqrt(3) * (Sin(t_x) * Sin(t_z) - Sin(t_y) * Cos(t_x) * Cos(t_z)) + 29.416744000000001 * Sin(t_x) * Cos(t_z) + 29.416744000000001 * Sin(t_y) * Sin(t_z) * Cos(t_x) + 57.3743040000000;
            return new Point(x, y, z);
        }
        private Point GetPointC(SmarpodPose pose)
        {
            var x_pose = pose.position_x * 1000.0;
            var y_pose = pose.position_y * 1000.0;
            var z_pose = pose.position_z * 1000.0;
            var t_x = pose.rotation_x;
            var t_y = pose.rotation_y;
            var t_z = pose.rotation_z;
            var x = x_pose - 29.416744000000001 * Sin(t_z) * Cos(t_y) + 29.416744000000001 * Sqrt(3) * Cos(t_y) * Cos(t_z);
            var y = y_pose + 29.416744000000001 * Sqrt(3) * (Sin(t_x) * Sin(t_y) * Cos(t_z) + Sin(t_z) * Cos(t_x)) - 29.416744000000001 * Sin(t_x) * Sin(t_y) * Sin(t_z) + 29.416744000000001 * Cos(t_x) * Cos(t_z);
            var z = z_pose + 29.416744000000001 * Sqrt(3) * (Sin(t_x) * Sin(t_z) - Sin(t_y) * Cos(t_x) * Cos(t_z)) + 29.416744000000001 * Sin(t_x) * Cos(t_z) + 29.416744000000001 * Sin(t_y) * Sin(t_z) * Cos(t_x) + 57.374304000000002;
            return new Point(x, y, z);
        }
        private Point GetAJoints(SmarpodPose pose) => GetTower1JointValues(GetPointA(pose));
        private Point GetBJoints(SmarpodPose pose) => GetTower2JointValues(GetPointB(pose));
        private Point GetCJoints(SmarpodPose pose) => GetTower3JointValues(GetPointC(pose));

        private double Sin(double x) => Math.Sin(x);
        private double Cos(double x) => Math.Cos(x);
        private double Sqrt(double x) => Math.Sqrt(x);
    }
}
