// generated from rosidl_generator_cpp/resource/idl__struct.hpp.em
// with input from customservices:srv/SpawnObject.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__STRUCT_HPP_
#define CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__STRUCT_HPP_

#include <algorithm>
#include <array>
#include <memory>
#include <string>
#include <vector>

#include "rosidl_runtime_cpp/bounded_vector.hpp"
#include "rosidl_runtime_cpp/message_initialization.hpp"


#ifndef _WIN32
# define DEPRECATED__customservices__srv__SpawnObject_Request __attribute__((deprecated))
#else
# define DEPRECATED__customservices__srv__SpawnObject_Request __declspec(deprecated)
#endif

namespace customservices
{

namespace srv
{

// message struct
template<class ContainerAllocator>
struct SpawnObject_Request_
{
  using Type = SpawnObject_Request_<ContainerAllocator>;

  explicit SpawnObject_Request_(rosidl_runtime_cpp::MessageInitialization _init = rosidl_runtime_cpp::MessageInitialization::ALL)
  {
    if (rosidl_runtime_cpp::MessageInitialization::ALL == _init ||
      rosidl_runtime_cpp::MessageInitialization::ZERO == _init)
    {
      this->obj_name = "";
      this->parent_frame = "";
      std::fill<typename std::array<float, 3>::iterator, float>(this->translation.begin(), this->translation.end(), 0.0f);
      std::fill<typename std::array<float, 4>::iterator, float>(this->rotation.begin(), this->rotation.end(), 0.0f);
      this->cad_data = "";
    }
  }

  explicit SpawnObject_Request_(const ContainerAllocator & _alloc, rosidl_runtime_cpp::MessageInitialization _init = rosidl_runtime_cpp::MessageInitialization::ALL)
  : obj_name(_alloc),
    parent_frame(_alloc),
    translation(_alloc),
    rotation(_alloc),
    cad_data(_alloc)
  {
    if (rosidl_runtime_cpp::MessageInitialization::ALL == _init ||
      rosidl_runtime_cpp::MessageInitialization::ZERO == _init)
    {
      this->obj_name = "";
      this->parent_frame = "";
      std::fill<typename std::array<float, 3>::iterator, float>(this->translation.begin(), this->translation.end(), 0.0f);
      std::fill<typename std::array<float, 4>::iterator, float>(this->rotation.begin(), this->rotation.end(), 0.0f);
      this->cad_data = "";
    }
  }

  // field types and members
  using _obj_name_type =
    std::basic_string<char, std::char_traits<char>, typename std::allocator_traits<ContainerAllocator>::template rebind_alloc<char>>;
  _obj_name_type obj_name;
  using _parent_frame_type =
    std::basic_string<char, std::char_traits<char>, typename std::allocator_traits<ContainerAllocator>::template rebind_alloc<char>>;
  _parent_frame_type parent_frame;
  using _translation_type =
    std::array<float, 3>;
  _translation_type translation;
  using _rotation_type =
    std::array<float, 4>;
  _rotation_type rotation;
  using _cad_data_type =
    std::basic_string<char, std::char_traits<char>, typename std::allocator_traits<ContainerAllocator>::template rebind_alloc<char>>;
  _cad_data_type cad_data;

  // setters for named parameter idiom
  Type & set__obj_name(
    const std::basic_string<char, std::char_traits<char>, typename std::allocator_traits<ContainerAllocator>::template rebind_alloc<char>> & _arg)
  {
    this->obj_name = _arg;
    return *this;
  }
  Type & set__parent_frame(
    const std::basic_string<char, std::char_traits<char>, typename std::allocator_traits<ContainerAllocator>::template rebind_alloc<char>> & _arg)
  {
    this->parent_frame = _arg;
    return *this;
  }
  Type & set__translation(
    const std::array<float, 3> & _arg)
  {
    this->translation = _arg;
    return *this;
  }
  Type & set__rotation(
    const std::array<float, 4> & _arg)
  {
    this->rotation = _arg;
    return *this;
  }
  Type & set__cad_data(
    const std::basic_string<char, std::char_traits<char>, typename std::allocator_traits<ContainerAllocator>::template rebind_alloc<char>> & _arg)
  {
    this->cad_data = _arg;
    return *this;
  }

  // constant declarations

