// generated from rosidl_generator_cpp/resource/idl__traits.hpp.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__TRAITS_HPP_
#define CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__TRAITS_HPP_

#include <stdint.h>

#include <sstream>
#include <string>
#include <type_traits>

#include "customservices/msg/detail/spawn_object__response__struct.hpp"
#include "rosidl_runtime_cpp/traits.hpp"

namespace customservices
{

namespace msg
{

inline void to_flow_style_yaml(
  const SpawnObject_Response & msg,
  std::ostream & out)
{
  out << "{";
  // member: success
  {
    out << "success: ";
    rosidl_generator_traits::value_to_yaml(msg.success, out);
  }
  out << "}";
}  // NOLINT(readability/fn_size)

inline void to_block_style_yaml(
  const SpawnObject_Response & msg,
  std::ostream & out, size_t indentation = 0)
{
  // member: success
  {
    if (indentation > 0) {
      out << std::string(indentation, ' ');
    }
    out << "success: ";
    rosidl_generator_traits::value_to_yaml(msg.success, out);
    out << "\n";
  }
}  // NOLINT(readability/fn_size)

inline std::string to_yaml(const SpawnObject_Response & msg, bool use_flow_style = false)
{
  std::ostringstream out;
  if (use_flow_style) {
    to_flow_style_yaml(msg, out);
  } else {
    to_block_style_yaml(msg, out);
  }
  return out.str();
}

}  // namespace msg

}  // namespace customservices

namespace rosidl_generator_traits
{

[[deprecated("use customservices::msg::to_block_style_yaml() instead")]]
inline void to_yaml(
  const customservices::msg::SpawnObject_Response & msg,
  std::ostream & out, size_t indentation = 0)
{
  customservices::msg::to_block_style_yaml(msg, out, indentation);
}

[[deprecated("use customservices::msg::to_yaml() instead")]]
inline std::string to_yaml(const customservices::msg::SpawnObject_Response & msg)
{
  return customservices::msg::to_yaml(msg);
}

template<>
inline const char * data_type<customservices::msg::SpawnObject_Response>()
{
  return "customservices::msg::SpawnObject_Response";
}

template<>
inline const char * name<customservices::msg::SpawnObject_Response>()
{
  return "customservices/msg/SpawnObject_Response";
}

template<>
struct has_fixed_size<customservices::msg::SpawnObject_Response>
  : std::integral_constant<bool, true> {};

template<>
struct has_bounded_size<customservices::msg::SpawnObject_Response>
  : std::integral_constant<bool, true> {};

template<>
struct is_message<customservices::msg::SpawnObject_Response>
  : std::true_type {};

}  // namespace rosidl_generator_traits

#endif  // CUSTOMSERVICES__MSG__DETAIL__SPAWN_OBJECT__RESPONSE__TRAITS_HPP_
