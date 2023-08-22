// generated from rosidl_typesupport_introspection_cpp/resource/idl__type_support.cpp.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

#include "array"
#include "cstddef"
#include "string"
#include "vector"
#include "rosidl_runtime_c/message_type_support_struct.h"
#include "rosidl_typesupport_cpp/message_type_support.hpp"
#include "rosidl_typesupport_interface/macros.h"
#include "customservices/msg/detail/spawn_object__response__struct.hpp"
#include "rosidl_typesupport_introspection_cpp/field_types.hpp"
#include "rosidl_typesupport_introspection_cpp/identifier.hpp"
#include "rosidl_typesupport_introspection_cpp/message_introspection.hpp"
#include "rosidl_typesupport_introspection_cpp/message_type_support_decl.hpp"
#include "rosidl_typesupport_introspection_cpp/visibility_control.h"

namespace customservices
{

namespace msg
{

namespace rosidl_typesupport_introspection_cpp
{

void SpawnObject_Response_init_function(
  void * message_memory, rosidl_runtime_cpp::MessageInitialization _init)
{
  new (message_memory) customservices::msg::SpawnObject_Response(_init);
}

void SpawnObject_Response_fini_function(void * message_memory)
{
  auto typed_message = static_cast<customservices::msg::SpawnObject_Response *>(message_memory);
  typed_message->~SpawnObject_Response();
}

static const ::rosidl_typesupport_introspection_cpp::MessageMember SpawnObject_Response_message_member_array[1] = {
  {
    "success",  // name
    ::rosidl_typesupport_introspection_cpp::ROS_TYPE_BOOLEAN,  // type
    0,  // upper bound of string
    nullptr,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices::msg::SpawnObject_Response, success),  // bytes offset in struct
    nullptr,  // default value
    nullptr,  // size() function pointer
    nullptr,  // get_const(index) function pointer
    nullptr,  // get(index) function pointer
    nullptr,  // fetch(index, &value) function pointer
    nullptr,  // assign(index, value) function pointer
    nullptr  // resize(index) function pointer
  }
};

static const ::rosidl_typesupport_introspection_cpp::MessageMembers SpawnObject_Response_message_members = {
  "customservices::msg",  // message namespace
  "SpawnObject_Response",  // message name
  1,  // number of fields
  sizeof(customservices::msg::SpawnObject_Response),
  SpawnObject_Response_message_member_array,  // message members
  SpawnObject_Response_init_function,  // function to initialize message memory (memory has to be allocated)
  SpawnObject_Response_fini_function  // function to terminate message instance (will not free memory)
};

static const rosidl_message_type_support_t SpawnObject_Response_message_type_support_handle = {
  ::rosidl_typesupport_introspection_cpp::typesupport_identifier,
  &SpawnObject_Response_message_members,
  get_message_typesupport_handle_function,
};

}  // namespace rosidl_typesupport_introspection_cpp

}  // namespace msg

}  // namespace customservices


namespace rosidl_typesupport_introspection_cpp
{

template<>
ROSIDL_TYPESUPPORT_INTROSPECTION_CPP_PUBLIC
const rosidl_message_type_support_t *
get_message_type_support_handle<customservices::msg::SpawnObject_Response>()
{
  return &::customservices::msg::rosidl_typesupport_introspection_cpp::SpawnObject_Response_message_type_support_handle;
}

}  // namespace rosidl_typesupport_introspection_cpp

#ifdef __cplusplus
extern "C"
{
#endif

ROSIDL_TYPESUPPORT_INTROSPECTION_CPP_PUBLIC
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_cpp, customservices, msg, SpawnObject_Response)() {
  return &::customservices::msg::rosidl_typesupport_introspection_cpp::SpawnObject_Response_message_type_support_handle;
}

#ifdef __cplusplus
}
#endif
