// generated from rosidl_generator_c/resource/idl__functions.h.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__REQUEST__FUNCTIONS_H_
#define CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__REQUEST__FUNCTIONS_H_

#ifdef __cplusplus
extern "C"
{
#endif

#include <stdbool.h>
#include <stdlib.h>

#include "rosidl_runtime_c/visibility_control.h"
#include "customservices/msg/rosidl_generator_c__visibility_control.h"

#include "customservices/msg/detail/spawn_object__request__struct.h"

/// Initialize msg/SpawnObject_Request message.
/**
 * If the init function is called twice for the same message without
 * calling fini inbetween previously allocated memory will be leaked.
 * \param[in,out] msg The previously allocated message pointer.
 * Fields without a default value will not be initialized by this function.
 * You might want to call memset(msg, 0, sizeof(
 * customservices__msg__SpawnObject_Request
 * )) before or use
 * customservices__msg__SpawnObject_Request__create()
 * to allocate and initialize the message.
 * \return true if initialization was successful, otherwise false
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
bool
customservices__msg__SpawnObject_Request__init(customservices__msg__SpawnObject_Request * msg);

/// Finalize msg/SpawnObject_Request message.
/**
 * \param[in,out] msg The allocated message pointer.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
void
customservices__msg__SpawnObject_Request__fini(customservices__msg__SpawnObject_Request * msg);

/// Create msg/SpawnObject_Request message.
/**
 * It allocates the memory for the message, sets the memory to zero, and
 * calls
 * customservices__msg__SpawnObject_Request__init().
 * \return The pointer to the initialized message if successful,
 * otherwise NULL
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
customservices__msg__SpawnObject_Request *
customservices__msg__SpawnObject_Request__create();

/// Destroy msg/SpawnObject_Request message.
/**
 * It calls
 * customservices__msg__SpawnObject_Request__fini()
 * and frees the memory of the message.
 * \param[in,out] msg The allocated message pointer.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
void
customservices__msg__SpawnObject_Request__destroy(customservices__msg__SpawnObject_Request * msg);

/// Check for msg/SpawnObject_Request message equality.
/**
 * \param[in] lhs The message on the left hand size of the equality operator.
 * \param[in] rhs The message on the right hand size of the equality operator.
 * \return true if messages are equal, otherwise false.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
bool
customservices__msg__SpawnObject_Request__are_equal(const customservices__msg__SpawnObject_Request * lhs, const customservices__msg__SpawnObject_Request * rhs);

/// Copy a msg/SpawnObject_Request message.
/**
 * This functions performs a deep copy, as opposed to the shallow copy that
 * plain assignment yields.
 *
 * \param[in] input The source message pointer.
 * \param[out] output The target message pointer, which must
 *   have been initialized before calling this function.
 * \return true if successful, or false if either pointer is null
 *   or memory allocation fails.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
bool
customservices__msg__SpawnObject_Request__copy(
  const customservices__msg__SpawnObject_Request * input,
  customservices__msg__SpawnObject_Request * output);

/// Initialize array of msg/SpawnObject_Request messages.
/**
 * It allocates the memory for the number of elements and calls
 * customservices__msg__SpawnObject_Request__init()
 * for each element of the array.
 * \param[in,out] array The allocated array pointer.
 * \param[in] size The size / capacity of the array.
 * \return true if initialization was successful, otherwise false
 * If the array pointer is valid and the size is zero it is guaranteed
 # to return true.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
bool
customservices__msg__SpawnObject_Request__Sequence__init(customservices__msg__SpawnObject_Request__Sequence * array, size_t size);

/// Finalize array of msg/SpawnObject_Request messages.
/**
 * It calls
 * customservices__msg__SpawnObject_Request__fini()
 * for each element of the array and frees the memory for the number of
 * elements.
 * \param[in,out] array The initialized array pointer.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
void
customservices__msg__SpawnObject_Request__Sequence__fini(customservices__msg__SpawnObject_Request__Sequence * array);

/// Create array of msg/SpawnObject_Request messages.
/**
 * It allocates the memory for the array and calls
 * customservices__msg__SpawnObject_Request__Sequence__init().
 * \param[in] size The size / capacity of the array.
 * \return The pointer to the initialized array if successful, otherwise NULL
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
customservices__msg__SpawnObject_Request__Sequence *
customservices__msg__SpawnObject_Request__Sequence__create(size_t size);

/// Destroy array of msg/SpawnObject_Request messages.
/**
 * It calls
 * customservices__msg__SpawnObject_Request__Sequence__fini()
 * on the array,
 * and frees the memory of the array.
 * \param[in,out] array The initialized array pointer.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
void
customservices__msg__SpawnObject_Request__Sequence__destroy(customservices__msg__SpawnObject_Request__Sequence * array);

/// Check for msg/SpawnObject_Request message array equality.
/**
 * \param[in] lhs The message array on the left hand size of the equality operator.
 * \param[in] rhs The message array on the right hand size of the equality operator.
 * \return true if message arrays are equal in size and content, otherwise false.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
bool
customservices__msg__SpawnObject_Request__Sequence__are_equal(const customservices__msg__SpawnObject_Request__Sequence * lhs, const customservices__msg__SpawnObject_Request__Sequence * rhs);

/// Copy an array of msg/SpawnObject_Request messages.
/**
 * This functions performs a deep copy, as opposed to the shallow copy that
 * plain assignment yields.
 *
 * \param[in] input The source array pointer.
 * \param[out] output The target array pointer, which must
 *   have been initialized before calling this function.
 * \return true if successful, or false if either pointer
 *   is null or memory allocation fails.
 */
ROSIDL_GENERATOR_C_PUBLIC_customservices
bool
customservices__msg__SpawnObject_Request__Sequence__copy(
  const customservices__msg__SpawnObject_Request__Sequence * input,
  customservices__msg__SpawnObject_Request__Sequence * output);

#ifdef __cplusplus
}
#endif

#endif  // CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__REQUEST__FUNCTIONS_H_
