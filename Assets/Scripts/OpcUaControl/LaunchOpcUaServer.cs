using System;
using System.Diagnostics;
using UnityEngine;

public class LaunchOpcUaServer : MonoBehaviour
{
    private Process ros2Process = new Process();
    void Awake()
    {
        // Define the command to run
        string ros2Command = "ros2";
        string ros2Arguments = "run opcua_server opcua_server";

        // Start the ROS 2 node process
        ros2Process.StartInfo.FileName = ros2Command;
        ros2Process.StartInfo.Arguments = ros2Arguments;
        ros2Process.StartInfo.UseShellExecute = false;
        ros2Process.StartInfo.RedirectStandardOutput = true;
        ros2Process.StartInfo.RedirectStandardError = true;

        // Start the process
        ros2Process.Start();

        UnityEngine.Debug.Log("opcua-server started.");

        // Optionally, log output
        ros2Process.OutputDataReceived += (sender, args) => UnityEngine.Debug.Log(args.Data);
        ros2Process.BeginOutputReadLine();
    }

    // Shutdown when the application quits
    void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("Shutting down opcua-server.");
        ros2Process.Kill();
    }
}
