// generated from rosidl_typesupport_introspection_c/resource/idl__type_support.c.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice

#include <stddef.h>
#include "customservices/msg/detail/spawn_object__request__rosidl_typesupport_introspection_c.h"
#include "customservices/msg/rosidl_typesupport_introspection_c__visibility_control.h"
#include "rosidl_typesupport_introspection_c/field_types.h"
#include "rosidl_typesupport_introspection_c/identifier.h"
#include "rosidl_typesupport_introspection_c/message_introspection.h"
#include "customservices/msg/detail/spawn_object__request__functions.h"
#include "customservices/msg/detail/spawn_object__request__struct.h"


// Include directives for member types
// Member `obj_name`
// Member `parent_frame`
// Member `cad_data`
#include "rosidl_runtime_c/string_functions.h"

#ifdef __cplusplus
extern "C"
{
#endif

void customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_init_function(
  void * message_memory, enum rosidl_runtime_c__message_initialization _init)
{
  // TODO(karsten1987): initializers are not yet implemented for typesupport c
  // see https://github.com/ros2/ros2/issues/397
  (void) _init;
  customservices__msg__SpawnObject_Request__init(message_memory);
}

void customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_fini_function(void * message_memory)
{
  customservices__msg__SpawnObject_Request__fini(message_memory);
}

size_t customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__translation(
  const void * untyped_member)
{
  (void)untyped_member;
  return 3;
}

const void * customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__translation(
  const void * untyped_member, size_t index)
{
  const float * member =
    (const float *)(untyped_member);
  return &member[index];
}

void * customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__translation(
  void * untyped_member, size_t index)
{
  float * member =
    (float *)(untyped_member);
  return &member[index];
}

void customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__translation(
  const void * untyped_member, size_t index, void * untyped_value)
{
  const float * item =
    ((const float *)
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__translation(untyped_member, index));
  float * value =
    (float *)(untyped_value);
  *value = *item;
}

void customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__translation(
  void * untyped_member, size_t index, const void * untyped_value)
{
  float * item =
    ((float *)
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__translation(untyped_member, index));
  const float * value =
    (const float *)(untyped_value);
  *item = *value;
}

size_t customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__rotation(
  const void * untyped_member)
{
  (void)untyped_member;
  return 4;
}

const void * customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__rotation(
  const void * untyped_member, size_t index)
{
  const float * member =
    (const float *)(untyped_member);
  return &member[index];
}

void * customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__rotation(
  void * untyped_member, size_t index)
{
  float * member =
    (float *)(untyped_member);
  return &member[index];
}

void customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__rotation(
  const void * untyped_member, size_t index, void * untyped_value)
{
  const float * item =
    ((const float *)
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__rotation(untyped_member, index));
  float * value =
    (float *)(untyped_value);
  *value = *item;
}

void customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__rotation(
  void * untyped_member, size_t index, const void * untyped_value)
{
  float * item =
    ((float *)
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__rotation(untyped_member, index));
  const float * value =
    (const float *)(untyped_value);
  *item = *value;
}

static rosidl_typesupport_introspection_c__MessageMember customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_member_array[5] = {
  {
    "obj_name",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices__msg__SpawnObject_Request, obj_name),  // bytes offset in struct
    NULL,  // default value
    NULL,  // size() function pointer
    NULL,  // get_const(index) function pointer
    NULL,  // get(index) function pointer
    NULL,  // fetch(index, &value) function pointer
    NULL,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  },
  {
    "parent_frame",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices__msg__SpawnObject_Request, parent_frame),  // bytes offset in struct
    NULL,  // default value
    NULL,  // size() function pointer
    NULL,  // get_const(index) function pointer
    NULL,  // get(index) function pointer
    NULL,  // fetch(index, &value) function pointer
    NULL,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  },
  {
    "translation",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_FLOAT,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    true,  // is array
    3,  // array size
    false,  // is upper bound
    offsetof(customservices__msg__SpawnObject_Request, translation),  // bytes offset in struct
    NULL,  // default value
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__translation,  // size() function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__translation,  // get_const(index) function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__translation,  // get(index) function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__translation,  // fetch(index, &value) function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__translation,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  },
  {
    "rotation",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_FLOAT,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    true,  // is array
    4,  // array size
    false,  // is upper bound
    offsetof(customservices__msg__SpawnObject_Request, rotation),  // bytes offset in struct
    NULL,  // default value
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__rotation,  // size() function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__rotation,  // get_const(index) function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__rotation,  // get(index) function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__rotation,  // fetch(index, &value) function pointer
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__rotation,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  },
  {
    "cad_data",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices__msg__SpawnObject_Request, cad_data),  // bytes offset in struct
    NULL,  // default value
    NULL,  // size() function pointer
    NULL,  // get_const(index) function pointer
    NULL,  // get(index) function pointer
    NULL,  // fetch(index, &value) function pointer
    NULL,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  }
};

static const rosidl_typesupport_introspection_c__MessageMembers customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_members = {
  "customservices__msg",  // message namespace
  "SpawnObject_Request",  // message name
  5,  // number of fields
  sizeof(customservices__msg__SpawnObject_Request),
  customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_member_array,  // message members
  customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_init_function,  // function to initialize message memory (memory has to be allocated)
  customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_fini_function  // function to terminate message instance (will not free memory)
};

// this is not const since it must be initialized on first access
// since C does not allow non-integral compile-time constants
static rosidl_message_type_support_t customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle = {
  0,
  &customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_members,
  get_message_typesupport_handle_function,
};

ROSIDL_TYPESUPPORT_INTROSPECTION_C_EXPORT_customservices
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, msg, SpawnObject_Request)() {
  if (!customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle.typesupport_identifier) {
    customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle.typesupport_identifier =
      rosidl_typesupport_introspection_c__identifier;
  }
  return &customservices__msg__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle;
}
#ifdef __cplusplus
}
#endif
