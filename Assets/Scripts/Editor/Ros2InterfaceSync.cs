using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Keeps Assets/Ros2ForUnity/Plugins in sync with the custom ROS2 interface packages of the
/// ROS2 workspace. Tools/sync_ros2_interfaces.py does the work; this class checks once per
/// editor session whether the interfaces are out of date and runs the sync from the ROS2 menu.
/// </summary>
[InitializeOnLoad]
public static class Ros2InterfaceSync
{
    private const string ScriptPath = "Tools/sync_ros2_interfaces.py";
    private const string LogPath = "Logs/ros2_interface_sync.log";
    private const string CheckedKey = "Ros2InterfaceSync.Checked";
    private const int TailLength = 40;

    [Serializable]
    private sealed class PackageState
    {
        public string name;
        public string status;
        public string detail;
    }

    [Serializable]
    private sealed class CheckResult
    {
        public bool available;
        public string message;
        public PackageState[] packages = new PackageState[0];

        public PackageState[] Outdated => packages.Where(p => p.status == "changed" || p.status == "new").ToArray();
        public PackageState[] Notes => packages.Where(p => p.status == "skipped" || p.status == "orphaned").ToArray();
    }

    private static Process syncProcess;
    private static int progressId;
    private static readonly Queue<string> syncTail = new Queue<string>();

    private static string ProjectRoot => Path.GetDirectoryName(Application.dataPath);

    static Ros2InterfaceSync()
    {
        AssemblyReloadEvents.beforeAssemblyReload += AbandonSyncOnReload;

        if (Application.isBatchMode || SessionState.GetBool(CheckedKey, false))
            return;
        SessionState.SetBool(CheckedKey, true);
        RunCheck(result =>
        {
            if (!result.available)
                return;
            if (result.Outdated.Length > 0)
                Debug.LogWarning("ROS2 interfaces are out of date: " + Describe(result.Outdated) +
                                 "\nRun ROS2/Sync Interfaces to rebuild Assets/Ros2ForUnity/Plugins.");
            if (result.Notes.Length > 0)
                Debug.Log("ROS2 interface sync notes: " + Describe(result.Notes));
        });
    }

    [MenuItem("ROS2/Check Interfaces")]
    private static void CheckInterfaces()
    {
        RunCheck(result =>
        {
            string text = !result.available ? result.message
                : result.Outdated.Length == 0 ? "All interface packages are up to date."
                : "Out of date:\n" + DescribeLines(result.Outdated);
            if (result.available && result.Notes.Length > 0)
                text += "\n\nNotes:\n" + DescribeLines(result.Notes);
            EditorUtility.DisplayDialog("ROS2 Interfaces", text, "OK");
        });
    }

    [MenuItem("ROS2/Sync Interfaces")]
    private static void SyncInterfaces()
    {
        RunCheck(result =>
        {
            if (!result.available)
            {
                EditorUtility.DisplayDialog("ROS2 Interface Sync", result.message, "OK");
                return;
            }

            string changes = result.Outdated.Length == 0
                ? "All interface packages are up to date. Build and deploy anyway?"
                : "Out of date:\n" + DescribeLines(result.Outdated);
            bool confirmed = EditorUtility.DisplayDialog("ROS2 Interface Sync",
                changes + "\n\nThis builds ros2-for-unity (a few minutes) and replaces Assets/Ros2ForUnity/Plugins. " +
                "The editor has to be restarted afterwards.",
                "Sync", "Cancel");
            if (confirmed && CanSync())
                StartSync();
        });
    }

