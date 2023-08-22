// generated from rosidl_typesupport_introspection_cpp/resource/idl__type_support.cpp.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice

#include "array"
#include "cstddef"
#include "string"
#include "vector"
#include "rosidl_runtime_c/message_type_support_struct.h"
#include "rosidl_typesupport_cpp/message_type_support.hpp"
#include "rosidl_typesupport_interface/macros.h"
#include "customservices/msg/detail/spawn_object__request__struct.hpp"
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

void SpawnObject_Request_init_function(
  void * message_memory, rosidl_runtime_cpp::MessageInitialization _init)
{
  new (message_memory) customservices::msg::SpawnObject_Request(_init);
}

void SpawnObject_Request_fini_function(void * message_memory)
{
  auto typed_message = static_cast<customservices::msg::SpawnObject_Request *>(message_memory);
  typed_message->~SpawnObject_Request();
}

size_t size_function__SpawnObject_Request__translation(const void * untyped_member)
{
  (void)untyped_member;
  return 3;
}

const void * get_const_function__SpawnObject_Request__translation(const void * untyped_member, size_t index)
{
  const auto & member =
    *reinterpret_cast<const std::array<float, 3> *>(untyped_member);
  return &member[index];
}

void * get_function__SpawnObject_Request__translation(void * untyped_member, size_t index)
{
  auto & member =
    *reinterpret_cast<std::array<float, 3> *>(untyped_member);
  return &member[index];
}

void fetch_function__SpawnObject_Request__translation(
  const void * untyped_member, size_t index, void * untyped_value)
{
  const auto & item = *reinterpret_cast<const float *>(
    get_const_function__SpawnObject_Request__translation(untyped_member, index));
  auto & value = *reinterpret_cast<float *>(untyped_value);
  value = item;
}

void assign_function__SpawnObject_Request__translation(
  void * untyped_member, size_t index, const void * untyped_value)
{
  auto & item = *reinterpret_cast<float *>(
    get_function__SpawnObject_Request__translation(untyped_member, index));
  const auto & value = *reinterpret_cast<const float *>(untyped_value);
  item = value;
}

size_t size_function__SpawnObject_Request__rotation(const void * untyped_member)
{
  (void)untyped_member;
  return 4;
}

const void * get_const_function__SpawnObject_Request__rotation(const void * untyped_member, size_t index)
{
  const auto & member =
    *reinterpret_cast<const std::array<float, 4> *>(untyped_member);
  return &member[index];
}

void * get_function__SpawnObject_Request__rotation(void * untyped_member, size_t index)
{
  auto & member =
    *reinterpret_cast<std::array<float, 4> *>(untyped_member);
  return &member[index];
}

void fetch_function__SpawnObject_Request__rotation(
  const void * untyped_member, size_t index, void * untyped_value)
{
  const auto & item = *reinterpret_cast<const float *>(
    get_const_function__SpawnObject_Request__rotation(untyped_member, index));
  auto & value = *reinterpret_cast<float *>(untyped_value);
  value = item;
}

void assign_function__SpawnObject_Request__rotation(
  void * untyped_member, size_t index, const void * untyped_value)
{
  auto & item = *reinterpret_cast<float *>(
    get_function__SpawnObject_Request__rotation(untyped_member, index));
  const auto & value = *reinterpret_cast<const float *>(untyped_value);
  item = value;
}

