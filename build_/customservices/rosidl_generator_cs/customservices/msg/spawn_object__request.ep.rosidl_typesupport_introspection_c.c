// generated from rosidl_generator_cs/resource/idl_typesupport.c.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice




#include <stdbool.h>
#include <stdint.h>
#include <rosidl_runtime_c/visibility_control.h>

#include <customservices/msg/spawn_object__request.h>

ROSIDL_GENERATOR_C_EXPORT
void * customservices__msg__SpawnObject_Request_native_get_type_support()
{
    return (void *)ROSIDL_GET_MSG_TYPE_SUPPORT(customservices, msg, SpawnObject_Request);
}

ROSIDL_GENERATOR_C_EXPORT
void *customservices__msg__SpawnObject_Request_native_create_native_message()
{
   customservices__msg__SpawnObject_Request *ros_message = customservices__msg__SpawnObject_Request__create();
   return ros_message;
}

ROSIDL_GENERATOR_C_EXPORT
void customservices__msg__SpawnObject_Request_native_destroy_native_message(void *raw_ros_message) {
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)raw_ros_message;
  customservices__msg__SpawnObject_Request__destroy(ros_message);
}


