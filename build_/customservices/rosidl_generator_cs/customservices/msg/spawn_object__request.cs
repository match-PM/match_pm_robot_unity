// generated from rosidl_generator_cs/resource/idl.cs.em
// with input from customservices:msg/SpawnObject_Request.idl
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
public class SpawnObject_Request : MessageInternals, Message
{
  private IntPtr _handle;
  private static readonly DllLoadUtils dllLoadUtils;

  public bool IsDisposed { get { return disposed; } }
  private bool disposed;

  // constant declarations

  // members
  public System.String Obj_name { get; set; }
  public System.String Parent_frame { get; set; }
  public float[] Translation { get; private set; }
  public float[] Rotation { get; private set; }
  public System.String Cad_data { get; set; }

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
  private delegate IntPtr NativeReadFieldObj_nameType(IntPtr messageHandle);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void NativeWriteFieldObj_nameType(
    IntPtr messageHandle, [MarshalAs (UnmanagedType.LPStr)] string value);


  private static NativeReadFieldObj_nameType native_read_field_obj_name = null;
  private static NativeWriteFieldObj_nameType native_write_field_obj_name = null;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate IntPtr NativeReadFieldParent_frameType(IntPtr messageHandle);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void NativeWriteFieldParent_frameType(
    IntPtr messageHandle, [MarshalAs (UnmanagedType.LPStr)] string value);


