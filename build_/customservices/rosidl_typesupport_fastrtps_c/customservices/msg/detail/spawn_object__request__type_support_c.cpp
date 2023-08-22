// generated from rosidl_typesupport_fastrtps_c/resource/idl__type_support_c.cpp.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice
#include "customservices/msg/detail/spawn_object__request__rosidl_typesupport_fastrtps_c.h"


#include <cassert>
#include <limits>
#include <string>
#include "rosidl_typesupport_fastrtps_c/identifier.h"
#include "rosidl_typesupport_fastrtps_c/wstring_conversion.hpp"
#include "rosidl_typesupport_fastrtps_cpp/message_type_support.h"
#include "customservices/msg/rosidl_typesupport_fastrtps_c__visibility_control.h"
#include "customservices/msg/detail/spawn_object__request__struct.h"
#include "customservices/msg/detail/spawn_object__request__functions.h"
#include "fastcdr/Cdr.h"

#ifndef _WIN32
# pragma GCC diagnostic push
# pragma GCC diagnostic ignored "-Wunused-parameter"
# ifdef __clang__
#  pragma clang diagnostic ignored "-Wdeprecated-register"
#  pragma clang diagnostic ignored "-Wreturn-type-c-linkage"
# endif
#endif
#ifndef _WIN32
# pragma GCC diagnostic pop
#endif

// includes and forward declarations of message dependencies and their conversion functions

