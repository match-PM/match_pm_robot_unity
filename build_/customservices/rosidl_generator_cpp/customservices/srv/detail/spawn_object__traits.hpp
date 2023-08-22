// generated from rosidl_generator_cpp/resource/idl__traits.hpp.em
// with input from customservices:srv/SpawnObject.idl
// generated code does not contain a copyright notice

#ifndef CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__TRAITS_HPP_
#define CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__TRAITS_HPP_

#include <stdint.h>

#include <sstream>
#include <string>
#include <type_traits>

#include "customservices/srv/detail/spawn_object__struct.hpp"
#include "rosidl_runtime_cpp/traits.hpp"

namespace customservices
{

namespace srv
{

inline void to_flow_style_yaml(
  const SpawnObject_Request & msg,
  std::ostream & out)
{
  out << "{";
  // member: obj_name
  {
    out << "obj_name: ";
    rosidl_generator_traits::value_to_yaml(msg.obj_name, out);
    out << ", ";
  }

  // member: parent_frame
  {
    out << "parent_frame: ";
    rosidl_generator_traits::value_to_yaml(msg.parent_frame, out);
    out << ", ";
  }

  // member: translation
  {
    if (msg.translation.size() == 0) {
      out << "translation: []";
    } else {
      out << "translation: [";
      size_t pending_items = msg.translation.size();
      for (auto item : msg.translation) {
        rosidl_generator_traits::value_to_yaml(item, out);
        if (--pending_items > 0) {
          out << ", ";
        }
      }
      out << "]";
    }
    out << ", ";
  }

  // member: rotation
  {
    if (msg.rotation.size() == 0) {
      out << "rotation: []";
    } else {
      out << "rotation: [";
      size_t pending_items = msg.rotation.size();
      for (auto item : msg.rotation) {
        rosidl_generator_traits::value_to_yaml(item, out);
        if (--pending_items > 0) {
          out << ", ";
        }
      }
      out << "]";
    }
    out << ", ";
  }

  // member: cad_data
  {
    out << "cad_data: ";
    rosidl_generator_traits::value_to_yaml(msg.cad_data, out);
  }
  out << "}";
}  // NOLINT(readability/fn_size)

inline void to_block_style_yaml(
  const SpawnObject_Request & msg,
  std::ostream & out, size_t indentation = 0)
{
  // member: obj_name
  {
    if (indentation > 0) {
      out << std::string(indentation, ' ');
    }
    out << "obj_name: ";
    rosidl_generator_traits::value_to_yaml(msg.obj_name, out);
    out << "\n";
  }

  // member: parent_frame
  {
    if (indentation > 0) {
      out << std::string(indentation, ' ');
    }
    out << "parent_frame: ";
    rosidl_generator_traits::value_to_yaml(msg.parent_frame, out);
    out << "\n";
  }

  // member: translation
  {
    if (indentation > 0) {
      out << std::string(indentation, ' ');
    }
    if (msg.translation.size() == 0) {
      out << "translation: []\n";
    } else {
      out << "translation:\n";
      for (auto item : msg.translation) {
        if (indentation > 0) {
          out << std::string(indentation, ' ');
        }
        out << "- ";
        rosidl_generator_traits::value_to_yaml(item, out);
        out << "\n";
      }
    }
  }

  // member: rotation
  {
    if (indentation > 0) {
      out << std::string(indentation, ' ');
    }
    if (msg.rotation.size() == 0) {
      out << "rotation: []\n";
    } else {
      out << "rotation:\n";
      for (auto item : msg.rotation) {
        if (indentation > 0) {
          out << std::string(indentation, ' ');
        }
        out << "- ";
        rosidl_generator_traits::value_to_yaml(item, out);
        out << "\n";
      }
    }
  }

  // member: cad_data
  {
    if (indentation > 0) {
      out << std::string(indentation, ' ');
    }
    out << "cad_data: ";
    rosidl_generator_traits::value_to_yaml(msg.cad_data, out);
    out << "\n";
  }
}  // NOLINT(readability/fn_size)

inline std::string to_yaml(const SpawnObject_Request & msg, bool use_flow_style = false)
{
  std::ostringstream out;
  if (use_flow_style) {
    to_flow_style_yaml(msg, out);
  } else {
    to_block_style_yaml(msg, out);
  }
  return out.str();
}

}  // namespace srv

}  // namespace customservices

