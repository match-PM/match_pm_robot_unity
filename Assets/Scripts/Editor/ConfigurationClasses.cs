using System;
using System.Collections.Generic;

[System.Serializable]
public class ComponentConfig
{
    public string componentName;               // Name of the GameObject
    public string[] attachedScripts;           // List of script names attached to the GameObject
    public ArticulationBodyConfig articulationBodyConfig; // ArticulationBody settings (if applicable)
}

[System.Serializable]
public class ArticulationBodyConfig
{
    public string collisionDetection;
    public float linearDamping;
    public float angularDamping;
    public float jointFriction;
    public float mass;
    public bool useGravity;
    public ArticulationDriveConfig xDrive;
}

[System.Serializable]
public class ArticulationDriveConfig
{
    public float lowerLimit;
    public float upperLimit;
    public float stiffness;
    public float damping;
    public float forceLimit;
    public float targetVelocity;
    public string driveType;
}

[System.Serializable]
public class ConfigData
{
    public List<ComponentConfig> components;   // List of all components in the model
}
