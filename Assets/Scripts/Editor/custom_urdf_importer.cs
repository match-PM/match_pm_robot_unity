using UnityEditor;
using UnityEngine;
using Unity.Robotics.UrdfImporter;

public class URDFImporterEditorScript : EditorWindow
{
    private string urdfFilePath = "Assets/PM_Robot/pm_robot_unity.urdf";
    private ImportSettings importSettings;

    [MenuItem("URDF Tools/Import URDF File")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(URDFImporterEditorScript));
    }

    void OnEnable()
    {
        // Initialize default import settings
        importSettings = new ImportSettings
        {
            chosenAxis = ImportSettings.axisType.zAxis,
            convexMethod = ImportSettings.convexDecomposer.unity  
        };
    }

    void OnGUI()
    {
        GUILayout.Label("URDF Importer Tool", EditorStyles.boldLabel);

        urdfFilePath = EditorGUILayout.TextField("URDF File Path", urdfFilePath);
        
        // Import settings
        GUILayout.Label("Import Settings", EditorStyles.boldLabel);
        importSettings.chosenAxis = (ImportSettings.axisType)EditorGUILayout.EnumPopup("Chosen Axis", importSettings.chosenAxis);
        importSettings.convexMethod = (ImportSettings.convexDecomposer)EditorGUILayout.EnumPopup("Convex Method", importSettings.convexMethod);


        if (GUILayout.Button("Import URDF"))
        {
            ImportURDF();
        }
    }

    private void ImportURDF()
    {
        if (string.IsNullOrEmpty(urdfFilePath))
        {
            Debug.LogError("Please specify a valid URDF file path.");
            return;
        }

        if (!System.IO.File.Exists(urdfFilePath))
        {
            Debug.LogError("URDF file not found at path: " + urdfFilePath);
            return;
        }
        
        Debug.Log("URDF file found!");  
    }
}