#if defined(__cplusplus)
extern "C"
{
#endif

#include "rosidl_runtime_c/string.h"  // cad_data, obj_name, parent_frame
#include "rosidl_runtime_c/string_functions.h"  // cad_data, obj_name, parent_frame

// forward declare type support functions


using _SpawnObject_Request__ros_msg_type = customservices__msg__SpawnObject_Request;

static bool _SpawnObject_Request__cdr_serialize(
  const void * untyped_ros_message,
  eprosima::fastcdr::Cdr & cdr)
{
  if (!untyped_ros_message) {
    fprintf(stderr, "ros message handle is null\n");
    return false;
  }
  const _SpawnObject_Request__ros_msg_type * ros_message = static_cast<const _SpawnObject_Request__ros_msg_type *>(untyped_ros_message);
  // Field name: obj_name
  {
    const rosidl_runtime_c__String * str = &ros_message->obj_name;
    if (str->capacity == 0 || str->capacity <= str->size) {
      fprintf(stderr, "string capacity not greater than size\n");
      return false;
    }
    if (str->data[str->size] != '\0') {
      fprintf(stderr, "string not null-terminated\n");
      return false;
    }
    cdr << str->data;
  }

  // Field name: parent_frame
  {
    const rosidl_runtime_c__String * str = &ros_message->parent_frame;
    if (str->capacity == 0 || str->capacity <= str->size) {
      fprintf(stderr, "string capacity not greater than size\n");
      return false;
    }
    if (str->data[str->size] != '\0') {
      fprintf(stderr, "string not null-terminated\n");
      return false;
    }
    cdr << str->data;
  }

  // Field name: translation
  {
    size_t size = 3;
    auto array_ptr = ros_message->translation;
    cdr.serializeArray(array_ptr, size);
  }

  // Field name: rotation
  {
    size_t size = 4;
    auto array_ptr = ros_message->rotation;
    cdr.serializeArray(array_ptr, size);
  }

  // Field name: cad_data
  {
    const rosidl_runtime_c__String * str = &ros_message->cad_data;
    if (str->capacity == 0 || str->capacity <= str->size) {
      fprintf(stderr, "string capacity not greater than size\n");
      return false;
    }
    if (str->data[str->size] != '\0') {
      fprintf(stderr, "string not null-terminated\n");
      return false;
    }
    cdr << str->data;
  }

  return true;
}

static bool _SpawnObject_Request__cdr_deserialize(
  eprosima::fastcdr::Cdr & cdr,
  void * untyped_ros_message)
{
  if (!untyped_ros_message) {
    fprintf(stderr, "ros message handle is null\n");
    return false;
  }
  _SpawnObject_Request__ros_msg_type * ros_message = static_cast<_SpawnObject_Request__ros_msg_type *>(untyped_ros_message);
  // Field name: obj_name
  {
    std::string tmp;
    cdr >> tmp;
    if (!ros_message->obj_name.data) {
      rosidl_runtime_c__String__init(&ros_message->obj_name);
    }
    bool succeeded = rosidl_runtime_c__String__assign(
      &ros_message->obj_name,
      tmp.c_str());
    if (!succeeded) {
      fprintf(stderr, "failed to assign string into field 'obj_name'\n");
      return false;
    }
  }

  // Field name: parent_frame
  {
    std::string tmp;
    cdr >> tmp;
    if (!ros_message->parent_frame.data) {
      rosidl_runtime_c__String__init(&ros_message->parent_frame);
    }
    bool succeeded = rosidl_runtime_c__String__assign(
      &ros_message->parent_frame,
      tmp.c_str());
    if (!succeeded) {
      fprintf(stderr, "failed to assign string into field 'parent_frame'\n");
      return false;
    }
  }

  // Field name: translation
  {
    size_t size = 3;
    auto array_ptr = ros_message->translation;
    cdr.deserializeArray(array_ptr, size);
  }

  // Field name: rotation
  {
    size_t size = 4;
    auto array_ptr = ros_message->rotation;
    cdr.deserializeArray(array_ptr, size);
  }

  // Field name: cad_data
  {
    std::string tmp;
    cdr >> tmp;
    if (!ros_message->cad_data.data) {
      rosidl_runtime_c__String__init(&ros_message->cad_data);
    }
    bool succeeded = rosidl_runtime_c__String__assign(
      &ros_message->cad_data,
      tmp.c_str());
    if (!succeeded) {
      fprintf(stderr, "failed to assign string into field 'cad_data'\n");
      return false;
    }
  }

  return true;
}  // NOLINT(readability/fn_size)

ROSIDL_TYPESUPPORT_FASTRTPS_C_PUBLIC_customservices
size_t get_serialized_size_customservices__msg__SpawnObject_Request(
  const void * untyped_ros_message,
  size_t current_alignment)
{
  const _SpawnObject_Request__ros_msg_type * ros_message = static_cast<const _SpawnObject_Request__ros_msg_type *>(untyped_ros_message);
  (void)ros_message;
  size_t initial_alignment = current_alignment;

  const size_t padding = 4;
  const size_t wchar_size = 4;
  (void)padding;
  (void)wchar_size;

  // field.name obj_name
  current_alignment += padding +
    eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
    (ros_message->obj_name.size + 1);
  // field.name parent_frame
  current_alignment += padding +
    eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
    (ros_message->parent_frame.size + 1);
  // field.name translation
  {
    size_t array_size = 3;
    auto array_ptr = ros_message->translation;
    (void)array_ptr;
    size_t item_size = sizeof(array_ptr[0]);
    current_alignment += array_size * item_size +
      eprosima::fastcdr::Cdr::alignment(current_alignment, item_size);
  }
  // field.name rotation
  {
    size_t array_size = 4;
    auto array_ptr = ros_message->rotation;
    (void)array_ptr;
    size_t item_size = sizeof(array_ptr[0]);
    current_alignment += array_size * item_size +
      eprosima::fastcdr::Cdr::alignment(current_alignment, item_size);
  }
  // field.name cad_data
  current_alignment += padding +
    eprosima::fastcdr::Cdr::alignment(current_alignment, padding) +
    (ros_message->cad_data.size + 1);

  return current_alignment - initial_alignment;
}

static uint32_t _SpawnObject_Request__get_serialized_size(const void * untyped_ros_message)
{
  return static_cast<uint32_t>(
    get_serialized_size_customservices__msg__SpawnObject_Request(
      untyped_ros_message, 0));
}

ROSIDL_TYPESUPPORT_FASTRTPS_C_PUBLIC_customservices
size_t max_serialized_size_customservices__msg__SpawnObject_Request(
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

  // member: obj_name
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
  // member: parent_frame
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
  // member: translation
  {
    size_t array_size = 3;

    current_alignment += array_size * sizeof(uint32_t) +
      eprosima::fastcdr::Cdr::alignment(current_alignment, sizeof(uint32_t));
  }
  // member: rotation
  {
    size_t array_size = 4;

    current_alignment += array_size * sizeof(uint32_t) +
      eprosima::fastcdr::Cdr::alignment(current_alignment, sizeof(uint32_t));
  }
  // member: cad_data
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

static size_t _SpawnObject_Request__max_serialized_size(char & bounds_info)
{
  bool full_bounded;
  bool is_plain;
  size_t ret_val;

  ret_val = max_serialized_size_customservices__msg__SpawnObject_Request(
    full_bounded, is_plain, 0);

  bounds_info =
    is_plain ? ROSIDL_TYPESUPPORT_FASTRTPS_PLAIN_TYPE :
    full_bounded ? ROSIDL_TYPESUPPORT_FASTRTPS_BOUNDED_TYPE : ROSIDL_TYPESUPPORT_FASTRTPS_UNBOUNDED_TYPE;
  return ret_val;
}


static message_type_support_callbacks_t __callbacks_SpawnObject_Request = {
  "customservices::msg",
  "SpawnObject_Request",
  _SpawnObject_Request__cdr_serialize,
  _SpawnObject_Request__cdr_deserialize,
  _SpawnObject_Request__get_serialized_size,
  _SpawnObject_Request__max_serialized_size
};

static rosidl_message_type_support_t _SpawnObject_Request__type_support = {
  rosidl_typesupport_fastrtps_c__identifier,
  &__callbacks_SpawnObject_Request,
  get_message_typesupport_handle_function,
};

const rosidl_message_type_support_t *
ROSIDL_TYPESUPPORT_INTERFACE__MESSAGE_SYMBOL_NAME(rosidl_typesupport_fastrtps_c, customservices, msg, SpawnObject_Request)() {
  return &_SpawnObject_Request__type_support;
}

#if defined(__cplusplus)
}
#endif
