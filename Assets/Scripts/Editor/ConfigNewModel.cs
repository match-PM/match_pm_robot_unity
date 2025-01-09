using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window to save a configuration of a model and apply it to a new model
/// </summary>
public class ConfigNewModelScript : EditorWindow
{
    private SaveConfiguration saveConfig = new SaveConfiguration();
    private ApplyConfiguration applyConfig = new ApplyConfiguration();

    private string model_name = "pm_robot";

    private string config_name = "RobotConfig";

    [MenuItem("Config Tools/Save and Apply Configuration")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ConfigNewModelScript));
    }

    void OnEnable()
    {

    }

    void OnGUI()
    {
        GUILayout.Label("Save and Apply Configuration", EditorStyles.boldLabel);

        model_name = EditorGUILayout.TextField("Model Name", model_name);

        config_name = EditorGUILayout.TextField("Configuration Name", config_name);

        if (GUILayout.Button("Save Configuration"))
        {
            saveConfig.SaveConfig(model_name, config_name);
        }

        if (GUILayout.Button("Apply Configuration"))
        {
            applyConfig.ApplyConfig(model_name, config_name);
        }
    }
}
