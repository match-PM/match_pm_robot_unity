// generated from rosidl_generator_c/resource/idl__functions.c.em
// with input from customservices:msg/SpawnObject_Request.idl
// generated code does not contain a copyright notice
#include "customservices/msg/detail/spawn_object__request__functions.h"

#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

#include "rcutils/allocator.h"


// Include directives for member types
// Member `obj_name`
// Member `parent_frame`
// Member `cad_data`
#include "rosidl_runtime_c/string_functions.h"

bool
customservices__msg__SpawnObject_Request__init(customservices__msg__SpawnObject_Request * msg)
{
  if (!msg) {
    return false;
  }
  // obj_name
  if (!rosidl_runtime_c__String__init(&msg->obj_name)) {
    customservices__msg__SpawnObject_Request__fini(msg);
    return false;
  }
  // parent_frame
  if (!rosidl_runtime_c__String__init(&msg->parent_frame)) {
    customservices__msg__SpawnObject_Request__fini(msg);
    return false;
  }
  // translation
  // rotation
  // cad_data
  if (!rosidl_runtime_c__String__init(&msg->cad_data)) {
    customservices__msg__SpawnObject_Request__fini(msg);
    return false;
  }
  return true;
}

void
customservices__msg__SpawnObject_Request__fini(customservices__msg__SpawnObject_Request * msg)
{
  if (!msg) {
    return;
  }
  // obj_name
  rosidl_runtime_c__String__fini(&msg->obj_name);
  // parent_frame
  rosidl_runtime_c__String__fini(&msg->parent_frame);
  // translation
  // rotation
  // cad_data
  rosidl_runtime_c__String__fini(&msg->cad_data);
}

bool
customservices__msg__SpawnObject_Request__are_equal(const customservices__msg__SpawnObject_Request * lhs, const customservices__msg__SpawnObject_Request * rhs)
{
  if (!lhs || !rhs) {
    return false;
  }
  // obj_name
  if (!rosidl_runtime_c__String__are_equal(
      &(lhs->obj_name), &(rhs->obj_name)))
  {
    return false;
  }
  // parent_frame
  if (!rosidl_runtime_c__String__are_equal(
      &(lhs->parent_frame), &(rhs->parent_frame)))
  {
    return false;
  }
  // translation
  for (size_t i = 0; i < 3; ++i) {
    if (lhs->translation[i] != rhs->translation[i]) {
      return false;
    }
  }
  // rotation
  for (size_t i = 0; i < 4; ++i) {
    if (lhs->rotation[i] != rhs->rotation[i]) {
      return false;
    }
  }
  // cad_data
  if (!rosidl_runtime_c__String__are_equal(
      &(lhs->cad_data), &(rhs->cad_data)))
  {
    return false;
  }
  return true;
}

bool
customservices__msg__SpawnObject_Request__copy(
  const customservices__msg__SpawnObject_Request * input,
  customservices__msg__SpawnObject_Request * output)
{
  if (!input || !output) {
    return false;
  }
  // obj_name
  if (!rosidl_runtime_c__String__copy(
      &(input->obj_name), &(output->obj_name)))
  {
    return false;
  }
  // parent_frame
  if (!rosidl_runtime_c__String__copy(
      &(input->parent_frame), &(output->parent_frame)))
  {
    return false;
  }
  // translation
  for (size_t i = 0; i < 3; ++i) {
    output->translation[i] = input->translation[i];
  }
  // rotation
  for (size_t i = 0; i < 4; ++i) {
    output->rotation[i] = input->rotation[i];
  }
  // cad_data
  if (!rosidl_runtime_c__String__copy(
      &(input->cad_data), &(output->cad_data)))
  {
    return false;
  }
  return true;
}

customservices__msg__SpawnObject_Request *
customservices__msg__SpawnObject_Request__create()
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  customservices__msg__SpawnObject_Request * msg = (customservices__msg__SpawnObject_Request *)allocator.allocate(sizeof(customservices__msg__SpawnObject_Request), allocator.state);
  if (!msg) {
    return NULL;
  }
  memset(msg, 0, sizeof(customservices__msg__SpawnObject_Request));
  bool success = customservices__msg__SpawnObject_Request__init(msg);
  if (!success) {
    allocator.deallocate(msg, allocator.state);
    return NULL;
  }
  return msg;
}

void
customservices__msg__SpawnObject_Request__destroy(customservices__msg__SpawnObject_Request * msg)
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  if (msg) {
    customservices__msg__SpawnObject_Request__fini(msg);
  }
  allocator.deallocate(msg, allocator.state);
}


