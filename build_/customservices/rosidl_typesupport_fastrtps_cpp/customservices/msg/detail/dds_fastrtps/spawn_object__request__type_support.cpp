// generated from rosidl_typesupport_fastrtps_cpp/resource/idl__type_support.cpp.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice
#include "customservices/msg/detail/spawn_object__request__rosidl_typesupport_fastrtps_cpp.hpp"
#include "customservices/msg/detail/spawn_object__request__struct.hpp"

#include <limits>
#include <stdexcept>
#include <string>
#include "rosidl_typesupport_cpp/message_type_support.hpp"
#include "rosidl_typesupport_fastrtps_cpp/identifier.hpp"
#include "rosidl_typesupport_fastrtps_cpp/message_type_support.h"
#include "rosidl_typesupport_fastrtps_cpp/message_type_support_decl.hpp"
#include "rosidl_typesupport_fastrtps_cpp/wstring_conversion.hpp"
#include "fastcdr/Cdr.h"


// forward declaration of message dependencies and their conversion functions

namespace customservices
{

namespace msg
{

namespace typesupport_fastrtps_cpp
{

bool
ROSIDL_TYPESUPPORT_FASTRTPS_CPP_PUBLIC_customservices
cdr_serialize(
  const customservices::msg::SpawnObject_Request & ros_message,
  eprosima::fastcdr::Cdr & cdr)
{
  // Member: obj_name
  cdr << ros_message.obj_name;
  // Member: parent_frame
  cdr << ros_message.parent_frame;
  // Member: translation
  {
    cdr << ros_message.translation;
  }
  // Member: rotation
  {
    cdr << ros_message.rotation;
  }
  // Member: cad_data
  cdr << ros_message.cad_data;
  return true;
}

bool
ROSIDL_TYPESUPPORT_FASTRTPS_CPP_PUBLIC_customservices
cdr_deserialize(
  eprosima::fastcdr::Cdr & cdr,
  customservices::msg::SpawnObject_Request & ros_message)
{
  // Member: obj_name
  cdr >> ros_message.obj_name;

  // Member: parent_frame
  cdr >> ros_message.parent_frame;

  // Member: translation
  {
    cdr >> ros_message.translation;
  }

  // Member: rotation
  {
    cdr >> ros_message.rotation;
  }

  // Member: cad_data
  cdr >> ros_message.cad_data;

  return true;
}

size_t
ROSIDL_TYPESUPPORT_FASTRTPS_CPP_PUBLIC_customservices
get_serialized_size(
  const customservices::msg::SpawnObject_Request & ros_message,
  size_t current_alignment)
{
  size_t initial_alignment = current_alignment;

  const size_t padding = 4;
  const size_t wchar_size = 4;
  (void)padding;
  (void)wchar_size;

  // Member: obj_name
  current_alignment += padding +
    eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
    (ros_message.obj_name.size() + 1);
  // Member: parent_frame
  current_alignment += padding +
    eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
    (ros_message.parent_frame.size() + 1);
  // Member: translation
  {
    size_t array_size = 3;
    size_t item_size = sizeof(ros_message.translation[0]);
    current_alignment += array_size * item_size +
      eprosima::fastcdr::Cdr::alignment(current_alignment, item_size);
  }
  // Member: rotation
  {
    size_t array_size = 4;
    size_t item_size = sizeof(ros_message.rotation[0]);
    current_alignment += array_size * item_size +
      eprosima::fastcdr::Cdr::alignment(current_alignment, item_size);
  }
  // Member: cad_data
  current_alignment += padding +
    eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
    (ros_message.cad_data.size() + 1);

  return current_alignment - initial_alignment;
}

size_t
ROSIDL_TYPESUPPORT_FASTRTPS_CPP_PUBLIC_customservices
max_serialized_size_SpawnObject_Request(
  bool & full_bounded,
  bool & is_plain,
  size_t current_alignment)
{
  size_t initial_alignment = current_alignment;

  const size_t padding = 4;
  const size_t wchar_size = 4;
  (void)padding;
  (void)wchar_size;

  full_bounded = true;
  is_plain = true;


  // Member: obj_name
  {
    size_t array_size = 1;

    full_bounded = false;
    is_plain = false;
    for (size_t index = 0; index < array_size; ++index) {
      current_alignment += padding +
        eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
        1;
    }
  }

  // Member: parent_frame
  {
    size_t array_size = 1;

    full_bounded = false;
    is_plain = false;
    for (size_t index = 0; index < array_size; ++index) {
      current_alignment += padding +
        eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
        1;
    }
  }

  // Member: translation
  {
    size_t array_size = 3;

    current_alignment += array_size * sizeof(uint32_t) +
      eprosima::fastcdr::Cdr::alignment(current_alignment, sizeof(uint32_t));
  }

  // Member: rotation
  {
    size_t array_size = 4;

    current_alignment += array_size * sizeof(uint32_t) +
      eprosima::fastcdr::Cdr::alignment(current_alignment, sizeof(uint32_t));
  }

  // Member: cad_data
  {
    size_t array_size = 1;

    full_bounded = false;
    is_plain = false;
    for (size_t index = 0; index < array_size; ++index) {
      current_alignment += padding +
        eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
        1;
    }
  }

  return current_alignment - initial_alignment;
}

static bool _SpawnObject_Request__cdr_serialize(
  const void * untyped_ros_message,
  eprosima::fastcdr::Cdr & cdr)
{
  auto typed_message =
    static_cast<const customservices::msg::SpawnObject_Request *>(
    untyped_ros_message);
  return cdr_serialize(*typed_message, cdr);
}

static bool _SpawnObject_Request__cdr_deserialize(
  eprosima::fastcdr::Cdr & cdr,
  void * untyped_ros_message)
{
  auto typed_message =
    static_cast<customservices::msg::SpawnObject_Request *>(
    untyped_ros_message);
  return cdr_deserialize(cdr, *typed_message);
}

static uint32_t _SpawnObject_Request__get_serialized_size(
  const void * untyped_ros_message)
{
  auto typed_message =
    static_cast<const customservices::msg::SpawnObject_Request *>(
    untyped_ros_message);
  return static_cast<uint32_t>(get_serialized_size(*typed_message, 0));
}

static size_t _SpawnObject_Request__max_serialized_size(char & bounds_info)
{
  bool full_bounded;
  bool is_plain;
  size_t ret_val;

  ret_val = max_serialized_size_SpawnObject_Request(full_bounded, is_plain, 0);

  bounds_info =
    is_plain ? ROSIDL_TYPESUPPORT_FASTRTPS_PLAIN_TYPE :
    full_bounded ? ROSIDL_TYPESUPPORT_FASTRTPS_BOUNDED_TYPE : ROSIDL_TYPESUPPORT_FASTRTPS_UNBOUNDED_TYPE;
  return ret_val;
}

static message_type_support_callbacks_t _SpawnObject_Request__callbacks = {
  "customservices::msg",
  "SpawnObject_Request",
  _SpawnObject_Request__cdr_serialize,
  _SpawnObject_Request__cdr_deserialize,
  _SpawnObject_Request__get_serialized_size,
  _SpawnObject_Request__max_serialized_size
};

static rosidl_message_type_support_t _SpawnObject_Request__handle = {
  rosidl_typesupport_fastrtps_cpp::typesupport_identifier,
  &_SpawnObject_Request__callbacks,
  get_message_typesupport_handle_function,
};

}  // namespace typesupport_fastrtps_cpp

}  // namespace msg

}  // namespace customservices

namespace rosidl_typesupport_fastrtps_cpp
{

template<>
ROSIDL_TYPESUPPORT_FASTRTPS_CPP_EXPORT_customservices
const rosidl_message_type_support_t *
get_message_type_support_handle<customservices::msg::SpawnObject_Request>()
{
  return &customservices::msg::typesupport_fastrtps_cpp::_SpawnObject_Request__handle;
}

}  // namespace rosidl_typesupport_fastrtps_cpp

#ifdef __cplusplus
extern "C"
{
#endif

const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_fastrtps_cpp, customservices, msg, SpawnObject_Request)() {
  return &customservices::msg::typesupport_fastrtps_cpp::_SpawnObject_Request__handle;
}

#ifdef __cplusplus
}
#endif
