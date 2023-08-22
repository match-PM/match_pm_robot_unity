// generated from rosidl_generator_cs/resource/idl.cs.em
// with input from customservices:msg/SpawnObject_Response.idl
// generated code does not contain a copyright notice

//TODO (adamdbrw): include depending on what is needed
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ROS2;
using ROS2.Internal;




namespace customservices
{
namespace msg
{
// message class
public class SpawnObject_Response : MessageInternals, Message
{
  private IntPtr _handle;
  private static readonly DllLoadUtils dllLoadUtils;

  public bool IsDisposed { get { return disposed; } }
  private bool disposed;

  // constant declarations

  // members
  public bool Success { get; set; }

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate IntPtr NativeGetTypeSupportType();
  private static NativeGetTypeSupportType native_get_typesupport = null;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate IntPtr NativeCreateNativeMessageType();
  private static NativeCreateNativeMessageType native_create_native_message = null;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void NativeDestroyNativeMessageType(IntPtr messageHandle);
  private static NativeDestroyNativeMessageType native_destroy_native_message = null;

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate bool NativeReadFieldSuccessType(
    IntPtr messageHandle);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void NativeWriteFieldSuccessType(
    IntPtr messageHandle, bool value);


  private static NativeReadFieldSuccessType native_read_field_success = null;
  private static NativeWriteFieldSuccessType native_write_field_success = null;

  // This is done to preload before ros2 rmw_implementation attempts to find custom message library (and fails without absolute path)
  static private void MessageTypeSupportPreload()
  {
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    { //only affects Linux since on Windows PATH can be set effectively, dynamically
        const string rmw_fastrtps = "rmw_fastrtps_cpp";
        var rmw_implementation = Environment.GetEnvironmentVariable("RMW_IMPLEMENTATION");
        if (rmw_implementation == null)
        {
          var ros_distro = Environment.GetEnvironmentVariable("ROS_DISTRO");
          if (ros_distro == "galactic")
          { // no preloads for CycloneDDS, default for galactic
            return;
          }
          rmw_implementation = rmw_fastrtps; // default for all other distros
        }

        // TODO - generalize to Connext and other implementations
        if (rmw_implementation == rmw_fastrtps)
        { // TODO - get rcl level constants, e.g. rosidl_typesupport_fastrtps_c__identifier
          // Load typesupport for fastrtps (_c depends on _cpp)
          var loadUtils = DllLoadUtilsFactory.GetDllLoadUtils();
          IntPtr messageLibraryTypesupportFastRTPS_CPP = loadUtils.LoadLibraryNoSuffix("customservices__rosidl_typesupport_fastrtps_cpp");
          IntPtr messageLibraryTypesupportFastRTPS_C = loadUtils.LoadLibraryNoSuffix("customservices__rosidl_typesupport_fastrtps_c");
      }
    }
  }

  static SpawnObject_Response()
  {
    dllLoadUtils = DllLoadUtilsFactory.GetDllLoadUtils();
    IntPtr messageLibraryTypesupport = dllLoadUtils.LoadLibraryNoSuffix("customservices__rosidl_typesupport_c");
    IntPtr messageLibraryGenerator = dllLoadUtils.LoadLibraryNoSuffix("customservices__rosidl_generator_c");
    IntPtr messageLibraryIntro = dllLoadUtils.LoadLibraryNoSuffix("customservices__rosidl_typesupport_introspection_c");
    MessageTypeSupportPreload();

    IntPtr nativelibrary = dllLoadUtils.LoadLibrary("customservices_spawn_object__response__rosidl_typesupport_c");
    IntPtr native_get_typesupport_ptr = dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Response_native_get_type_support");
    SpawnObject_Response.native_get_typesupport = (NativeGetTypeSupportType)Marshal.GetDelegateForFunctionPointer(
      native_get_typesupport_ptr, typeof(NativeGetTypeSupportType));

    IntPtr native_create_native_message_ptr = dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Response_native_create_native_message");
    SpawnObject_Response.native_create_native_message = (NativeCreateNativeMessageType)Marshal.GetDelegateForFunctionPointer(
      native_create_native_message_ptr, typeof(NativeCreateNativeMessageType));

    IntPtr native_destroy_native_message_ptr = dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Response_native_destroy_native_message");
    SpawnObject_Response.native_destroy_native_message = (NativeDestroyNativeMessageType)Marshal.GetDelegateForFunctionPointer(
      native_destroy_native_message_ptr, typeof(NativeDestroyNativeMessageType));

    IntPtr native_read_field_success_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Response_native_read_field_success");
    SpawnObject_Response.native_read_field_success =
      (NativeReadFieldSuccessType)Marshal.GetDelegateForFunctionPointer(
      native_read_field_success_ptr, typeof(NativeReadFieldSuccessType));

    IntPtr native_write_field_success_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Response_native_write_field_success");
    SpawnObject_Response.native_write_field_success =
      (NativeWriteFieldSuccessType)Marshal.GetDelegateForFunctionPointer(
      native_write_field_success_ptr, typeof(NativeWriteFieldSuccessType));
  }

  public IntPtr TypeSupportHandle
  {
    get
    {
      return native_get_typesupport();
    }
  }

  // Handle. Create on first use. Can be set for nested classes. TODO -- access...
  public IntPtr Handle
  {
    get
    {
      if (_handle == IntPtr.Zero)
        _handle = native_create_native_message();
      return _handle;
    }
  }

  public SpawnObject_Response()
  {
  }

  public void ReadNativeMessage()
  {
    ReadNativeMessage(Handle);
  }

  public void ReadNativeMessage(IntPtr handle)
  {
    if (handle == IntPtr.Zero)
      throw new System.InvalidOperationException("Invalid handle for reading");
    Success = native_read_field_success(handle);
  }

  public void WriteNativeMessage()
  {
    if (_handle == IntPtr.Zero)
    { // message object reused for subsequent publishing.
      // This could be problematic if sequences sizes changed, but me handle that by checking for it in the c implementation
      _handle = native_create_native_message();
    }

    WriteNativeMessage(Handle);
  }

  // Write from CS to native handle
  public void WriteNativeMessage(IntPtr handle)
  {
    if (handle == IntPtr.Zero)
      throw new System.InvalidOperationException("Invalid handle for writing");
    native_write_field_success(handle, Success);
  }

  public void Dispose()
  {
    if (!disposed)
    {
      if (_handle != IntPtr.Zero)
      {
        native_destroy_native_message(_handle);
        _handle = IntPtr.Zero;
        disposed = true;
      }
    }
  }

  ~SpawnObject_Response()
  {
    Dispose();
  }

};  // class SpawnObject_Response
}  // namespace msg
}  // namespace customservices



