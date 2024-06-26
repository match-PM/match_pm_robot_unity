# match_pm_robot_unity
Simulation of match_pm_robot in Unity

## 1. Repository overview


## 2. Installation 
Make sure you have Unity installed.

### Unity
1. Run in the terminal: 

```
wget -qO - https://hub.unity3d.com/linux/keys/public | gpg --dearmor | sudo tee /usr/share/keyrings/Unity_Technologies_ApS.gpg > /dev/null
```
```
sudo sh -c 'echo "deb [signed-by=/usr/share/keyrings/Unity_Technologies_ApS.gpg] https://hub.unity3d.com/linux/repos/deb stable main" > /etc/apt/sources.list.d/unityhub.list'
```
```
sudo apt update
```
```
sudo apt-get install unityhub
```


2. After installation log into Unity account in browser. If after login you browser doesn't respond (white screen is shown -> no redirection and no change in unity hub app.), press Ctr + U on your browser in the unity login tab. Copy the string which contains: "unityhub:xxx". Close Unity Hub. Open a new treminal and run: "unityhub {copied string}".
3. After logging in into Unity Hub install Unity Editor. 
4. Update libss	

```
echo "deb http://security.ubuntu.com/ubuntu focal-security main" | sudo tee /etc/apt/sources.list.d/focal-security.list
```
```
sudo apt-get update
```
```
sudo apt-get install libssl1.1
```

3. If Unity does not respond after opening, the selected graphics drivers of your Unbutu distro are probably not from Nvidia:
    1. Open 'Additional Drivers' and select "Nvidia driver metapackage from nvidia-driver-- (proprietary, tested)". 
    2. If you are unable to connect to the local network after installing the drivers, restart the computer: 
        1. When starting, select the option "Advanced options for ubuntu" in the GRUB menu. 
        2. Then select the "Recovery mode" of your latest kernel (second Option in the Menu) or last stable kernel version. 
        3. After booting in recovery mode, start the terminal and run apt-get update.
        4. Restart your computer.   

### ROS2-for-Unity
1. Get all prerequisites:
```
sudo apt install -y ros-${ROS_DISTRO}-test-msgs
```
```
sudo apt install -y ros-${ROS_DISTRO}-fastrtps ros-${ROS_DISTRO}-rmw-fastrtps-cpp
```
```
sudo apt install -y ros-${ROS_DISTRO}-cyclonedds ros-${ROS_DISTRO}-rmw-cyclonedds-cpp
```
```
curl -s https://packagecloud.io/install/repositories/dirk-thomas/vcstool/script.deb.sh | sudo bash
```
```
sudo apt-get update
```
```
sudo apt-get install -y python3-vcstool
```
```
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
```
```
sudo dpkg -i packages-microsoft-prod.deb
```
```
rm packages-microsoft-prod.deb
```
```
sudo apt-get update; \
        sudo apt-get install -y apt-transport-https && \
        sudo apt-get update && \
        sudo apt-get install -y dotnet-sdk-6.0
```
2. Clone project form Git Hub: https://github.com/RobotecAI/ros2cs
3. Source your ROS2 installation: source /opt/ros/foxy/setup.bash (ange foxy to whatever version you are using)
4. Navigate to the top project folder and run from terminal: ./get_repos.sh 
5. Run in the terminal: ./build.sh
5*. "The folder [/usr/share/dotnet/host/fxr] does not exist.". If you get following error while running code from 5. you need to reinstall your dotnet package:
```
sudo apt remove dotnet*
```
```
sudo apt remove 'dotnet*'
```
```
sudo apt remove netstandard*
```
```
sudo apt remove aspnetcore*
```
```
sudo gedit /etc/apt/preferences.d/99microsoft-dotnet.pref -> Save following following into your file:
            Package: *
        Pin: origin "packages.microsoft.com"
        Pin-Priority: 1001
```
```
sudo apt install apt-transport-https
```
```
sudo apt update
```
```
sudo apt install dotnet-sdk-6.0
```


### Building environment
1. Navigate to your workspace 
2. Run: colcon build 
3. If there are no previous installation files you might get this error:
    By not providing "Findros2cs_common.cmake" in CMAKE_MODULE_PATH this
    project has asked CMake to find a package configuration file provided by
    "ros2cs_common", but CMake did not find one.

    Could not find a package configuration file provided by "ros2cs_common"
    with any of the following names:

        ros2cs_commonConfig.cmake
        ros2cs_common-config.cmake

    Add the installation prefix of "ros2cs_common" to CMAKE_PREFIX_PATH or set
    "ros2cs_common_DIR" to a directory containing one of the above files.  If
    "ros2cs_common" provides a separate development package or SDK, be sure it
    has been installed.
Solution: DO NOT delete Build, Log and Install folders from step 1. Instead run following command in terminal (make sure you are still in your workspace):

        source ./install/setup.bash
        colcon build

### 3. Usage
Open the project in Unity. 
In the asset folder contains the main files of the project.
* `pm_robot`: Contains the robot.urdf, meshes and materials of the robot. The robot in Unity is build with an URDF-Importer(https://github.com/Unity-Technologies/URDF-Importer)
* `Scripts`: Contains the scripts to control the robot.

### 4. Build the Project
- To build the project using OPC UA Newtonsoft.jason package for Unity is required.
- Installation:
	1. Open Unity
	2. Open "Window" dorpdown menu and open "Package Manager"
	3. In package manager click on "+" symobl and choose "Add package by name..."
	3. Enter "com.unity.nuget.newtonsoft-json" as package name, and "3.0.1" as version
	4. Click "add"


Build:
1. Start Unity project from terminal
2. With your Unity project open, click on the File tab at the top of the Unity window. Select Build Settings. 
3. A new window will open where you can specify the Platform of your game. 
4. In the Scenes In Build panel, select the scenes that you would like to include in your published game. 
5. At the bottom-left of the Build Settings window, click on Player Settings. 
6. Open the Player tab. Specify the company name, product name, and game version. 
7. Expand the Resolution and Presentation section and chose Fullscreen window under Fullscreen mode.
8. Close Project settings window.
9. Click on Build and specify build directory.


