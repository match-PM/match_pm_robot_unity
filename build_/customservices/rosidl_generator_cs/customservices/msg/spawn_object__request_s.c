// generated from rosidl_generator_cs/resource/idl.c.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice



#include <stdlib.h>
#include <stdio.h>
#include <assert.h>
#include <stdint.h>
#include <string.h>

#include <customservices/msg/spawn_object__request.h>
#include <rosidl_runtime_c/visibility_control.h>
#include <rosidl_runtime_c/string.h>
#include <rosidl_runtime_c/string_functions.h>
#include <rosidl_runtime_c/string.h>
#include <rosidl_runtime_c/string_functions.h>
#include <rosidl_runtime_c/primitives_sequence.h>
#include <rosidl_runtime_c/primitives_sequence_functions.h>
#include <rosidl_runtime_c/string.h>
#include <rosidl_runtime_c/string_functions.h>

ROSIDL_GENERATOR_C_EXPORT
const char * customservices__msg__SpawnObject_Request_native_read_field_obj_name(void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  return ros_message->obj_name.data;
}
ROSIDL_GENERATOR_C_EXPORT
const char * customservices__msg__SpawnObject_Request_native_read_field_parent_frame(void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  return ros_message->parent_frame.data;
}
ROSIDL_GENERATOR_C_EXPORT
const char * customservices__msg__SpawnObject_Request_native_read_field_cad_data(void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  return ros_message->cad_data.data;
}

ROSIDL_GENERATOR_C_EXPORT
void customservices__msg__SpawnObject_Request_native_write_field_obj_name(void *message_handle, const char * value)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  if (&ros_message->obj_name.data)
  { // reinitializing string if message is being reused
    rosidl_runtime_c__String__fini(&ros_message->obj_name);
    rosidl_runtime_c__String__init(&ros_message->obj_name);
  }
  rosidl_runtime_c__String__assign(
    &ros_message->obj_name, value);
}
ROSIDL_GENERATOR_C_EXPORT
void customservices__msg__SpawnObject_Request_native_write_field_parent_frame(void *message_handle, const char * value)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  if (&ros_message->parent_frame.data)
  { // reinitializing string if message is being reused
    rosidl_runtime_c__String__fini(&ros_message->parent_frame);
    rosidl_runtime_c__String__init(&ros_message->parent_frame);
  }
  rosidl_runtime_c__String__assign(
    &ros_message->parent_frame, value);
}
ROSIDL_GENERATOR_C_EXPORT
void customservices__msg__SpawnObject_Request_native_write_field_cad_data(void *message_handle, const char * value)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  if (&ros_message->cad_data.data)
  { // reinitializing string if message is being reused
    rosidl_runtime_c__String__fini(&ros_message->cad_data);
    rosidl_runtime_c__String__init(&ros_message->cad_data);
  }
  rosidl_runtime_c__String__assign(
    &ros_message->cad_data, value);
}


ROSIDL_GENERATOR_C_EXPORT
bool customservices__msg__SpawnObject_Request_native_write_field_translation(float *value, int size, void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  if (size != 3)
    return false;
  float *dest = ros_message->translation;
  memcpy(dest, value, sizeof(float)*size);
  return true;
}
ROSIDL_GENERATOR_C_EXPORT
bool customservices__msg__SpawnObject_Request_native_write_field_rotation(float *value, int size, void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  if (size != 4)
    return false;
  float *dest = ros_message->rotation;
  memcpy(dest, value, sizeof(float)*size);
  return true;
}


ROSIDL_GENERATOR_C_EXPORT
float *customservices__msg__SpawnObject_Request_native_read_field_translation(int *size, void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  *size = 3;
  return ros_message->translation;
}
ROSIDL_GENERATOR_C_EXPORT
float *customservices__msg__SpawnObject_Request_native_read_field_rotation(int *size, void *message_handle)
{
  customservices__msg__SpawnObject_Request *ros_message = (customservices__msg__SpawnObject_Request *)message_handle;
  *size = 4;
  return ros_message->rotation;
}








