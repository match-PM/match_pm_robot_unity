using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window to save a configuration of a model and apply it to a new model
/// </summary>
public class ConfigNewModelScript : EditorWindow
{
    private SaveConfiguration saveConfig = new SaveConfiguration();
    private ApplyConfiguration applyConfig = new ApplyConfiguration();

    private ApplyGeneralSettings applyGeneralSettings = new ApplyGeneralSettings();

    private string model_name = "pm_robot";

    private string config_name = "RobotConfig";

    // path to the configuration files in Assets/PM_Robot/Configs
    private string config_path = Application.dataPath + "/PM_Robot/Configs/";

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

        config_path = EditorGUILayout.TextField("Configuration Files Path", config_path);

        config_name = EditorGUILayout.TextField("Configuration Name", config_name);

        if (GUILayout.Button("Save Configuration"))
        {
            saveConfig.path_config = config_path;
            saveConfig.SaveConfig(model_name, config_name);
        }

        if (GUILayout.Button("Rename Links"))
        {
            applyGeneralSettings.RenameLinks(model_name);
        }

        if (GUILayout.Button("Apply General Settings"))
        {
            applyGeneralSettings.path_config = config_path;
            applyGeneralSettings.ApplySettings(model_name, config_name);
        }

        if (GUILayout.Button("Apply Settings of ArticulationBody and add scripts"))
        {
            applyConfig.path_config = config_path;
            applyConfig.ApplyConfig(model_name, config_name);
        }
    }
}
