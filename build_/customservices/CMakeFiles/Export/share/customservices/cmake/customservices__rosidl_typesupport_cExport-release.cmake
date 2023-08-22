#----------------------------------------------------------------
# Generated CMake target import file for configuration "Release".
#----------------------------------------------------------------

# Commands may need to know the format version.
set(CMAKE_IMPORT_FILE_VERSION 1)

# Import target "customservices::customservices__rosidl_typesupport_c" for configuration "Release"
set_property(TARGET customservices::customservices__rosidl_typesupport_c APPEND PROPERTY IMPORTED_CONFIGURATIONS RELEASE)
set_target_properties(customservices::customservices__rosidl_typesupport_c PROPERTIES
  IMPORTED_LINK_DEPENDENT_LIBRARIES_RELEASE "rosidl_runtime_c::rosidl_runtime_c;rosidl_typesupport_c::rosidl_typesupport_c"
  IMPORTED_LOCATION_RELEASE "${_IMPORT_PREFIX}/lib/libcustomservices__rosidl_typesupport_c.so"
  IMPORTED_SONAME_RELEASE "libcustomservices__rosidl_typesupport_c.so"
  )

list(APPEND _IMPORT_CHECK_TARGETS customservices::customservices__rosidl_typesupport_c )
list(APPEND _IMPORT_CHECK_FILES_FOR_customservices::customservices__rosidl_typesupport_c "${_IMPORT_PREFIX}/lib/libcustomservices__rosidl_typesupport_c.so" )

# Commands beyond this point should not need to know the version.
set(CMAKE_IMPORT_FILE_VERSION)
