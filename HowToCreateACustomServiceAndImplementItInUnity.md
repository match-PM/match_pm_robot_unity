# How to create a custom service and implement it in Unity

This Tutorial discribes how to create a ros2 service, which is pretty straight forward, and compiling it with ros2-for-Unity, which took me hours to figure out.

## 1. Create Service and compile
This part is based on [this](https://roboticsbackend.com/ros2-create-custom-message/) tutorial

Go to src-Folder in ros2-for-unity

    cd /home/pmlab/Documents/ros2-for-unity/ros2-for-unity/src/ros2cs/src

create a new package

    ros2 pkg create *my_fancy_interface_name*

Navigate inside the package, and:

Remove the `include/` and `src/` folders.
Add `srv/` folder (also `msg/` if you want to create message).

    $ cd ~/ros2_ws/src/*my_fancy_interface_name*/
    $ rm -rf include/
    $ rm -rf src/
    $ mkdir srv
    $ mkdir msg

Add this 3 lines to the `package.xml`-File

    <buildtool_depend>rosidl_default_generators</buildtool_depend>
    <exec_depend>rosidl_default_runtime</exec_depend>
    <member_of_group>rosidl_interface_packages</member_of_group>

Now navigate to the `/srv`-Folder and Create an empty file with the `srv`-Ending
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

Done that, navigate back to the ros2-for-unity-folder

    cd ../../../

and call the build.sh to compile everything

    ./build.sh

The first time you might get some errors, but running `./build.sh` again should fix that.

This far, everything is the same for a message. You will get this on your own.

## 2. Deploying
Afterwards everything should be located in `/Documents/ros2-for-unity/ros2-for-unity/install/asset/Ros2ForUnity`

Just copy that folder to the Unity `Assets` folder

Before you (re-)start Unity make sure, you source'd all of your terminals. This is importent, otherwise Unity and ROS2 doesn't know the right linking.

    source /Documents/ros2-for-unity/ros2-for-unity/install/setup.bash

or just add that line to your `~.bashrc`-file.


## 3. Implementing

In this folder

    *Unity*/Assets/Ros2ForUnity/Scripts

there are some examples for implementation.
I used `ROS2ServiceExample.cs` to implement my service.

One thing I want to mention is, that in this two lines

    using spawnObjReq = my_fancy_interface_name.srv.FancyService_Request;
    using spawnObjResp = my_fancy_interface_name.srv.FancyService_Response;

the Service-type is torn apart by using an underscore ( _ ) and adding `Request` / `Response`
Thats why we needed the three dashes earlier.

After implementation similar to the example you should be able to use the service like any outer service.

Take care to user the right service name and type everywhere.