namespace rosidl_generator_traits
{

[[deprecated("use customservices::srv::to_block_style_yaml() instead")]]
inline void to_yaml(
  const customservices::srv::SpawnObject_Request & msg,
  std::ostream & out, size_t indentation = 0)
{
  customservices::srv::to_block_style_yaml(msg, out, indentation);
}

[[deprecated("use customservices::srv::to_yaml() instead")]]
inline std::string to_yaml(const customservices::srv::SpawnObject_Request & msg)
{
  return customservices::srv::to_yaml(msg);
}

template<>
inline const char * data_type<customservices::srv::SpawnObject_Request>()
{
  return "customservices::srv::SpawnObject_Request";
}

template<>
inline const char * name<customservices::srv::SpawnObject_Request>()
{
  return "customservices/srv/SpawnObject_Request";
}

template<>
struct has_fixed_size<customservices::srv::SpawnObject_Request>
  : std::integral_constant<bool, false> {};

template<>
struct has_bounded_size<customservices::srv::SpawnObject_Request>
  : std::integral_constant<bool, false> {};

template<>
struct is_message<customservices::srv::SpawnObject_Request>
  : std::true_type {};

}  // namespace rosidl_generator_traits

namespace customservices
{

namespace srv
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

}  // namespace srv

}  // namespace customservices

namespace rosidl_generator_traits
{

[[deprecated("use customservices::srv::to_block_style_yaml() instead")]]
inline void to_yaml(
  const customservices::srv::SpawnObject_Response & msg,
  std::ostream & out, size_t indentation = 0)
{
  customservices::srv::to_block_style_yaml(msg, out, indentation);
}

[[deprecated("use customservices::srv::to_yaml() instead")]]
inline std::string to_yaml(const customservices::srv::SpawnObject_Response & msg)
{
  return customservices::srv::to_yaml(msg);
}

template<>
inline const char * data_type<customservices::srv::SpawnObject_Response>()
{
  return "customservices::srv::SpawnObject_Response";
}

template<>
inline const char * name<customservices::srv::SpawnObject_Response>()
{
  return "customservices/srv/SpawnObject_Response";
}

template<>
struct has_fixed_size<customservices::srv::SpawnObject_Response>
  : std::integral_constant<bool, true> {};

template<>
struct has_bounded_size<customservices::srv::SpawnObject_Response>
  : std::integral_constant<bool, true> {};

template<>
struct is_message<customservices::srv::SpawnObject_Response>
  : std::true_type {};

}  // namespace rosidl_generator_traits

namespace rosidl_generator_traits
{

template<>
inline const char * data_type<customservices::srv::SpawnObject>()
{
  return "customservices::srv::SpawnObject";
}

template<>
inline const char * name<customservices::srv::SpawnObject>()
{
  return "customservices/srv/SpawnObject";
}

template<>
struct has_fixed_size<customservices::srv::SpawnObject>
  : std::integral_constant<
    bool,
    has_fixed_size<customservices::srv::SpawnObject_Request>::value &&
    has_fixed_size<customservices::srv::SpawnObject_Response>::value
  >
{
};

template<>
struct has_bounded_size<customservices::srv::SpawnObject>
  : std::integral_constant<
    bool,
    has_bounded_size<customservices::srv::SpawnObject_Request>::value &&
    has_bounded_size<customservices::srv::SpawnObject_Response>::value
  >
{
};

template<>
struct is_service<customservices::srv::SpawnObject>
  : std::true_type
{
};

template<>
struct is_service_request<customservices::srv::SpawnObject_Request>
  : std::true_type
{
};

template<>
struct is_service_response<customservices::srv::SpawnObject_Response>
  : std::true_type
{
};

}  // namespace rosidl_generator_traits

#endif  // CUSTOMSERVICES__SRV__DETAIL__SPAWN_OBJECT__TRAITS_HPP_
