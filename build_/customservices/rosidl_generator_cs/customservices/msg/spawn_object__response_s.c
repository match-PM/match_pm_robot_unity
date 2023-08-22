// generated from rosidl_generator_cs/resource/idl.c.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice



#include <stdlib.h>
#include <stdio.h>
#include <assert.h>
#include <stdint.h>
#include <string.h>

#include <customservices/msg/spawn_object__response.h>
#include <rosidl_runtime_c/visibility_control.h>

ROSIDL_GENERATOR_C_EXPORT
bool customservices__msg__SpawnObject_Response_native_read_field_success(void *message_handle)
{
  customservices__msg__SpawnObject_Response *ros_message = (customservices__msg__SpawnObject_Response *)message_handle;
  return ros_message->success;
}

ROSIDL_GENERATOR_C_EXPORT
void customservices__msg__SpawnObject_Response_native_write_field_success(void *message_handle, bool value)
{
  customservices__msg__SpawnObject_Response *ros_message = (customservices__msg__SpawnObject_Response *)message_handle;
  ros_message->success = value;
}












