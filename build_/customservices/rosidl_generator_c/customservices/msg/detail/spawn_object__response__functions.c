// generated from rosidl_generator_c/resource/idl__functions.c.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice
#include "customservices/msg/detail/spawn_object__response__functions.h"

#include <assert.h>
#include <stdbool.h>
#include <stdlib.h>
#include <string.h>

#include "rcutils/allocator.h"


bool
customservices__msg__SpawnObject_Response__init(customservices__msg__SpawnObject_Response * msg)
{
  if (!msg) {
    return false;
  }
  // success
  return true;
}

void
customservices__msg__SpawnObject_Response__fini(customservices__msg__SpawnObject_Response * msg)
{
  if (!msg) {
    return;
  }
  // success
}

bool
customservices__msg__SpawnObject_Response__are_equal(const customservices__msg__SpawnObject_Response * lhs, const customservices__msg__SpawnObject_Response * rhs)
{
  if (!lhs || !rhs) {
    return false;
  }
  // success
  if (lhs->success != rhs->success) {
    return false;
  }
  return true;
}

bool
customservices__msg__SpawnObject_Response__copy(
  const customservices__msg__SpawnObject_Response * input,
  customservices__msg__SpawnObject_Response * output)
{
  if (!input || !output) {
    return false;
  }
  // success
  output->success = input->success;
  return true;
}

customservices__msg__SpawnObject_Response *
customservices__msg__SpawnObject_Response__create()
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  customservices__msg__SpawnObject_Response * msg = (customservices__msg__SpawnObject_Response *)allocator.allocate(sizeof(customservices__msg__SpawnObject_Response), allocator.state);
  if (!msg) {
    return NULL;
  }
  memset(msg, 0, sizeof(customservices__msg__SpawnObject_Response));
  bool success = customservices__msg__SpawnObject_Response__init(msg);
  if (!success) {
    allocator.deallocate(msg, allocator.state);
    return NULL;
  }
  return msg;
}

void
customservices__msg__SpawnObject_Response__destroy(customservices__msg__SpawnObject_Response * msg)
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  if (msg) {
    customservices__msg__SpawnObject_Response__fini(msg);
  }
  allocator.deallocate(msg, allocator.state);
}


bool
customservices__msg__SpawnObject_Response__Sequence__init(customservices__msg__SpawnObject_Response__Sequence * array, size_t size)
{
  if (!array) {
    return false;
  }
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  customservices__msg__SpawnObject_Response * data = NULL;

  if (size) {
    data = (customservices__msg__SpawnObject_Response *)allocator.zero_allocate(size, sizeof(customservices__msg__SpawnObject_Response), allocator.state);
    if (!data) {
      return false;
    }
    // initialize all array elements
    size_t i;
    for (i = 0; i < size; ++i) {
      bool success = customservices__msg__SpawnObject_Response__init(&data[i]);
      if (!success) {
        break;
      }
    }
    if (i < size) {
      // if initialization failed finalize the already initialized array elements
      for (; i > 0; --i) {
        customservices__msg__SpawnObject_Response__fini(&data[i - 1]);
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
customservices__msg__SpawnObject_Response__Sequence__fini(customservices__msg__SpawnObject_Response__Sequence * array)
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
      customservices__msg__SpawnObject_Response__fini(&array->data[i]);
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

customservices__msg__SpawnObject_Response__Sequence *
customservices__msg__SpawnObject_Response__Sequence__create(size_t size)
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  customservices__msg__SpawnObject_Response__Sequence * array = (customservices__msg__SpawnObject_Response__Sequence *)allocator.allocate(sizeof(customservices__msg__SpawnObject_Response__Sequence), allocator.state);
  if (!array) {
    return NULL;
  }
  bool success = customservices__msg__SpawnObject_Response__Sequence__init(array, size);
  if (!success) {
    allocator.deallocate(array, allocator.state);
    return NULL;
  }
  return array;
}

void
customservices__msg__SpawnObject_Response__Sequence__destroy(customservices__msg__SpawnObject_Response__Sequence * array)
{
  rcutils_allocator_t allocator = rcutils_get_default_allocator();
  if (array) {
    customservices__msg__SpawnObject_Response__Sequence__fini(array);
  }
  allocator.deallocate(array, allocator.state);
}

bool
customservices__msg__SpawnObject_Response__Sequence__are_equal(const customservices__msg__SpawnObject_Response__Sequence * lhs, const customservices__msg__SpawnObject_Response__Sequence * rhs)
{
  if (!lhs || !rhs) {
    return false;
  }
  if (lhs->size != rhs->size) {
    return false;
  }
  for (size_t i = 0; i < lhs->size; ++i) {
    if (!customservices__msg__SpawnObject_Response__are_equal(&(lhs->data[i]), &(rhs->data[i]))) {
      return false;
    }
  }
  return true;
}

bool
customservices__msg__SpawnObject_Response__Sequence__copy(
  const customservices__msg__SpawnObject_Response__Sequence * input,
  customservices__msg__SpawnObject_Response__Sequence * output)
{
  if (!input || !output) {
    return false;
  }
  if (output->capacity < input->size) {
    const size_t allocation_size =
      input->size * sizeof(customservices__msg__SpawnObject_Response);
    rcutils_allocator_t allocator = rcutils_get_default_allocator();
    customservices__msg__SpawnObject_Response * data =
      (customservices__msg__SpawnObject_Response *)allocator.reallocate(
      output->data, allocation_size, allocator.state);
    if (!data) {
      return false;
    }
    // If reallocation succeeded, memory may or may not have been moved
    // to fulfill the allocation request, invalidating output->data.
    output->data = data;
    for (size_t i = output->capacity; i < input->size; ++i) {
      if (!customservices__msg__SpawnObject_Response__init(&output->data[i])) {
        // If initialization of any new item fails, roll back
        // all previously initialized items. Existing items
        // in output are to be left unmodified.
        for (; i-- > output->capacity; ) {
          customservices__msg__SpawnObject_Response__fini(&output->data[i]);
        }
        return false;
      }
    }
    output->capacity = input->size;
  }
  output->size = input->size;
  for (size_t i = 0; i < input->size; ++i) {
    if (!customservices__msg__SpawnObject_Response__copy(
        &(input->data[i]), &(output->data[i])))
    {
      return false;
    }
  }
  return true;
}