bool
customservices__msg__SpawnObject_Request__Sequence__init(customservices__msg__SpawnObject_Request__Sequence * array, size_t size)
{
  if (!array) {
    return false;
  }
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  customservices__msg__SpawnObject_Request * data = NULL;

  if (size) {
    data = (customservices__msg__SpawnObject_Request *)allocator.zero_allocate(size, sizeof(customservices__msg__SpawnObject_Request), allocator.state);
    if (!data) {
      return false;
    }
    // initialize all array elements
    size_t i;
    for (i = 0; i < size; ++i) {
      bool success = customservices__msg__SpawnObject_Request__init(&data[i]);
      if (!success) {
        break;
      }
    }
    if (i < size) {
      // if initialization failed finalize the already initialized array elements
      for (; i > 0; --i) {
        customservices__msg__SpawnObject_Request__fini(&data[i - 1]);
      }
      allocator.deallocate(data, allocator.state);
      return false;
    }
  }
  array->data = data;
  array->size = size;
  array->capacity = size;
  return true;
}

void
customservices__msg__SpawnObject_Request__Sequence__fini(customservices__msg__SpawnObject_Request__Sequence * array)
{
  if (!array) {
    return;
  }
  rcutils_allocator_t allocator = rcutils_get_default_allocator();

  if (array->data) {
    // ensure that data and capacity values are consistent
    assert(array->capacity > 0);
    // finalize all array elements
    for (size_t i = 0; i < array->capacity; ++i) {
      customservices__msg__SpawnObject_Request__fini(&array->data[i]);
    }
    allocator.deallocate(array->data, allocator.state);
    array->data = NULL;
    array->size = 0;
    array->capacity = 0;
  } else {
    // ensure that data, size, and capacity values are consistent
    assert(0 == array->size);
    assert(0 == array->capacity);
  }
}

customservices__msg__SpawnObject_Request__Sequence *
customservices__msg__SpawnObject_Request__Sequence__create(size_t size)
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  customservices__msg__SpawnObject_Request__Sequence * array = (customservices__msg__SpawnObject_Request__Sequence *)allocator.allocate(sizeof(customservices__msg__SpawnObject_Request__Sequence), allocator.state);
  if (!array) {
    return NULL;
  }
  bool success = customservices__msg__SpawnObject_Request__Sequence__init(array, size);
  if (!success) {
    allocator.deallocate(array, allocator.state);
    return NULL;
  }
  return array;
}

void
customservices__msg__SpawnObject_Request__Sequence__destroy(customservices__msg__SpawnObject_Request__Sequence * array)
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  if (array) {
    customservices__msg__SpawnObject_Request__Sequence__fini(array);
  }
  allocator.deallocate(array, allocator.state);
}

bool
customservices__msg__SpawnObject_Request__Sequence__are_equal(const customservices__msg__SpawnObject_Request__Sequence * lhs, const customservices__msg__SpawnObject_Request__Sequence * rhs)
{
  if (!lhs || !rhs) {
    return false;
  }
  if (lhs->size != rhs->size) {
    return false;
  }
  for (size_t i = 0; i < lhs->size; ++i) {
    if (!customservices__msg__SpawnObject_Request__are_equal(&(lhs->data[i]), &(rhs->data[i]))) {
      return false;
    }
  }
  return true;
}

bool
customservices__msg__SpawnObject_Request__Sequence__copy(
  const customservices__msg__SpawnObject_Request__Sequence * input,
  customservices__msg__SpawnObject_Request__Sequence * output)
{
  if (!input || !output) {
    return false;
  }
  if (output->capacity < input->size) {
    const size_t allocation_size =
      input->size * sizeof(customservices__msg__SpawnObject_Request);
    rcutils_allocator_t allocator = rcutils_get_default_allocator();
    customservices__msg__SpawnObject_Request * data =
      (customservices__msg__SpawnObject_Request *)allocator.reallocate(
      output->data, allocation_size, allocator.state);
    if (!data) {
      return false;
    }
    // If reallocation succeeded, memory may or may not have been moved
    // to fulfill the allocation request, invalidating output->data.
    output->data = data;
    for (size_t i = output->capacity; i < input->size; ++i) {
      if (!customservices__msg__SpawnObject_Request__init(&output->data[i])) {
        // If initialization of any new item fails, roll back
        // all previously initialized items. Existing items
        // in output are to be left unmodified.
        for (; i-- > output->capacity; ) {
          customservices__msg__SpawnObject_Request__fini(&output->data[i]);
        }
        return false;
      }
    }
    output->capacity = input->size;
  }
  output->size = input->size;
  for (size_t i = 0; i < input->size; ++i) {
    if (!customservices__msg__SpawnObject_Request__copy(
        &(input->data[i]), &(output->data[i])))
    {
      return false;
    }
  }
  return true;
}
