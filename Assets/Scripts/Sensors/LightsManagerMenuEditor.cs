using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightsManagerMenu))]
public class LightsManagerMenuEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LightsManagerMenu lightsManagerMenu = (LightsManagerMenu)target;

        // =======================
        //         OPCUA
        // =======================

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("OPCUA settings", EditorStyles.boldLabel);

        // Check-mark field to ask the user if the OPCUA is used for light control
        GUIContent useOPCUALabel = new GUIContent("Use OPCUA?", "Choose if light should be controled via OPCUA node.");
        lightsManagerMenu.useOPCUA = EditorGUILayout.Toggle(useOPCUALabel, lightsManagerMenu.useOPCUA);

        // OPCUA additional settings
        if (lightsManagerMenu.useOPCUA)
        {

            //  OCUA Parent name 
            GUIContent useOPCUAParentLabel = new GUIContent("OPCUA parent node", "Name of the parent node of the light on OPCUA server.");
            lightsManagerMenu.OPCUANodeName_Parent = EditorGUILayout.TextField(useOPCUAParentLabel, lightsManagerMenu.OPCUANodeName_Parent);

            // Opcua Child name
            GUIContent useOPCUAChildLabel = new GUIContent("OPCUA child node", "Name of the child node of the light on OPCUA server.");
            lightsManagerMenu.OPCUANodeName_Child = EditorGUILayout.TextField(useOPCUAChildLabel, lightsManagerMenu.OPCUANodeName_Child);

        }
        EditorGUILayout.Space();


        // =======================
        //     Lights Settings
        // =======================


        EditorGUILayout.LabelField("Light settings", EditorStyles.boldLabel);

        // Light type
        GUIContent lightSelectionLabel = new GUIContent("Selected light type", "Choose the type of light to be used.");
        lightsManagerMenu.selectedLightType = (LightsManagerMenu.LightSelection)EditorGUILayout.EnumPopup(lightSelectionLabel, lightsManagerMenu.selectedLightType);

        // Light intensity
        GUIContent lightIntensityLabel = new GUIContent("Light intensity", "Intensity of the used light in Unity.");
        lightsManagerMenu.lightIntensity = EditorGUILayout.FloatField(lightIntensityLabel, lightsManagerMenu.lightIntensity);

        // Light offset from focus point
        GUIContent lightOffsetLabel = new GUIContent("Offset from focus point", "Offset of the light in negative direction of optical axis of camera.");
        lightsManagerMenu.distanceFromFocusPoint = EditorGUILayout.FloatField(lightOffsetLabel, lightsManagerMenu.distanceFromFocusPoint);

        // Ring light additional settings
        if (lightsManagerMenu.selectedLightType == LightsManagerMenu.LightSelection.Ring_Light)
        {

            // Number of lights for ring light
            GUIContent numLightsLabel = new GUIContent("Number of lights", "Number of single lights objects used to create a ring light.");
            lightsManagerMenu.numberOfLights = EditorGUILayout.IntField(numLightsLabel, lightsManagerMenu.numberOfLights);

            // Ring light radius
            GUIContent ringLightRRadius = new GUIContent("Ring light radius", "Ring radius of the ring light.");
            lightsManagerMenu.radius = EditorGUILayout.FloatField(ringLightRRadius, lightsManagerMenu.radius);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
