// generated from rosidl_generator_cpp/resource/idl__struct.hpp.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__STRUCT_HPP_
#define CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__STRUCT_HPP_

#include <algorithm>
#include <array>
#include <memory>
#include <string>
#include <vector>

#include "rosidl_runtime_cpp/bounded_vector.hpp"
#include "rosidl_runtime_cpp/message_initialization.hpp"


#ifndef _WIN32
# define DEPRECATED__customservices__msg__SpawnObject_Response __attribute__((deprecated))
#else
# define DEPRECATED__customservices__msg__SpawnObject_Response __declspec(deprecated)
#endif

namespace customservices
{

namespace msg
{

// message struct
template<class ContainerAllocator>
struct SpawnObject_Response_
{
  using Type = SpawnObject_Response_<ContainerAllocator>;

  explicit SpawnObject_Response_(rosidl_runtime_cpp::MessageInitialization _init = rosidl_runtime_cpp::MessageInitialization::ALL)
  {
    if (rosidl_runtime_cpp::MessageInitialization::ALL == _init ||
      rosidl_runtime_cpp::MessageInitialization::ZERO == _init)
    {
      this->success = false;
    }
  }

  explicit SpawnObject_Response_(const ContainerAllocator & _alloc, rosidl_runtime_cpp::MessageInitialization _init = rosidl_runtime_cpp::MessageInitialization::ALL)
  {
    (void)_alloc;
    if (rosidl_runtime_cpp::MessageInitialization::ALL == _init ||
      rosidl_runtime_cpp::MessageInitialization::ZERO == _init)
    {
      this->success = false;
    }
  }

  // field types and members
  using _success_type =
    bool;
  _success_type success;

  // setters for named parameter idiom
  Type & set__success(
    const bool & _arg)
  {
    this->success = _arg;
    return *this;
  }

  // constant declarations

  // pointer types
  using RawPtr =
    customservices::msg::SpawnObject_Response_<ContainerAllocator> *;
  using ConstRawPtr =
    const customservices::msg::SpawnObject_Response_<ContainerAllocator> *;
  using SharedPtr =
    std::shared_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator>>;
  using ConstSharedPtr =
    std::shared_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator> const>;

  template<typename Deleter = std::default_delete<
      customservices::msg::SpawnObject_Response_<ContainerAllocator>>>
  using UniquePtrWithDeleter =
    std::unique_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator>, Deleter>;

  using UniquePtr = UniquePtrWithDeleter<>;

  template<typename Deleter = std::default_delete<
      customservices::msg::SpawnObject_Response_<ContainerAllocator>>>
  using ConstUniquePtrWithDeleter =
    std::unique_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator> const, Deleter>;
  using ConstUniquePtr = ConstUniquePtrWithDeleter<>;

  using WeakPtr =
    std::weak_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator>>;
  using ConstWeakPtr =
    std::weak_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator> const>;

  // pointer types similar to ROS 1, use SharedPtr / ConstSharedPtr instead
  // NOTE: Can't use 'using' here because GNU C++ can't parse attributes properly
  typedef DEPRECATED__customservices__msg__SpawnObject_Response
    std::shared_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator>>
    Ptr;
  typedef DEPRECATED__customservices__msg__SpawnObject_Response
    std::shared_ptr<customservices::msg::SpawnObject_Response_<ContainerAllocator> const>
    ConstPtr;

  // comparison operators
  bool operator==(const SpawnObject_Response_ & other) const
  {
    if (this->success != other.success) {
      return false;
    }
    return true;
  }
  bool operator!=(const SpawnObject_Response_ & other) const
  {
    return !this->operator==(other);
  }
};  // struct SpawnObject_Response_

// alias to use template instance with default allocator
using SpawnObject_Response =
  customservices::msg::SpawnObject_Response_<std::allocator<void>>;

// constant definitions

}  // namespace msg

}  // namespace customservices

#endif  // CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__STRUCT_HPP_
