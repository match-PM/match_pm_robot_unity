# match_pm_robot_unity
Simulation of match_pm_robot in Unity. 

The simulation encompasses all relevant functionalities of the real robot and therefore enables offline-programming.


# 1. Installation 
Make sure you have Unity and ros2-for-unity installed.

You can find detailed instructions here: [Installation](Docs/INSTALLATION.md)


# 2. Usage
Open the project in Unity. 

To start the simulation, do the following:

1. Start the opcua-server

		ros2 run opcua_server opcua_server


2. Start the unity simulation by entering the play mode

3. Start ros2

		ros2 launch pm_robot_bringup pm_robot_unity_HW.launch.py 

If the robot movements are not executed in the simulation, restart the simulation (game-mode on/off).

## Communication structure
 

# 3. Modify the model
If the real robot has been modified, the simulation has to be adapted.

If hardware has been changed, you have to build a new model. You can find a detailed instruction here: [Update Robot](Docs/UPDATEROBOT.md)

To add new ros2-services, you find an instruction here: [Add new Services](Docs/AddNewServices.md)

# 4. Build the Project
To build the project using OPC UA Newtonsoft.json package for Unity is required.
- Installation:
	1. Open Unity
	2. Open "Window" dropdown menu and open "Package Manager"
	3. In package manager click on "+" symobl and choose "Add package by name..."
	3. Enter "com.unity.nuget.newtonsoft-json" as package name, and "3.0.1" as version
	4. Click "add"


### Build
1. Start Unity project from terminal
2. With your Unity project open, click on the File tab at the top of the Unity window. Select Build Settings. 
3. A new window will open where you can specify the Platform of your game. 
4. In the Scenes In Build panel, select the scenes that you would like to include in your published game. 
5. At the bottom-left of the Build Settings window, click on Player Settings. 
6. Open the Player tab. Specify the company name, product name, and game version. 
7. Expand the Resolution and Presentation section and chose Fullscreen window under Fullscreen mode.
8. Close Project settings window.
9. Click on Build and specify build directory.


