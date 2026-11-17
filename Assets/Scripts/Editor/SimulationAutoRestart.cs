using UnityEditor;
using UnityEngine;

// =============================================================================
// Second half of SimulationRestartService: after the service left Play Mode,
// enter it again. The flag lives in SessionState, which survives the domain
// reload on entering Play Mode but not an Editor restart.
//
// Re-entering waits a moment so the OPC UA server process of the previous
// session (stopped by LaunchOpcUaServer.OnApplicationQuit) has released its
// port before the new session starts its own.
// =============================================================================

[InitializeOnLoad]
public static class SimulationAutoRestart
{
    private const double ReenterDelaySeconds = 2.0;
    private static double reenterTime = -1.0;

    static SimulationAutoRestart()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredEditMode)
            return;
        if (!SessionState.GetBool(SimulationRestartService.RestartPendingKey, false))
            return;

        SessionState.EraseBool(SimulationRestartService.RestartPendingKey);
        reenterTime = EditorApplication.timeSinceStartup + ReenterDelaySeconds;
        EditorApplication.update += ReenterWhenDue;
    }

    private static void ReenterWhenDue()
    {
        if (EditorApplication.timeSinceStartup < reenterTime)
            return;

        EditorApplication.update -= ReenterWhenDue;
        reenterTime = -1.0;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Debug.Log("SimulationAutoRestart: entering Play Mode again.");
        EditorApplication.isPlaying = true;
    }
}
