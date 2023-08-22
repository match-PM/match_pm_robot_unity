# Copyright (C) 2020-2021, Robotec.ai
# Copyright (C) 2015-2017, Illumina, inc.
#
# Based on
# https://github.com/Illumina/interop/tree/master/cmake/Modules
#
# Find the xbuild tool and mono interpreter
#
# MONO_FOUND             System has Mono dev files, as well as mono, mcs, gmcs and gacutil if not MONO_ONLY_LIBRARIES_REQUIRED
# Mono_EXECUTABLE        Where to find 'mono'
# XBUILD_EXECUTABLE      Where to find 'xbuild'
# Mono_VERSION           The version number of the Mono interpreter

set(Mono_ROOT "" CACHE PATH "Set the location of the Mono root directory")

if(Mono_ROOT AND EXISTS "${Mono_ROOT}")
    find_program(Mono_EXECUTABLE mono
            PATHS ${Mono_ROOT} ${Mono_ROOT}/lib/mono/1.0
            PATH_SUFFIXES bin
            NO_DEFAULT_PATH)
    find_program(XBUILD_EXECUTABLE xbuild
            PATHS ${Mono_ROOT} ${Mono_ROOT}/lib/mono/1.0
            PATH_SUFFIXES bin
            NO_DEFAULT_PATH)
endif()

find_program(Mono_EXECUTABLE mono)
find_program(XBUILD_EXECUTABLE xbuild)

if(WIN32)
    set(Mono_CLR "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Novell\\Mono;DefaultCLR]")
    get_filename_component(csharp_mono_bin_hints "[HKEY_LOCAL_MACHINE\\SOFTWARE\\Novell\\Mono\\${Mono_CLR};SdkInstallRoot]/bin" ABSOLUTE)
elseif(CMAKE_SYSTEM_NAME STREQUAL "Darwin")
    set(csharp_mono_bin_hints
            "/Library/Frameworks/Mono.framework/Commands"
            "/usr"
            "/usr/local"
            "/usr/lib/mono/2.0"
            "/opt/novell/mono")
else()
    set(csharp_mono_bin_hints
            "/usr/bin/"
            "/usr/local/bin/"
            "/usr/lib/mono/2.0"
            "/opt/novell/mono/bin")
endif()

find_program(Mono_EXECUTABLE mono mono.exe
        HINTS ${csharp_mono_bin_hints}
        PATH_SUFFIXES bin
        NO_DEFAULT_PATH
)

find_program(XBUILD_EXECUTABLE xbuild xbuild.exe
        HINTS ${csharp_mono_bin_hints}
        PATH_SUFFIXES bin
        NO_DEFAULT_PATH
)

if(EXISTS "${Mono_EXECUTABLE}")
    execute_process(
      COMMAND ${Mono_EXECUTABLE} -V
      OUTPUT_VARIABLE csharp_mono_version_string
    )
string(REGEX MATCH "([0-9]*)([.])([0-9]*)([.]*)([0-9]*)" csharp_mono_version_temp "${csharp_mono_version_string}")
    set(Mono_VERSION ${csharp_mono_version_temp} CACHE STRING "C# Mono interpreter version")
endif()


include(FindPackageHandleStandardArgs)
find_package_handle_standard_args(Mono DEFAULT_MSG XBUILD_EXECUTABLE Mono_EXECUTABLE)
mark_as_advanced(Mono_EXECUTABLE XBUILD_EXECUTABLE Mono_VERSION)
