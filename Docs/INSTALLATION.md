## Unity
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

## ROS2-for-Unity
### 1. Get all prerequisites:
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
### 2. Install ros2-for-unity
1. Clone the repro to your home- or documents-folder

    https://github.com/RobotecAI/ros2-for-unity

2. Source your ROS2 installation: 
```
source /opt/ros/humble/setup.bash 
```

### 3. Get custom interfaces
The custom interfaces you need to run the unity-simulation are defined in the repro https://github.com/match-PM/pm_ros2_unity_interface.git

To clone the repro, you can defined it as a custom_messages.repro. 

Go to Documents/ros2-for-unity and open `ros2_for_unity_custom_messages.repos`.

Type in:

    repositories:
      src/ros2cs/custom_messages/pm_ros2_unity_interface:
        type: git
        url: https://github.com/match-PM/pm_ros2_unity_interface.git
        version: main

Execute

    ./pull_repositories.sh

Now the repro is located at Documents/ros2-for-unity/src/ros2cs/custom_messages

Build the workspace

    ./build.sh

The first time you might get some errors, but running `./build.sh` again should fix that.

If yout get the error: "The folder [/usr/share/dotnet/host/fxr] does not exist.", you need to reinstall your dotnet package:
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

#### 4. Deploy
Afterwards everything should be located in `~/ros2-for-unity/install/asset/Ros2ForUnity`

If you have not made any changes to the custom interfaces, the match_pm_robot_unity simulation should run.

If not or you made changes copy that folder (`~/ros2-for-unity/install/asset/Ros2ForUnity`) to the Unity `Assets` folder.

Before you (re-)start Unity make sure, you source'd all of your terminals. This is importent, otherwise Unity and ROS2 doesn't know the right linking.

Enter this to your `~.bashrc`-file.

    source ~/ros2-for-unity/install/setup.bash


## Building environment
1. Navigate to your ros2-workspace 
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

## Urdf-Importer
The urdf-importer is already included in match_pm_robot_unity.

If you want to install it manually, to https://github.com/Unity-Technologies/URDF-Importer and follow the install instructions.