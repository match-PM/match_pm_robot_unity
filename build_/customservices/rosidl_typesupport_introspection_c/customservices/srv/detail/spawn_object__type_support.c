// generated from rosidl_typesupport_introspection_c/resource/idl__type_support.c.em
// with input from customservices:srv/SpawnObject.idl
// generated code does not contain a copyright notice

#include <stddef.h>
#include "customservices/srv/detail/spawn_object__rosidl_typesupport_introspection_c.h"
#include "customservices/msg/rosidl_typesupport_introspection_c__visibility_control.h"
#include "rosidl_typesupport_introspection_c/field_types.h"
#include "rosidl_typesupport_introspection_c/identifier.h"
#include "rosidl_typesupport_introspection_c/message_introspection.h"
#include "customservices/srv/detail/spawn_object__functions.h"
#include "customservices/srv/detail/spawn_object__struct.h"


// Include directives for member types
// Member `obj_name`
// Member `parent_frame`
// Member `cad_data`
#include "rosidl_runtime_c/string_functions.h"

#ifdef __cplusplus
extern "C"
{
#endif

void customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_init_function(
  void * message_memory, enum rosidl_runtime_c__message_initialization _init)
{
  // TODO(karsten1987): initializers are not yet implemented for typesupport c
  // see https://github.com/ros2/ros2/issues/397
  (void) _init;
  customservices__srv__SpawnObject_Request__init(message_memory);
}

void customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_fini_function(void * message_memory)
{
  customservices__srv__SpawnObject_Request__fini(message_memory);
}

size_t customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__translation(
  const void * untyped_member)
{
  (void)untyped_member;
  return 3;
}

const void * customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__translation(
  const void * untyped_member, size_t index)
{
  const float * member =
    (const float *)(untyped_member);
  return &member[index];
}

void * customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__translation(
  void * untyped_member, size_t index)
{
  float * member =
    (float *)(untyped_member);
  return &member[index];
}

void customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__translation(
  const void * untyped_member, size_t index, void * untyped_value)
{
  const float * item =
    ((const float *)
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__translation(untyped_member, index));
  float * value =
    (float *)(untyped_value);
  *value = *item;
}

void customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__translation(
  void * untyped_member, size_t index, const void * untyped_value)
{
  float * item =
    ((float *)
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__translation(untyped_member, index));
  const float * value =
    (const float *)(untyped_value);
  *item = *value;
}

size_t customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__rotation(
  const void * untyped_member)
{
  (void)untyped_member;
  return 4;
}

const void * customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__rotation(
  const void * untyped_member, size_t index)
{
  const float * member =
    (const float *)(untyped_member);
  return &member[index];
}

void * customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__rotation(
  void * untyped_member, size_t index)
{
  float * member =
    (float *)(untyped_member);
  return &member[index];
}

void customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__rotation(
  const void * untyped_member, size_t index, void * untyped_value)
{
  const float * item =
    ((const float *)
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__rotation(untyped_member, index));
  float * value =
    (float *)(untyped_value);
  *value = *item;
}

void customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__rotation(
  void * untyped_member, size_t index, const void * untyped_value)
{
  float * item =
    ((float *)
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__rotation(untyped_member, index));
  const float * value =
    (const float *)(untyped_value);
  *item = *value;
}

static rosidl_typesupport_introspection_c__MessageMember customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_member_array[5] = {
  {
    "obj_name",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices__srv__SpawnObject_Request, obj_name),  // bytes offset in struct
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
    offsetof(customservices__srv__SpawnObject_Request, parent_frame),  // bytes offset in struct
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
    offsetof(customservices__srv__SpawnObject_Request, translation),  // bytes offset in struct
    NULL,  // default value
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__translation,  // size() function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__translation,  // get_const(index) function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__translation,  // get(index) function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__translation,  // fetch(index, &value) function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__translation,  // assign(index, value) function pointer
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
    offsetof(customservices__srv__SpawnObject_Request, rotation),  // bytes offset in struct
    NULL,  // default value
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__size_function__SpawnObject_Request__rotation,  // size() function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_const_function__SpawnObject_Request__rotation,  // get_const(index) function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__get_function__SpawnObject_Request__rotation,  // get(index) function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__fetch_function__SpawnObject_Request__rotation,  // fetch(index, &value) function pointer
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__assign_function__SpawnObject_Request__rotation,  // assign(index, value) function pointer
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
    offsetof(customservices__srv__SpawnObject_Request, cad_data),  // bytes offset in struct
    NULL,  // default value
    NULL,  // size() function pointer
    NULL,  // get_const(index) function pointer
    NULL,  // get(index) function pointer
    NULL,  // fetch(index, &value) function pointer
    NULL,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  }
};

static const rosidl_typesupport_introspection_c__MessageMembers customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_members = {
  "customservices__srv",  // message namespace
  "SpawnObject_Request",  // message name
  5,  // number of fields
  sizeof(customservices__srv__SpawnObject_Request),
  customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_member_array,  // message members
  customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_init_function,  // function to initialize message memory (memory has to be allocated)
  customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_fini_function  // function to terminate message instance (will not free memory)
};

// this is not const since it must be initialized on first access
// since C does not allow non-integral compile-time constants
static rosidl_message_type_support_t customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle = {
  0,
  &customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_members,
  get_message_typesupport_handle_function,
};

