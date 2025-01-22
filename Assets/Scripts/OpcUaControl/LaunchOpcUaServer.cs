using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

public class LaunchOpcUaServer : MonoBehaviour
{
    private Process ros2Process;
    public bool serverIsRunning = false;

    void Awake()
    {
        try
        {
            // Define the command to run
            string ros2Command = "ros2";
            string ros2Arguments = "run opcua_server opcua_server";

            // Initialize the process
            ros2Process = new Process
            {
                StartInfo =
                {
                    FileName = ros2Command,
                    Arguments = ros2Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            // ros2Process.OutputDataReceived += (sender, args) =>
            // {
            //     if (!string.IsNullOrEmpty(args.Data))
            //     {
            //         UnityEngine.Debug.Log(args.Data);

            //         // Check for a specific message that indicates the server is running
            //         if (args.Data.Contains("Server running")) // Adjust based on actual server output
            //         {
            //             serverIsRunning = true;
            //         }
            //     }
            // };

            ros2Process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    UnityEngine.Debug.LogError(args.Data);
                }
            };

            // Start the process
            ros2Process.Start();
            ros2Process.BeginOutputReadLine();
            ros2Process.BeginErrorReadLine();

            UnityEngine.Debug.Log("Starting opcua-server...");

        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Failed to start opcua-server: {e.Message}");
        }
    }

    // Shutdown when the application quits
    void OnApplicationQuit()
    {
        if (ros2Process != null && !ros2Process.HasExited)
        {
            try
            {
                UnityEngine.Debug.Log("Shutting down OPC UA server...");
                // This kills only the direct process, not children:
                ros2Process.StandardInput.WriteLine("\x3"); // Ctrl+C
                ros2Process.StandardInput.Flush();

                // Wait up to 5 seconds (blocking) for the process to exit
                ros2Process.WaitForExit(5000); 
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"Error while shutting down OPC UA server: {e.Message}");
            }
            finally
            {
                ros2Process.Dispose();
                UnityEngine.Debug.Log("OPC UA server has been shut down.");
            }
        }
    }
}
