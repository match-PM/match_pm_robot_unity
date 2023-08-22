// generated from rosidl_generator_c/resource/idl__struct.h.em
// with input from customservices:srv/SpawnObject.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__STRUCT_H_
#define CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__STRUCT_H_

#ifdef __cplusplus
extern "C"
{
#endif

#include <stdbool.h>
#include <stddef.h>
#include <stdint.h>


// Constants defined in the message

// Include directives for member types
// Member 'obj_name'
// Member 'parent_frame'
// Member 'cad_data'
#include "rosidl_runtime_c/string.h"

/// Struct defined in srv/SpawnObject in the package customservices.
typedef struct customservices__srv__SpawnObject_Request
{
  rosidl_runtime_c__String obj_name;
  rosidl_runtime_c__String parent_frame;
  float translation[3];
  float rotation[4];
  rosidl_runtime_c__String cad_data;
} customservices__srv__SpawnObject_Request;

// Struct for a sequence of customservices__srv__SpawnObject_Request.
typedef struct customservices__srv__SpawnObject_Request__Sequence
{
  customservices__srv__SpawnObject_Request * data;
  /// The number of valid items in data
  size_t size;
  /// The number of allocated items in data
  size_t capacity;
} customservices__srv__SpawnObject_Request__Sequence;


// Constants defined in the message

/// Struct defined in srv/SpawnObject in the package customservices.
typedef struct customservices__srv__SpawnObject_Response
{
  bool success;
} customservices__srv__SpawnObject_Response;

// Struct for a sequence of customservices__srv__SpawnObject_Response.
typedef struct customservices__srv__SpawnObject_Response__Sequence
{
  customservices__srv__SpawnObject_Response * data;
  /// The number of valid items in data
  size_t size;
  /// The number of allocated items in data
  size_t capacity;
} customservices__srv__SpawnObject_Response__Sequence;

#ifdef __cplusplus
}
#endif

#endif  // CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__STRUCT_H_