ROSIDL_TYPESUPPORT_INTROSPECTION_C_EXPORT_customservices
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject_Request)() {
  if (!customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle.typesupport_identifier) {
    customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle.typesupport_identifier =
      rosidl_typesupport_introspection_c__identifier;
  }
  return &customservices__srv__SpawnObject_Request__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle;
}
#ifdef __cplusplus
}
#endif

// already included above
// #include <stddef.h>
// already included above
// #include "customservices/srv/detail/spawn_object__rosidl_typesupport_introspection_c.h"
// already included above
// #include "customservices/msg/rosidl_typesupport_introspection_c__visibility_control.h"
// already included above
// #include "rosidl_typesupport_introspection_c/field_types.h"
// already included above
// #include "rosidl_typesupport_introspection_c/identifier.h"
// already included above
// #include "rosidl_typesupport_introspection_c/message_introspection.h"
// already included above
// #include "customservices/srv/detail/spawn_object__functions.h"
// already included above
// #include "customservices/srv/detail/spawn_object__struct.h"


#ifdef __cplusplus
extern "C"
{
#endif

void customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_init_function(
  void * message_memory, enum rosidl_runtime_c__message_initialization _init)
{
  // TODO(karsten1987): initializers are not yet implemented for typesupport c
  // see https://github.com/ros2/ros2/issues/397
  (void) _init;
  customservices__srv__SpawnObject_Response__init(message_memory);
}

void customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_fini_function(void * message_memory)
{
  customservices__srv__SpawnObject_Response__fini(message_memory);
}

static rosidl_typesupport_introspection_c__MessageMember customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_member_array[1] = {
  {
    "success",  // name
    rosidl_typesupport_introspection_c__ROS_TYPE_BOOLEAN,  // type
    0,  // upper bound of string
    NULL,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices__srv__SpawnObject_Response, success),  // bytes offset in struct
    NULL,  // default value
    NULL,  // size() function pointer
    NULL,  // get_const(index) function pointer
    NULL,  // get(index) function pointer
    NULL,  // fetch(index, &value) function pointer
    NULL,  // assign(index, value) function pointer
    NULL  // resize(index) function pointer
  }
};

static const rosidl_typesupport_introspection_c__MessageMembers customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_members = {
  "customservices__srv",  // message namespace
  "SpawnObject_Response",  // message name
  1,  // number of fields
  sizeof(customservices__srv__SpawnObject_Response),
  customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_member_array,  // message members
  customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_init_function,  // function to initialize message memory (memory has to be allocated)
  customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_fini_function  // function to terminate message instance (will not free memory)
};

// this is not const since it must be initialized on first access
// since C does not allow non-integral compile-time constants
static rosidl_message_type_support_t customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle = {
  0,
  &customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_members,
  get_message_typesupport_handle_function,
};

ROSIDL_TYPESUPPORT_INTROSPECTION_C_EXPORT_customservices
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject_Response)() {
  if (!customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle.typesupport_identifier) {
    customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle.typesupport_identifier =
      rosidl_typesupport_introspection_c__identifier;
  }
  return &customservices__srv__SpawnObject_Response__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle;
}
#ifdef __cplusplus
}
#endif

#include "rosidl_runtime_c/service_type_support_struct.h"
// already included above
// #include "customservices/msg/rosidl_typesupport_introspection_c__visibility_control.h"
// already included above
// #include "customservices/srv/detail/spawn_object__rosidl_typesupport_introspection_c.h"
// already included above
// #include "rosidl_typesupport_introspection_c/identifier.h"
#include "rosidl_typesupport_introspection_c/service_introspection.h"

// this is intentionally not const to allow initialization later to prevent an initialization race
static rosidl_typesupport_introspection_c__ServiceMembers customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_members = {
  "customservices__srv",  // service namespace
  "SpawnObject",  // service name
  // these two fields are initialized below on the first access
  NULL,  // request message
  // customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_Request_message_type_support_handle,
  NULL  // response message
  // customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_Response_message_type_support_handle
};

static rosidl_service_type_support_t customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_type_support_handle = {
  0,
  &customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_members,
  get_service_typesupport_handle_function,
};

// Forward declaration of request/response type support functions
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject_Request)();

const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject_Response)();

ROSIDL_TYPESUPPORT_INTROSPECTION_C_EXPORT_customservices
const rosidl_service_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__SERVICE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject)() {
  if (!customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_type_support_handle.typesupport_identifier) {
    customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_type_support_handle.typesupport_identifier =
      rosidl_typesupport_introspection_c__identifier;
  }
  rosidl_typesupport_introspection_c__ServiceMembers * service_members =
    (rosidl_typesupport_introspection_c__ServiceMembers *)customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_type_support_handle.data;

  if (!service_members->request_members_) {
    service_members->request_members_ =
      (const rosidl_typesupport_introspection_c__MessageMembers *)
      ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject_Request)()->data;
  }
  if (!service_members->response_members_) {
    service_members->response_members_ =
      (const rosidl_typesupport_introspection_c__MessageMembers *)
      ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_c, customservices, srv, SpawnObject_Response)()->data;
  }

  return &customservices__srv__detail__spawn_object__rosidl_typesupport_introspection_c__SpawnObject_service_type_support_handle;
}
