set(CROSS_ROOTFS $ENV{ROOTFS_DIR})

set(TARGET_ARCH_NAME $ENV{TARGET_BUILD_ARCH})
set(CMAKE_SYSTEM_NAME Linux)
set(CMAKE_SYSTEM_VERSION 1)

if(TARGET_ARCH_NAME STREQUAL "armel")
  set(CMAKE_SYSTEM_PROCESSOR armv7l)
  set(TOOLCHAIN "arm-linux-gnueabi")
  if("$ENV{__DistroRid}" MATCHES "tizen.*")
    set(TIZEN_TOOLCHAIN "armv7l-tizen-linux-gnueabi/6.2.1")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "arm")
  set(CMAKE_SYSTEM_PROCESSOR armv7l)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/armv6-alpine-linux-musleabihf)
    set(TOOLCHAIN "armv6-alpine-linux-musleabihf")
  else()
    set(TOOLCHAIN "arm-linux-gnueabihf")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "arm64")
  set(CMAKE_SYSTEM_PROCESSOR aarch64)
  if(EXISTS ${CROSS_ROOTFS}/usr/lib/gcc/aarch64-alpine-linux-musl)
    set(TOOLCHAIN "aarch64-alpine-linux-musl")
  else()
    set(TOOLCHAIN "aarch64-linux-gnu")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "x86")
  set(CMAKE_SYSTEM_PROCESSOR i686)
  set(TOOLCHAIN "i686-linux-gnu")
else()
  message(FATAL_ERROR "Arch is ${TARGET_ARCH_NAME}. Only armel, arm, arm64 and x86 are supported!")
endif()

if(DEFINED ENV{TOOLCHAIN})
  set(TOOLCHAIN $ENV{TOOLCHAIN})
endif()

# Specify include paths
if(TARGET_ARCH_NAME STREQUAL "armel")
  if(DEFINED TIZEN_TOOLCHAIN)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/)
    include_directories(SYSTEM ${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}/include/c++/armv7l-tizen-linux-gnueabi)
  endif()
endif()

set(CMAKE_SYSROOT "${CROSS_ROOTFS}")
set(CMAKE_C_COMPILER_EXTERNAL_TOOLCHAIN "${CROSS_ROOTFS}/usr")
set(CMAKE_CXX_COMPILER_EXTERNAL_TOOLCHAIN "${CROSS_ROOTFS}/usr")
set(CMAKE_ASM_COMPILER_EXTERNAL_TOOLCHAIN "${CROSS_ROOTFS}/usr")

# Specify link flags

if(TARGET_ARCH_NAME STREQUAL "armel")
  if(DEFINED TIZEN_TOOLCHAIN) # For Tizen only
    add_link_options("-B${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
    add_link_options("-L${CROSS_ROOTFS}/lib")
    add_link_options("-L${CROSS_ROOTFS}/usr/lib")
    add_link_options("-L${CROSS_ROOTFS}/usr/lib/gcc/${TIZEN_TOOLCHAIN}")
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "x86")
  add_link_options(-m32)
endif()

# Specify compile options

if(TARGET_ARCH_NAME MATCHES "^(arm|armel|arm64)$")
  set(CMAKE_C_COMPILER_TARGET ${TOOLCHAIN})
  set(CMAKE_CXX_COMPILER_TARGET ${TOOLCHAIN})
  set(CMAKE_ASM_COMPILER_TARGET ${TOOLCHAIN})
endif()

if(TARGET_ARCH_NAME MATCHES "^(arm|armel)$")
  add_compile_options(-mthumb)
  if (NOT DEFINED CLR_ARM_FPU_TYPE)
    set (CLR_ARM_FPU_TYPE vfpv3)
  endif (NOT DEFINED CLR_ARM_FPU_TYPE)

  add_compile_options (-mfpu=${CLR_ARM_FPU_TYPE})
  if (NOT DEFINED CLR_ARM_FPU_CAPABILITY)
    set (CLR_ARM_FPU_CAPABILITY 0x7)
  endif (NOT DEFINED CLR_ARM_FPU_CAPABILITY)

  add_definitions (-DCLR_ARM_FPU_CAPABILITY=${CLR_ARM_FPU_CAPABILITY})

  if(TARGET_ARCH_NAME STREQUAL "armel")
    add_compile_options(-mfloat-abi=softfp)
    if(DEFINED TIZEN_TOOLCHAIN)
      add_compile_options(-Wno-deprecated-declarations) # compile-time option
      add_compile_options(-D__extern_always_inline=inline) # compile-time option
    endif()
  endif()
elseif(TARGET_ARCH_NAME STREQUAL "x86")
  add_compile_options(-m32)
  add_compile_options(-Wno-error=unused-command-line-argument)
endif()

# Set LLDB include and library paths for builds that need lldb.
if(TARGET_ARCH_NAME MATCHES "^(arm|armel|x86)$")
  if(TARGET_ARCH_NAME STREQUAL "x86")
    set(LLVM_CROSS_DIR "$ENV{LLVM_CROSS_HOME}")
  else() # arm/armel case
    set(LLVM_CROSS_DIR "$ENV{LLVM_ARM_HOME}")
  endif()
  if(LLVM_CROSS_DIR)
    set(WITH_LLDB_LIBS "${LLVM_CROSS_DIR}/lib/" CACHE STRING "")
    set(WITH_LLDB_INCLUDES "${LLVM_CROSS_DIR}/include" CACHE STRING "")
    set(LLDB_H "${WITH_LLDB_INCLUDES}" CACHE STRING "")
    set(LLDB "${LLVM_CROSS_DIR}/lib/liblldb.so" CACHE STRING "")
  else()
    if(TARGET_ARCH_NAME STREQUAL "x86")
      set(WITH_LLDB_LIBS "${CROSS_ROOTFS}/usr/lib/i386-linux-gnu" CACHE STRING "")
      set(CHECK_LLVM_DIR "${CROSS_ROOTFS}/usr/lib/llvm-3.8/include")
      if(EXISTS "${CHECK_LLVM_DIR}" AND IS_DIRECTORY "${CHECK_LLVM_DIR}")
        set(WITH_LLDB_INCLUDES "${CHECK_LLVM_DIR}")
      else()
        set(WITH_LLDB_INCLUDES "${CROSS_ROOTFS}/usr/lib/llvm-3.6/include")
      endif()
    else() # arm/armel case
      set(WITH_LLDB_LIBS "${CROSS_ROOTFS}/usr/lib/${TOOLCHAIN}" CACHE STRING "")
      set(WITH_LLDB_INCLUDES "${CROSS_ROOTFS}/usr/lib/llvm-3.6/include" CACHE STRING "")
    endif()
  endif()
endif()


set(CMAKE_FIND_ROOT_PATH_MODE_PROGRAM NEVER)
set(CMAKE_FIND_ROOT_PATH_MODE_LIBRARY ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_INCLUDE ONLY)
set(CMAKE_FIND_ROOT_PATH_MODE_PACKAGE ONLY)
