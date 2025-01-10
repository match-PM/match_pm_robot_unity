# Update pm_robot
If new tools, chucks, sensors etc. have been added to the robot, the simulation model has to be updated.

## 1. Generate new pm_robot_urdf and generate model in unity
A new `pm_robot_unity.urdf` can be generated using the script `genereate_unity_urdf.py` which you can find in the match_pm_robot repro.
1. Run the script. 

## 2. Save Config of old pm_robot
Before you delete the old pm_robot, you can save the configuration. These can be applied to the new model to reduce manual configuration effort.

1. Click on `Config Tools` on the Menu Bar and open the menu.
2. Enter a name for the configuration.
3. Click on `Save Configuration`.

A JSON is generated containing the config of the ArticulationBodys and all which script was attached to each GameObject.

## 3. Build new pm_robot
Now you can build the new model.

1. Delete the old pm_robot in the unity scene.
2. Delete the folder `meshes` at `Assets/PM_Robot'
3. Delele the old `pm_robot_unity.urdf`
4. Copy the new generated `pm_robot_unity.urdf` and the folder `meshes` from `ros2_ws/install/pm_robot_description/share/pm_robot_description` 
2. Right click on the `pm_robot_unity.urdf` and choose 'Import Robot from selected urdf-file'

Now, the model can be configured using the `Config Tools`.

### Rename Links
The links have to be renamed, because the names from the urdf differ from the naming in the opcua-server.

The links that have to be renamed are defined in the `OpcUaAxisNames.json`.

1. Click on `Rename Links`.

### Apply General Settings
Some general setting have to be set. For example the base_link has to be set to inmovable.

1. Click on `Apply General Setting`

### Configuration of links and objects
The config saved form the old model can be applied to the new one. 

1. Click on `Apply Settings of ArticulationBody and add scripts`

### Add sensors
Cameras, lasers and other sensors are not included in the urdf. They are prefabs and can be manually added to the model.

The prefabs can be found at `Assets/Resources/Prefabs`