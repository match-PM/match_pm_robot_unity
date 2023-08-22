// generated from rosidl_generator_cpp/resource/idl__builder.hpp.em
// with input from customservices:srv/SpawnObject.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__BUILDER_HPP_
#define CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__BUILDER_HPP_

#include <algorithm>
#include <utility>

#include "customservices/srv/detail/spawn_object__struct.hpp"
#include "rosidl_runtime_cpp/message_initialization.hpp"


namespace customservices
{

namespace srv
{

namespace builder
{

class Init_SpawnObject_Request_cad_data
{
public:
  explicit Init_SpawnObject_Request_cad_data(::customservices::srv::SpawnObject_Request & msg)
  : msg_(msg)
  {}
  ::customservices::srv::SpawnObject_Request cad_data(::customservices::srv::SpawnObject_Request::_cad_data_type arg)
  {
    msg_.cad_data = std::move(arg);
    return std::move(msg_);
  }

private:
  ::customservices::srv::SpawnObject_Request msg_;
};

class Init_SpawnObject_Request_rotation
{
public:
  explicit Init_SpawnObject_Request_rotation(::customservices::srv::SpawnObject_Request & msg)
  : msg_(msg)
  {}
  Init_SpawnObject_Request_cad_data rotation(::customservices::srv::SpawnObject_Request::_rotation_type arg)
  {
    msg_.rotation = std::move(arg);
    return Init_SpawnObject_Request_cad_data(msg_);
  }

private:
  ::customservices::srv::SpawnObject_Request msg_;
};

class Init_SpawnObject_Request_translation
{
public:
  explicit Init_SpawnObject_Request_translation(::customservices::srv::SpawnObject_Request & msg)
  : msg_(msg)
  {}
  Init_SpawnObject_Request_rotation translation(::customservices::srv::SpawnObject_Request::_translation_type arg)
  {
    msg_.translation = std::move(arg);
    return Init_SpawnObject_Request_rotation(msg_);
  }

private:
  ::customservices::srv::SpawnObject_Request msg_;
};

class Init_SpawnObject_Request_parent_frame
{
public:
  explicit Init_SpawnObject_Request_parent_frame(::customservices::srv::SpawnObject_Request & msg)
  : msg_(msg)
  {}
  Init_SpawnObject_Request_translation parent_frame(::customservices::srv::SpawnObject_Request::_parent_frame_type arg)
  {
    msg_.parent_frame = std::move(arg);
    return Init_SpawnObject_Request_translation(msg_);
  }

private:
  ::customservices::srv::SpawnObject_Request msg_;
};

class Init_SpawnObject_Request_obj_name
{
public:
  Init_SpawnObject_Request_obj_name()
  : msg_(::rosidl_runtime_cpp::MessageInitialization::SKIP)
  {}
  Init_SpawnObject_Request_parent_frame obj_name(::customservices::srv::SpawnObject_Request::_obj_name_type arg)
  {
    msg_.obj_name = std::move(arg);
    return Init_SpawnObject_Request_parent_frame(msg_);
  }

private:
  ::customservices::srv::SpawnObject_Request msg_;
};

}  // namespace builder

}  // namespace srv

template<typename MessageType>
auto build();

template<>
inline
auto build<::customservices::srv::SpawnObject_Request>()
{
  return customservices::srv::builder::Init_SpawnObject_Request_obj_name();
}

}  // namespace customservices


namespace customservices
{

namespace srv
{

namespace builder
{

class Init_SpawnObject_Response_success
{
public:
  Init_SpawnObject_Response_success()
  : msg_(::rosidl_runtime_cpp::MessageInitialization::SKIP)
  {}
  ::customservices::srv::SpawnObject_Response success(::customservices::srv::SpawnObject_Response::_success_type arg)
  {
    msg_.success = std::move(arg);
    return std::move(msg_);
  }

private:
  ::customservices::srv::SpawnObject_Response msg_;
};

}  // namespace builder

}  // namespace srv

template<typename MessageType>
auto build();

template<>
inline
auto build<::customservices::srv::SpawnObject_Response>()
{
  return customservices::srv::builder::Init_SpawnObject_Response_success();
}

}  // namespace customservices

#endif  // CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__BUILDER_HPP_