  private static NativeReadFieldParent_frameType native_read_field_parent_frame = null;
  private static NativeWriteFieldParent_frameType native_write_field_parent_frame = null;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  internal delegate IntPtr NativeReadFieldTranslationType(
    out int array_size,
    IntPtr messageHandle);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  internal delegate bool NativeWriteFieldTranslationType(
      [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4, SizeParamIndex = 1)]
      float[] values,
      int array_size,
      IntPtr messageHandle);

  private static NativeReadFieldTranslationType native_read_field_translation = null;
  private static NativeWriteFieldTranslationType native_write_field_translation = null;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  internal delegate IntPtr NativeReadFieldRotationType(
    out int array_size,
    IntPtr messageHandle);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  internal delegate bool NativeWriteFieldRotationType(
      [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.R4, SizeParamIndex = 1)]
      float[] values,
      int array_size,
      IntPtr messageHandle);

  private static NativeReadFieldRotationType native_read_field_rotation = null;
  private static NativeWriteFieldRotationType native_write_field_rotation = null;
  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate IntPtr NativeReadFieldCad_dataType(IntPtr messageHandle);

  [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
  private delegate void NativeWriteFieldCad_dataType(
    IntPtr messageHandle, [MarshalAs (UnmanagedType.LPStr)] string value);


  private static NativeReadFieldCad_dataType native_read_field_cad_data = null;
  private static NativeWriteFieldCad_dataType native_write_field_cad_data = null;

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

  static SpawnObject_Request()
  {
    dllLoadUtils = DllLoadUtilsFactory.GetDllLoadUtils();
    IntPtr messageLibraryTypesupport = dllLoadUtils.LoadLibraryNoSuffix("customservices__rosidl_typesupport_c");
    IntPtr messageLibraryGenerator = dllLoadUtils.LoadLibraryNoSuffix("customservices__rosidl_generator_c");
    IntPtr messageLibraryIntro = dllLoadUtils.LoadLibraryNoSuffix("customservices__rosidl_typesupport_introspection_c");
    MessageTypeSupportPreload();

    IntPtr nativelibrary = dllLoadUtils.LoadLibrary("customservices_spawn_object__request__rosidl_typesupport_c");
    IntPtr native_get_typesupport_ptr = dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_get_type_support");
    SpawnObject_Request.native_get_typesupport = (NativeGetTypeSupportType)Marshal.GetDelegateForFunctionPointer(
      native_get_typesupport_ptr, typeof(NativeGetTypeSupportType));

    IntPtr native_create_native_message_ptr = dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_create_native_message");
    SpawnObject_Request.native_create_native_message = (NativeCreateNativeMessageType)Marshal.GetDelegateForFunctionPointer(
      native_create_native_message_ptr, typeof(NativeCreateNativeMessageType));

    IntPtr native_destroy_native_message_ptr = dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_destroy_native_message");
    SpawnObject_Request.native_destroy_native_message = (NativeDestroyNativeMessageType)Marshal.GetDelegateForFunctionPointer(
      native_destroy_native_message_ptr, typeof(NativeDestroyNativeMessageType));

    IntPtr native_read_field_obj_name_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_read_field_obj_name");
    SpawnObject_Request.native_read_field_obj_name =
      (NativeReadFieldObj_nameType)Marshal.GetDelegateForFunctionPointer(
      native_read_field_obj_name_ptr, typeof(NativeReadFieldObj_nameType));

    IntPtr native_write_field_obj_name_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_write_field_obj_name");
    SpawnObject_Request.native_write_field_obj_name =
      (NativeWriteFieldObj_nameType)Marshal.GetDelegateForFunctionPointer(
      native_write_field_obj_name_ptr, typeof(NativeWriteFieldObj_nameType));
    IntPtr native_read_field_parent_frame_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_read_field_parent_frame");
    SpawnObject_Request.native_read_field_parent_frame =
      (NativeReadFieldParent_frameType)Marshal.GetDelegateForFunctionPointer(
      native_read_field_parent_frame_ptr, typeof(NativeReadFieldParent_frameType));

    IntPtr native_write_field_parent_frame_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_write_field_parent_frame");
    SpawnObject_Request.native_write_field_parent_frame =
      (NativeWriteFieldParent_frameType)Marshal.GetDelegateForFunctionPointer(
      native_write_field_parent_frame_ptr, typeof(NativeWriteFieldParent_frameType));
    IntPtr native_read_field_translation_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_read_field_translation");
    SpawnObject_Request.native_read_field_translation =
      (NativeReadFieldTranslationType)Marshal.GetDelegateForFunctionPointer(
      native_read_field_translation_ptr, typeof(NativeReadFieldTranslationType));

    IntPtr native_write_field_translation_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_write_field_translation");
    SpawnObject_Request.native_write_field_translation =
      (NativeWriteFieldTranslationType)Marshal.GetDelegateForFunctionPointer(
      native_write_field_translation_ptr, typeof(NativeWriteFieldTranslationType));
    IntPtr native_read_field_rotation_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_read_field_rotation");
    SpawnObject_Request.native_read_field_rotation =
      (NativeReadFieldRotationType)Marshal.GetDelegateForFunctionPointer(
      native_read_field_rotation_ptr, typeof(NativeReadFieldRotationType));

    IntPtr native_write_field_rotation_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_write_field_rotation");
    SpawnObject_Request.native_write_field_rotation =
      (NativeWriteFieldRotationType)Marshal.GetDelegateForFunctionPointer(
      native_write_field_rotation_ptr, typeof(NativeWriteFieldRotationType));
    IntPtr native_read_field_cad_data_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_read_field_cad_data");
    SpawnObject_Request.native_read_field_cad_data =
      (NativeReadFieldCad_dataType)Marshal.GetDelegateForFunctionPointer(
      native_read_field_cad_data_ptr, typeof(NativeReadFieldCad_dataType));

    IntPtr native_write_field_cad_data_ptr =
      dllLoadUtils.GetProcAddress(nativelibrary, "customservices__msg__SpawnObject_Request_native_write_field_cad_data");
    SpawnObject_Request.native_write_field_cad_data =
      (NativeWriteFieldCad_dataType)Marshal.GetDelegateForFunctionPointer(
      native_write_field_cad_data_ptr, typeof(NativeWriteFieldCad_dataType));
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

  public SpawnObject_Request()
  {
    Obj_name = "";
    Parent_frame = "";
    Translation = new float[3];
    Rotation = new float[4];
    Cad_data = "";
  }

  public void ReadNativeMessage()
  {
    ReadNativeMessage(Handle);
  }

  public void ReadNativeMessage(IntPtr handle)
  {
    if (handle == IntPtr.Zero)
      throw new System.InvalidOperationException("Invalid handle for reading");
    {
      IntPtr pStr = native_read_field_obj_name(handle);
      Obj_name = Marshal.PtrToStringAnsi(pStr);
    }
    {
      IntPtr pStr = native_read_field_parent_frame(handle);
      Parent_frame = Marshal.PtrToStringAnsi(pStr);
    }
    { //TODO - (adam) this is a bit clunky. Is there a better way to marshal unsigned and bool types?
      int arraySize = 0;
      IntPtr pArr = native_read_field_translation(out arraySize, handle);
      Translation = new float[arraySize];
      float[] __Translation = new float[arraySize];

      if (arraySize != 0)
      {
        int start = 0;
        Marshal.Copy(pArr, __Translation, start, arraySize);
      }
      for (int i = 0; i < arraySize; ++i)
      {
        Translation[i] = (float)(__Translation[i]);
      }
    }
    { //TODO - (adam) this is a bit clunky. Is there a better way to marshal unsigned and bool types?
      int arraySize = 0;
      IntPtr pArr = native_read_field_rotation(out arraySize, handle);
      Rotation = new float[arraySize];
      float[] __Rotation = new float[arraySize];

      if (arraySize != 0)
      {
        int start = 0;
        Marshal.Copy(pArr, __Rotation, start, arraySize);
      }
      for (int i = 0; i < arraySize; ++i)
      {
        Rotation[i] = (float)(__Rotation[i]);
      }
    }
    {
      IntPtr pStr = native_read_field_cad_data(handle);
      Cad_data = Marshal.PtrToStringAnsi(pStr);
    }
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
    native_write_field_obj_name(handle, Obj_name);
    native_write_field_parent_frame(handle, Parent_frame);
    {
            bool success = native_write_field_translation(Translation, Translation.Length, handle);
      
      if (!success)
        throw new System.InvalidOperationException("Error writing field for translation");
    }
    {
            bool success = native_write_field_rotation(Rotation, Rotation.Length, handle);
      
      if (!success)
        throw new System.InvalidOperationException("Error writing field for rotation");
    }
    native_write_field_cad_data(handle, Cad_data);
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

  ~SpawnObject_Request()
  {
    Dispose();
  }

};  // class SpawnObject_Request
}  // namespace msg
}  // namespace customservices



