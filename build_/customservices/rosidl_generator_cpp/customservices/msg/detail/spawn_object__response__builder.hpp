// generated from rosidl_generator_cpp/resource/idl__builder.hpp.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__BUILDER_HPP_
#define CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__BUILDER_HPP_

#include <algorithm>
#include <utility>

#include "customservices/msg/detail/spawn_object__response__struct.hpp"
#include "rosidl_runtime_cpp/message_initialization.hpp"


namespace customservices
{

namespace msg
{

namespace builder
{

class Init_SpawnObject_Response_success
{
public:
  Init_SpawnObject_Response_success()
  : msg_(::rosidl_runtime_cpp::MessageInitialization::SKIP)
  {}
  ::customservices::msg::SpawnObject_Response success(::customservices::msg::SpawnObject_Response::_success_type arg)
  {
    msg_.success = std::move(arg);
    return std::move(msg_);
  }

private:
  ::customservices::msg::SpawnObject_Response msg_;
};

}  // namespace builder

}  // namespace msg

template<typename MessageType>
auto build();

template<>
inline
auto build<::customservices::msg::SpawnObject_Response>()
{
  return customservices::msg::builder::Init_SpawnObject_Response_success();
}

}  // namespace customservices

#endif  // CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__BUILDER_HPP_