    [MenuItem("ROS2/Sync Interfaces", true)]
    private static bool CanSync()
    {
        return syncProcess == null && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static void RunCheck(Action<CheckResult> onResult)
    {
        var output = new List<string>();
        var errors = new List<string>();
        Process process;
        try
        {
            process = StartScript("check --json", output, errors);
        }
        catch (Exception e)
        {
            onResult(new CheckResult { message = $"Could not run python3 {ScriptPath}: {e.Message}" });
            return;
        }

        WhenExited(process, () =>
        {
            string json;
            lock (output)
                json = output.LastOrDefault(line => line.TrimStart().StartsWith("{"));
            CheckResult result = null;
            try
            {
                result = json != null ? JsonUtility.FromJson<CheckResult>(json) : null;
            }
            catch (ArgumentException)
            {
            }
            if (result == null)
            {
                lock (errors)
                    result = new CheckResult { message = "Interface check failed:\n" + string.Join("\n", errors) };
            }
            onResult(result);
        });
    }

    private static void StartSync()
    {
        lock (syncTail)
            syncTail.Clear();

        try
        {
            syncProcess = StartScript("sync", null, null);
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("ROS2 Interface Sync", $"Could not run python3 {ScriptPath}: {e.Message}", "OK");
            return;
        }

        // Without this the editor would import half-copied plugins as soon as it regains focus.
        AssetDatabase.DisallowAutoRefresh();
        progressId = Progress.Start("ROS2 interface sync", "Building ros2-for-unity...", Progress.Options.Indefinite);
        Progress.RegisterCancelCallback(progressId, () =>
        {
            // SIGTERM lets the script stop the colcon build it started; Process.Kill would orphan it.
            if (syncProcess != null && !syncProcess.HasExited)
                Process.Start("kill", $"-TERM {syncProcess.Id}");
            return true;
        });
        EditorApplication.update += ReportSyncProgress;
        WhenExited(syncProcess, FinishSync);
    }

    private static void ReportSyncProgress()
    {
        string line;
        lock (syncTail)
            line = syncTail.LastOrDefault();
        if (!string.IsNullOrEmpty(line) && Progress.Exists(progressId))
            Progress.SetDescription(progressId, line.Length > 120 ? line.Substring(0, 120) : line);
    }

    private static void FinishSync()
    {
        EditorApplication.update -= ReportSyncProgress;
        int exitCode = syncProcess.ExitCode;
        syncProcess.Dispose();
        syncProcess = null;
        AssetDatabase.AllowAutoRefresh();

        string tail;
        lock (syncTail)
            tail = string.Join("\n", syncTail);
        string log = Path.Combine(ProjectRoot, LogPath);

        if (exitCode != 0)
        {
            Progress.Finish(progressId, Progress.Status.Failed);
            Debug.LogError($"ROS2 interface sync failed (exit code {exitCode}). Full log: {log}\n{tail}");
            EditorUtility.DisplayDialog("ROS2 Interface Sync",
                $"The sync failed. Assets/Ros2ForUnity is unchanged unless the build had already finished.\n\nLog: {log}",
                "OK");
            return;
        }

        Progress.Finish(progressId, Progress.Status.Succeeded);
        Debug.Log($"ROS2 interface sync finished. Full log: {log}\n{tail}");

        // Native typesupport libraries stay loaded by the editor process once ROS2 was
        // initialised, so the new plugins only take effect after a restart.
        bool restart = EditorUtility.DisplayDialog("ROS2 Interface Sync",
            "Interfaces synced. Restart the editor so the new native plugins are loaded.",
            "Restart now", "Later");
        if (restart && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorApplication.OpenProject(ProjectRoot);
            return;
        }

        AssetDatabase.Refresh();
        Debug.LogWarning("ROS2 interfaces were synced. Restart the editor before entering play mode.");
    }

    private static void AbandonSyncOnReload()
    {
        if (syncProcess == null)
            return;
        // A domain reload drops the callbacks; the script keeps running and writes its log.
        AssetDatabase.AllowAutoRefresh();
        Progress.Finish(progressId, Progress.Status.Failed);
        Debug.LogWarning($"Script reload during ROS2 interface sync. It continues in the background, see {LogPath}; " +
                         "refresh the assets and restart the editor once it is done.");
    }

    private static Process StartScript(string arguments, List<string> output, List<string> errors)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo("python3", $"{ScriptPath} {arguments}")
            {
                WorkingDirectory = ProjectRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            },
        };
        process.OutputDataReceived += (_, e) => Collect(e.Data, output);
        process.ErrorDataReceived += (_, e) => Collect(e.Data, errors);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static void Collect(string line, List<string> target)
    {
        if (line == null)
            return;
        if (target != null)
        {
            lock (target)
                target.Add(line);
            return;
        }
        lock (syncTail)
        {
            syncTail.Enqueue(line);
            while (syncTail.Count > TailLength)
                syncTail.Dequeue();
        }
    }

    private static void WhenExited(Process process, Action onExit)
    {
        void Poll()
        {
            if (!process.HasExited)
                return;
            EditorApplication.update -= Poll;
            process.WaitForExit(); // flushes the asynchronous output handlers
            onExit();
        }

        EditorApplication.update += Poll;
    }

    private static string Describe(IEnumerable<PackageState> packages)
    {
        return string.Join(", ", packages.Select(p => $"{p.name} ({p.status})"));
    }

    private static string DescribeLines(IEnumerable<PackageState> packages)
    {
        return string.Join("\n", packages.Select(p => $"• {p.name}: {p.status} – {p.detail}"));
    }
}