static const ::rosidl_typesupport_introspection_cpp::MessageMember SpawnObject_Request_message_member_array[5] = {
  {
    "obj_name",  // name
    ::rosidl_typesupport_introspection_cpp::ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    nullptr,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices::msg::SpawnObject_Request, obj_name),  // bytes offset in struct
    nullptr,  // default value
    nullptr,  // size() function pointer
    nullptr,  // get_const(index) function pointer
    nullptr,  // get(index) function pointer
    nullptr,  // fetch(index, &value) function pointer
    nullptr,  // assign(index, value) function pointer
    nullptr  // resize(index) function pointer
  },
  {
    "parent_frame",  // name
    ::rosidl_typesupport_introspection_cpp::ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    nullptr,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices::msg::SpawnObject_Request, parent_frame),  // bytes offset in struct
    nullptr,  // default value
    nullptr,  // size() function pointer
    nullptr,  // get_const(index) function pointer
    nullptr,  // get(index) function pointer
    nullptr,  // fetch(index, &value) function pointer
    nullptr,  // assign(index, value) function pointer
    nullptr  // resize(index) function pointer
  },
  {
    "translation",  // name
    ::rosidl_typesupport_introspection_cpp::ROS_TYPE_FLOAT,  // type
    0,  // upper bound of string
    nullptr,  // members of sub message
    true,  // is array
    3,  // array size
    false,  // is upper bound
    offsetof(customservices::msg::SpawnObject_Request, translation),  // bytes offset in struct
    nullptr,  // default value
    size_function__SpawnObject_Request__translation,  // size() function pointer
    get_const_function__SpawnObject_Request__translation,  // get_const(index) function pointer
    get_function__SpawnObject_Request__translation,  // get(index) function pointer
    fetch_function__SpawnObject_Request__translation,  // fetch(index, &value) function pointer
    assign_function__SpawnObject_Request__translation,  // assign(index, value) function pointer
    nullptr  // resize(index) function pointer
  },
  {
    "rotation",  // name
    ::rosidl_typesupport_introspection_cpp::ROS_TYPE_FLOAT,  // type
    0,  // upper bound of string
    nullptr,  // members of sub message
    true,  // is array
    4,  // array size
    false,  // is upper bound
    offsetof(customservices::msg::SpawnObject_Request, rotation),  // bytes offset in struct
    nullptr,  // default value
    size_function__SpawnObject_Request__rotation,  // size() function pointer
    get_const_function__SpawnObject_Request__rotation,  // get_const(index) function pointer
    get_function__SpawnObject_Request__rotation,  // get(index) function pointer
    fetch_function__SpawnObject_Request__rotation,  // fetch(index, &value) function pointer
    assign_function__SpawnObject_Request__rotation,  // assign(index, value) function pointer
    nullptr  // resize(index) function pointer
  },
  {
    "cad_data",  // name
    ::rosidl_typesupport_introspection_cpp::ROS_TYPE_STRING,  // type
    0,  // upper bound of string
    nullptr,  // members of sub message
    false,  // is array
    0,  // array size
    false,  // is upper bound
    offsetof(customservices::msg::SpawnObject_Request, cad_data),  // bytes offset in struct
    nullptr,  // default value
    nullptr,  // size() function pointer
    nullptr,  // get_const(index) function pointer
    nullptr,  // get(index) function pointer
    nullptr,  // fetch(index, &value) function pointer
    nullptr,  // assign(index, value) function pointer
    nullptr  // resize(index) function pointer
  }
};

static const ::rosidl_typesupport_introspection_cpp::MessageMembers SpawnObject_Request_message_members = {
  "customservices::msg",  // message namespace
  "SpawnObject_Request",  // message name
  5,  // number of fields
  sizeof(customservices::msg::SpawnObject_Request),
  SpawnObject_Request_message_member_array,  // message members
  SpawnObject_Request_init_function,  // function to initialize message memory (memory has to be allocated)
  SpawnObject_Request_fini_function  // function to terminate message instance (will not free memory)
};

static const rosidl_message_type_support_t SpawnObject_Request_message_type_support_handle = {
  ::rosidl_typesupport_introspection_cpp::typesupport_identifier,
  &SpawnObject_Request_message_members,
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
get_message_type_support_handle<customservices::msg::SpawnObject_Request>()
{
  return &::customservices::msg::rosidl_typesupport_introspection_cpp::SpawnObject_Request_message_type_support_handle;
}

}  // namespace rosidl_typesupport_introspection_cpp

#ifdef __cplusplus
extern "C"
{
#endif

ROSIDL_TYPESUPPORT_INTROSPECTION_CPP_PUBLIC
const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_introspection_cpp, customservices, msg, SpawnObject_Request)() {
  return &::customservices::msg::rosidl_typesupport_introspection_cpp::SpawnObject_Request_message_type_support_handle;
}

#ifdef __cplusplus
}
#endif
