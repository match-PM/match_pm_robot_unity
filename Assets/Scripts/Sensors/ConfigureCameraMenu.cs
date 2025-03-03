using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(ConfigureCamera))]



public class ConfigureCameraMenu : Editor
{
    public override void OnInspectorGUI()
    {
        ConfigureCamera configureCamera = (ConfigureCamera)target;
        // =======================
        //      Lens Settings 
        // =======================

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Lens specifications", EditorStyles.boldLabel);


        // Lens mangnification
        GUIContent lensMagnificationLabel = new GUIContent("Lens magnification", "Magnification of the lens of the real camera.");
        configureCamera.lensMagnification = EditorGUILayout.FloatField(lensMagnificationLabel, configureCamera.lensMagnification);
        EditorGUILayout.Space();



        // =======================
        //     Sensor Settings
        // =======================

        EditorGUILayout.LabelField("Sensor specifications", EditorStyles.boldLabel);

        // Sensor widht
        GUIContent sensorWidthLabel = new GUIContent("Sensor width", "Width of camera sensor in Px.");
        configureCamera.sensorWidth = EditorGUILayout.FloatField(sensorWidthLabel, configureCamera.sensorWidth);


        // Sensor height
        GUIContent sensorHeightLabel = new GUIContent("Sensor height", "Height of camera sensor in Px.");
        configureCamera.sensorHeight = EditorGUILayout.FloatField(sensorHeightLabel, configureCamera.sensorHeight);


        // Pixel size X
        GUIContent pixelSizeXLabel = new GUIContent("Pixel size X", "Width of single pixel in µm.");
        configureCamera.pixelSizeX = EditorGUILayout.FloatField(pixelSizeXLabel, configureCamera.pixelSizeX);


        // Pixel size Y
        GUIContent pixelSizeYLabel = new GUIContent("Pixel size Y", "Height of single pixel in µm.");
        configureCamera.pixelSizeY = EditorGUILayout.FloatField(pixelSizeYLabel, configureCamera.pixelSizeY);


        // Scale
        GUIContent scaleLabel = new GUIContent("Scale", "Conversion factor from millimeters to Unity units. Eg.If 1 Unity unit = 1 mm, then scale=1.");
        configureCamera.pixelSizeY = EditorGUILayout.FloatField(scaleLabel, configureCamera.scale);


        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

    }
}
