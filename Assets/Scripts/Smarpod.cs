using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace ROS2
{
    using SetPivot_Request = smarpod_interfaces.srv.SetPivot_Request;
    using SetPivot_Response = smarpod_interfaces.srv.SetPivot_Response;

    using GetPivot_Request = smarpod_interfaces.srv.GetPivot_Request;
    using GetPivot_Response = smarpod_interfaces.srv.GetPivot_Response;

    using GetPose_Request = smarpod_interfaces.srv.GetPose_Request;
    using GetPose_Response = smarpod_interfaces.srv.GetPose_Response;

    using Move_Request = smarpod_interfaces.srv.Move_Request;
    using Move_Response = smarpod_interfaces.srv.Move_Response;

    using Stop_Request = smarpod_interfaces.srv.Stop_Request;
    using Stop_Response = smarpod_interfaces.srv.Stop_Response;

    using StopAndHold_Request = smarpod_interfaces.srv.StopAndHold_Request;
    using StopAndHold_Response = smarpod_interfaces.srv.StopAndHold_Response;

    using GetMoveStatus_Request = smarpod_interfaces.srv.GetMoveStatus_Request;
    using GetMoveStatus_Response = smarpod_interfaces.srv.GetMoveStatus_Response;

    using SetSpeed_Request = smarpod_interfaces.srv.SetSpeed_Request;
    using SetSpeed_Response = smarpod_interfaces.srv.SetSpeed_Response;

    using GetSpeed_Request = smarpod_interfaces.srv.GetSpeed_Request;
    using GetSpeed_Response = smarpod_interfaces.srv.GetSpeed_Response;

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

    public class Smarpod : MonoBehaviour
    {
        private const string NodeName = "Smarpod";

        // ROS
        private ROS2UnityComponent ros2Unity;
        private ROS2Node ros2Node;

        private IService<SetPivot_Request, SetPivot_Response> srvSetPivot;
        private IService<GetPivot_Request, GetPivot_Response> srvGetPivot;
        private IService<GetPose_Request, GetPose_Response> srvGetPose;
        private IService<Move_Request, Move_Response> srvMove;
        private IService<Stop_Request, Stop_Response> srvStop;
        private IService<StopAndHold_Request, StopAndHold_Response> srvStopAndHold;
        private IService<GetMoveStatus_Request, GetMoveStatus_Response> srvGetMoveStatus;
        private IService<SetSpeed_Request, SetSpeed_Response> srvSetSpeed;
        private IService<GetSpeed_Request, GetSpeed_Response> srvGetSpeed;

        // Main thread action queue
        private readonly ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private void RunOnMainThread(Action action) => mainThreadActions.Enqueue(action);

        // Cached pose and speed for safe use in callbacks
        private SmarpodPose latestPose = new SmarpodPose();
        private double latestSpeed = 0.0;

        // Articulation bodies
        public ArticulationBody PlateXAxis, PlateYAxis, PlateZAxis;
        public ArticulationBody PlateAAxis, PlateBAxis, PlateCAxis;
        public ArticulationBody Tower1X, Tower1Y, Tower1Z;
        public ArticulationBody Tower2X, Tower2Y, Tower2Z;
        public ArticulationBody Tower3X, Tower3Y, Tower3Z;

        void Start()
        {
            ros2Unity = GetComponent<ROS2UnityComponent>();
        }

        void Update()
        {
            // Run any queued main-thread actions
            while (mainThreadActions.TryDequeue(out var action))
                action?.Invoke();

            if (ros2Unity.Ok())
            {
                if (ros2Node == null)
                {
                    ros2Node = ros2Unity.CreateNode(NodeName);
                    srvSetPivot = ros2Node.CreateService<SetPivot_Request, SetPivot_Response>("~/SetPivot", SetPivot);
                    srvGetPivot = ros2Node.CreateService<GetPivot_Request, GetPivot_Response>("~/GetPivot", GetPivot);
                    srvGetPose = ros2Node.CreateService<GetPose_Request, GetPose_Response>("~/GetPose", GetPose);
                    srvMove = ros2Node.CreateService<Move_Request, Move_Response>("~/Move", Move);
                    srvStop = ros2Node.CreateService<Stop_Request, Stop_Response>("~/Stop", Stop);
                    srvStopAndHold = ros2Node.CreateService<StopAndHold_Request, StopAndHold_Response>("~/StopAndHold", StopAndHold);
                    srvGetMoveStatus = ros2Node.CreateService<GetMoveStatus_Request, GetMoveStatus_Response>("~/GetMoveStatus", GetMoveStatus);
                    srvSetSpeed = ros2Node.CreateService<SetSpeed_Request, SetSpeed_Response>("~/SetSpeed", SetSpeed);
                    srvGetSpeed = ros2Node.CreateService<GetSpeed_Request, GetSpeed_Response>("~/GetSpeed", GetSpeed);
                }
            }

            // Cache pose and speed every frame
            latestPose = GetPoseInner();
            latestSpeed = GetCurrentSpeed();

            // Update towers using the pose
            UpdateTowers(latestPose);
        }

        private void UpdateTowers(SmarpodPose pose)
        {
            var t1 = GetAJoints(pose);
            var t2 = GetBJoints(pose);
            var t3 = GetCJoints(pose);

            Tower1X.SetDriveTarget(ArticulationDriveAxis.X, (float)t1.x);
            Tower1Y.SetDriveTarget(ArticulationDriveAxis.X, (float)t1.y);
            Tower1Z.SetDriveTarget(ArticulationDriveAxis.X, (float)t1.z);
            Tower2X.SetDriveTarget(ArticulationDriveAxis.X, (float)t2.x);
            Tower2Y.SetDriveTarget(ArticulationDriveAxis.X, (float)t2.y);
            Tower2Z.SetDriveTarget(ArticulationDriveAxis.X, (float)t2.z);
            Tower3X.SetDriveTarget(ArticulationDriveAxis.X, (float)t3.x);
            Tower3Y.SetDriveTarget(ArticulationDriveAxis.X, (float)t3.y);
            Tower3Z.SetDriveTarget(ArticulationDriveAxis.X, (float)t3.z);
        }

        // --- Service Callbacks ---

        private SetPivot_Response SetPivot(SetPivot_Request req) { throw new NotImplementedException(); }
        private GetPivot_Response GetPivot(GetPivot_Request req) { throw new NotImplementedException(); }

        private GetPose_Response GetPose(GetPose_Request req)
        {
            // Use cached pose (safe to read from any thread)
            var pose = latestPose;
            var res = new GetPose_Response();
            res.Xyz[0] = pose.position_x;
            res.Xyz[1] = pose.position_y;
            res.Xyz[2] = pose.position_z;
            res.Abc[0] = pose.rotation_x;
            res.Abc[1] = pose.rotation_y;
            res.Abc[2] = pose.rotation_z;
            return res;
        }

        private Move_Response Move(Move_Request req)
        {
            RunOnMainThread(() =>
            {
                PlateXAxis.SetDriveTarget(ArticulationDriveAxis.X, (float)req.X);
                PlateYAxis.SetDriveTarget(ArticulationDriveAxis.X, (float)req.Y);
                PlateZAxis.SetDriveTarget(ArticulationDriveAxis.X, (float)req.Z);
                PlateAAxis.SetDriveTarget(ArticulationDriveAxis.X, (float)req.A);
                PlateBAxis.SetDriveTarget(ArticulationDriveAxis.X, (float)req.B);
                PlateCAxis.SetDriveTarget(ArticulationDriveAxis.X, (float)req.C);
            });
            return new Move_Response() { Success = true };
        }

        private Stop_Response Stop(Stop_Request req)
        {
            RunOnMainThread(() =>
            {
                List<float> XTarget = new(), YTarget = new(), ZTarget = new();
                List<float> ATarget = new(), BTarget = new(), CTarget = new();
                PlateXAxis.GetDriveTargets(XTarget);
                PlateYAxis.GetDriveTargets(YTarget);
                PlateZAxis.GetDriveTargets(ZTarget);
                PlateAAxis.GetDriveTargets(ATarget);
                PlateBAxis.GetDriveTargets(BTarget);
                PlateCAxis.GetDriveTargets(CTarget);

                PlateXAxis.SetDriveTarget(ArticulationDriveAxis.X, XTarget[0]);
                PlateYAxis.SetDriveTarget(ArticulationDriveAxis.X, YTarget[0]);
                PlateZAxis.SetDriveTarget(ArticulationDriveAxis.X, ZTarget[0]);
                PlateAAxis.SetDriveTarget(ArticulationDriveAxis.X, ATarget[0]);
                PlateBAxis.SetDriveTarget(ArticulationDriveAxis.X, BTarget[0]);
                PlateCAxis.SetDriveTarget(ArticulationDriveAxis.X, CTarget[0]);
            });
            return new Stop_Response() { Success = true };
        }

        private StopAndHold_Response StopAndHold(StopAndHold_Request req)
        {
            Stop(new Stop_Request());
            return new StopAndHold_Response() { Success = true };
        }

        private GetMoveStatus_Response GetMoveStatus(GetMoveStatus_Request req) { throw new NotImplementedException(); }

        private SetSpeed_Response SetSpeed(SetSpeed_Request req)
        {
            RunOnMainThread(() =>
            {
                PlateXAxis.SetDriveTargetVelocity(ArticulationDriveAxis.X, (float)req.Speed);
                PlateYAxis.SetDriveTargetVelocity(ArticulationDriveAxis.X, (float)req.Speed);
                PlateZAxis.SetDriveTargetVelocity(ArticulationDriveAxis.X, (float)req.Speed);
                PlateAAxis.SetDriveTargetVelocity(ArticulationDriveAxis.X, (float)req.Speed);
                PlateBAxis.SetDriveTargetVelocity(ArticulationDriveAxis.X, (float)req.Speed);
                PlateCAxis.SetDriveTargetVelocity(ArticulationDriveAxis.X, (float)req.Speed);
            });
            return new SetSpeed_Response() { Success = true };
        }

        private GetSpeed_Response GetSpeed(GetSpeed_Request req)
        {
            // Use cached speed
            return new GetSpeed_Response() { Speed = latestSpeed };
        }

        // --- Pose and Speed Caching ---

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

        private double GetCurrentSpeed()
        {
            List<float> XVelocity = new();
            PlateXAxis.GetDriveTargetVelocities(XVelocity);
            return XVelocity.Count > 0 ? (double)XVelocity[0] : 0.0;
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
