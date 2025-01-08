using UnityEditor;
using UnityEngine;

public class ConfigNewModelScript : EditorWindow
{
    private SaveConfiguration saveConfig = new SaveConfiguration();
    private ApplyConfiguration applyConfig = new ApplyConfiguration();

    private string model_name = "pm_robot";

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

        if (GUILayout.Button("Save Configuration"))
        {
            saveConfig.SaveConfig(model_name);
        }

        if (GUILayout.Button("Apply Configuration"))
        {
            applyConfig.ApplyConfig(model_name);
        }
    }
}
