// generated from rosidl_generator_cs/resource/idl_typesupport.c.em
// with input from customservices:srv/SpawnObject.idl
// generated code does not contain a copyright notice







#include <stdbool.h>
#include <stdint.h>
#include <rosidl_runtime_c/visibility_control.h>

#include <customservices/srv/spawn_object.h>

ROSIDL_GENERATOR_C_EXPORT
void * customservices__srv__SpawnObject_Request_native_get_type_support()
{
    return (void *)ROSIDL_GET_SRV_TYPE_SUPPORT(customservices, srv, SpawnObject);
}

ROSIDL_GENERATOR_C_EXPORT
void *customservices__srv__SpawnObject_Request_native_create_native_message()
{
   customservices__srv__SpawnObject_Request *ros_message = customservices__srv__SpawnObject_Request__create();
   return ros_message;
}

ROSIDL_GENERATOR_C_EXPORT
void customservices__srv__SpawnObject_Request_native_destroy_native_message(void *raw_ros_message) {
  customservices__srv__SpawnObject_Request *ros_message = (customservices__srv__SpawnObject_Request *)raw_ros_message;
  customservices__srv__SpawnObject_Request__destroy(ros_message);
}







ROSIDL_GENERATOR_C_EXPORT
void * customservices__srv__SpawnObject_Response_native_get_type_support()
{
    return (void *)ROSIDL_GET_SRV_TYPE_SUPPORT(customservices, srv, SpawnObject);
}

ROSIDL_GENERATOR_C_EXPORT
void *customservices__srv__SpawnObject_Response_native_create_native_message()
{
   customservices__srv__SpawnObject_Response *ros_message = customservices__srv__SpawnObject_Response__create();
   return ros_message;
}

ROSIDL_GENERATOR_C_EXPORT
void customservices__srv__SpawnObject_Response_native_destroy_native_message(void *raw_ros_message) {
  customservices__srv__SpawnObject_Response *ros_message = (customservices__srv__SpawnObject_Response *)raw_ros_message;
  customservices__srv__SpawnObject_Response__destroy(ros_message);
}

