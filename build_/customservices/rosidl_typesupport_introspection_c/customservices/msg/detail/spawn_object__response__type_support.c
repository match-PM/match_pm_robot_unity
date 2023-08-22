// generated from rosidl_typesupport_introspection_c/resource/idl__type_support.c.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

#include <stddef.h>
#include "customservices/msg/detail/spawn_object__response__rosidl_typesupport_introspection_c.h"
#include "customservices/msg/rosidl_typesupport_introspection_c__visibility_control.h"
#include "rosidl_typesupport_introspection_c/field_types.h"
#include "rosidl_typesupport_introspection_c/identifier.h"
#include "rosidl_typesupport_introspection_c/message_introspection.h"
#include "customservices/msg/detail/spawn_object__response__functions.h"
#include "customservices/msg/detail/spawn_object__response__struct.h"


#ifdef __cplusplus
extern "C"
{
#endif

void customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_init_function(
  void * message_memory, enum rosidl_runtime_c__message_initialization _init)
{
  // TODO(karsten1987): initializers are not yet implemented for typesupport c
  // see https://github.com/ros2/ros2/issues/397
  (void) _init;
  customservices__msg__SpawnObject_Response__init(message_memory);
}

void customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_fini_function(void * message_memory)
{
  customservices__msg__SpawnObject_Response__fini(message_memory);
}

static rosidl_typesupport_introspection_c__MessageMember customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_member_array[1] = {
  {
    "success",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_BOOLEAN,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices__msg__SpawnObject_Response, success),  // bytes offset in struct
    NULL,  // default value
    NULL,  // size() function pointer
    NULL,  // get_const(index) function pointer
    NULL,  // get(index) function pointer
    NULL,  // fetch(index, &value) function pointer
    NULL,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  }
};

static const rosidl_typesupport_introspection_c__MessageMembers customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_members = {
  "customservices__msg",  // message namespace
  "SpawnObject_Response",  // message name
  1,  // number of fields
  sizeof(customservices__msg__SpawnObject_Response),
  customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_member_array,  // message members
  customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_init_function,  // function to initialize message memory (memory has to be allocated)
  customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_fini_function  // function to terminate message instance (will not free memory)
};

// this is not const since it must be initialized on first access
// since C does not allow non-integral compile-time constants
static rosidl_message_type_support_t customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle = {
  0,
  &customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_members,
  get_message_typesupport_handle_function,
};

ROSIDL_TYPESUPPORT_INTROSPECTION_C_EXPORT_customservices
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, msg, SpawnObject_Response)() {
  if (!customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle.typesupport_identifier) {
    customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle.typesupport_identifier =
      rosidl_typesupport_introspection_c__identifier;
  }
  return &customservices__msg__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle;
}
#ifdef __cplusplus
}
#endif
