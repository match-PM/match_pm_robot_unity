// generated from rosidl_generator_c/resource/idl__struct.h.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__STRUCT_H_
#define CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__STRUCT_H_

#ifdef __cplusplus
extern "C"
{
#endif

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>


// Constants defined in the message

/// Struct defined in msg/SpawnObject_Response in the package customservices.
typedef struct customservices__msg__SpawnObject_Response
{
  bool success;
} customservices__msg__SpawnObject_Response;

// Struct for a sequence of customservices__msg__SpawnObject_Response.
typedef struct customservices__msg__SpawnObject_Response__Sequence
{
  customservices__msg__SpawnObject_Response * data;
  /// The number of valid items in data
  size_t size;
  /// The number of allocated items in data
  size_t capacity;
} customservices__msg__SpawnObject_Response__Sequence;

#ifdef __cplusplus
}
#endif

#endif  // CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__STRUCT_H_
