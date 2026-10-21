# How to create a custom service and implement it in Unity

This Tutorial discribes how to create a ros2 service and implement it into Unity.

We are using ros2-for-unity, so the tutorial is based on [this](https://roboticsbackend.com/ros2-create-custom-message/) tutorial.

Make sure, you have ros2-for-unity [installed](INSTALLATION.md).

## Where the interfaces live
The interfaces are defined in their original packages in your ROS2 workspace (`~/ros2_ws/src`), e.g. `match_pm_robot/pm_msgs` or `smarpod_model/smarpod_interfaces`.

Unity gets them through the sync tool (`Tools/sync_ros2_interfaces.py`, Unity menu `ROS2`). It finds **every** interface package below `~/ros2_ws/src` automatically, i.e. every `package.xml` containing

    <member_of_group>rosidl_interface_packages</member_of_group>

New interface packages therefore need no extra registration. To leave one out, add its name to `exclude` in `Tools/ros2_interfaces.json`. Packages inside a folder with a `COLCON_IGNORE` file are ignored, just like colcon does.

The repro https://github.com/match-PM/pm_ros2_unity_interface.git (`~/Documents/ros2-for-unity/src/ros2cs/custom_messages/pm_ros2_unity_interface`) is only a mirror that ros2-for-unity builds from. Do **not** edit it by hand, the sync overwrites it.

## Create new package
If you want to implement a hole new funtionality, you can create a new package.

If you only want to add a new service to a package, you can directly do is (s. [add new service](#add-new-service))

Go to the repro in your ROS2 workspace where the package belongs, e.g.

    cd ~/ros2_ws/src/match_pm_robot

Create a new package

    ros2 pkg create *my_fancy_interface_name*

Navigate inside the package, and:

Remove the `include/` and `src/` folders.
Add `srv/` folder (also `msg/` if you want to create message).

    $ cd *my_fancy_interface_name*/
    $ rm -rf include/
    $ rm -rf src/
    $ mkdir srv
    $ mkdir msg

Add this 3 lines to the `package.xml`-File. The last one is also what makes the sync pick the package up.

    <buildtool_depend>rosidl_default_generators</buildtool_depend>
    <exec_depend>rosidl_default_runtime</exec_depend>
    <member_of_group>rosidl_interface_packages</member_of_group>

Dependencies of an interface package must be message packages that ros2-for-unity can build (`std_msgs`, `geometry_msgs`, `sensor_msgs`, ... or other packages from your workspace). Packages depending on e.g. `moveit_msgs` are skipped by the sync with a note.

## Add new service

Navigate to the `/srv`-Folder and Create an empty file with the `srv`-Ending
You can only user CamaelCaseNaming. You are __not__ allowed to use underscores ( _ ) 

    FancyService.srv

After that add your disired datatypes regarding [this](https://docs.ros.org/en/foxy/Concepts/About-ROS-Interfaces.html) documentation to your file.

The request- and the response part have to be devided by 3 dashes ( --- ). This becomes importent later.


Next, open the `CMakeLists.txt`-File and add these lines. Only one path per line and no dividing comma

    find_package(rosidl_default_generators REQUIRED)
    rosidl_generate_interfaces(${PROJECT_NAME}
    "srv/FancyService.srv"
    "srv/AnotherFancyService.srv
    "msg/FancyMessage.msg
    )
    ament_export_dependencies(rosidl_default_runtime)

This far, everything is the same for a message. You will get this on your own.

## Sync to Unity

Unity checks the interfaces once per editor session and logs a warning in the console when an interface package was added or changed since the last sync.

To bring them into Unity:

1. Unity menu `ROS2/Sync Interfaces` (or `ROS2/Check Interfaces` to only see what changed).
   From a terminal the same works with

        python3 Tools/sync_ros2_interfaces.py check
        python3 Tools/sync_ros2_interfaces.py sync

2. The sync mirrors the packages into `pm_ros2_unity_interface`, builds ros2-for-unity (a few minutes, progress in the Unity status bar, full log in `Logs/ros2_interface_sync.log`) and replaces `Assets/Ros2ForUnity/Plugins`.
   The build only sources `/opt/ros/<distro>`, not your workspace, so it does not matter what is sourced in the terminal.
3. Restart the Unity editor when asked. Native plugins that are already loaded can only be replaced by a restart.
4. Commit the changes in this project (`Assets/Ros2ForUnity`, `Tools/ros2_interfaces.lock.json`) and in `pm_ros2_unity_interface`.

Packages that are still in the mirror but no longer in the workspace are kept and reported as `orphaned`. `python3 Tools/sync_ros2_interfaces.py sync --prune` removes them, but refuses as long as a Unity script still uses them.

Paths can be changed in `Tools/ros2_interfaces.json` or with the environment variables `PM_ROS2_WS_SRC` and `PM_ROS2_FOR_UNITY`.

## Implementing

In this folder

    *Unity*/Assets/Ros2ForUnity/Scripts

there are some examples for implementation.
I used `ROS2ServiceExample.cs` to implement my service.

One thing I want to mention is, that in this two lines

    using spawnObjReq = my_fancy_interface_name.srv.FancyService_Request;
    using spawnObjResp = my_fancy_interface_name.srv.FancyService_Response;

the Service-type is torn apart by using an underscore ( _ ) and adding `Request` / `Response`
Thats why we needed the three dashes earlier.

After implementation similar to the example you should be able to use the service like any other service.

Take care to user the right service name and type everywhere.