  // pointer types
  using RawPtr =
    customservices::srv::SpawnObject_Request_<ContainerAllocator> *;
  using ConstRawPtr =
    const customservices::srv::SpawnObject_Request_<ContainerAllocator> *;
  using SharedPtr =
    std::shared_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator>>;
  using ConstSharedPtr =
    std::shared_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator> const>;

  template<typename Deleter = std::default_delete<
      customservices::srv::SpawnObject_Request_<ContainerAllocator>>>
  using UniquePtrWithDeleter =
    std::unique_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator>, Deleter>;

  using UniquePtr = UniquePtrWithDeleter<>;

  template<typename Deleter = std::default_delete<
      customservices::srv::SpawnObject_Request_<ContainerAllocator>>>
  using ConstUniquePtrWithDeleter =
    std::unique_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator> const, Deleter>;
  using ConstUniquePtr = ConstUniquePtrWithDeleter<>;

  using WeakPtr =
    std::weak_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator>>;
  using ConstWeakPtr =
    std::weak_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator> const>;

  // pointer types similar to ROS 1, use SharedPtr / ConstSharedPtr instead
  // NOTE: Can't use 'using' here because GNU C++ can't parse attributes properly
  typedef DEPRECATED__customservices__srv__SpawnObject_Request
    std::shared_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator>>
    Ptr;
  typedef DEPRECATED__customservices__srv__SpawnObject_Request
    std::shared_ptr<customservices::srv::SpawnObject_Request_<ContainerAllocator> const>
    ConstPtr;

  // comparison operators
  bool operator==(const SpawnObject_Request_ & other) const
  {
    if (this->obj_name != other.obj_name) {
      return false;
    }
    if (this->parent_frame != other.parent_frame) {
      return false;
    }
    if (this->translation != other.translation) {
      return false;
    }
    if (this->rotation != other.rotation) {
      return false;
    }
    if (this->cad_data != other.cad_data) {
      return false;
    }
    return true;
  }
  bool operator!=(const SpawnObject_Request_ & other) const
  {
    return !this->operator==(other);
  }
};  // struct SpawnObject_Request_

// alias to use template instance with default allocator
using SpawnObject_Request =
  customservices::srv::SpawnObject_Request_<std::allocator<void>>;

// constant definitions

}  // namespace srv

}  // namespace customservices


#ifndef _WIN32
# define DEPRECATED__customservices__srv__SpawnObject_Response __attribute__((deprecated))
#else
# define DEPRECATED__customservices__srv__SpawnObject_Response __declspec(deprecated)
#endif

namespace customservices
{

namespace srv
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
    customservices::srv::SpawnObject_Response_<ContainerAllocator> *;
  using ConstRawPtr =
    const customservices::srv::SpawnObject_Response_<ContainerAllocator> *;
  using SharedPtr =
    std::shared_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator>>;
  using ConstSharedPtr =
    std::shared_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator> const>;

  template<typename Deleter = std::default_delete<
      customservices::srv::SpawnObject_Response_<ContainerAllocator>>>
  using UniquePtrWithDeleter =
    std::unique_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator>, Deleter>;

  using UniquePtr = UniquePtrWithDeleter<>;

  template<typename Deleter = std::default_delete<
      customservices::srv::SpawnObject_Response_<ContainerAllocator>>>
  using ConstUniquePtrWithDeleter =
    std::unique_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator> const, Deleter>;
  using ConstUniquePtr = ConstUniquePtrWithDeleter<>;

  using WeakPtr =
    std::weak_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator>>;
  using ConstWeakPtr =
    std::weak_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator> const>;

  // pointer types similar to ROS 1, use SharedPtr / ConstSharedPtr instead
  // NOTE: Can't use 'using' here because GNU C++ can't parse attributes properly
  typedef DEPRECATED__customservices__srv__SpawnObject_Response
    std::shared_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator>>
    Ptr;
  typedef DEPRECATED__customservices__srv__SpawnObject_Response
    std::shared_ptr<customservices::srv::SpawnObject_Response_<ContainerAllocator> const>
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
  customservices::srv::SpawnObject_Response_<std::allocator<void>>;

// constant definitions

}  // namespace srv

}  // namespace customservices

namespace customservices
{

namespace srv
{

struct SpawnObject
{
  using Request = customservices::srv::SpawnObject_Request;
  using Response = customservices::srv::SpawnObject_Response;
};

}  // namespace srv

}  // namespace customservices

#endif  // CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__STRUCT_HPP_
