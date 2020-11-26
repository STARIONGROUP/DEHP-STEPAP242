# DEHP STEP-3D DST adaptor
# Create Shared DLL for a simplified interface to STEP 3D files

set(step3d_wrapper_app_SRCS
  main.cpp
  )

set(step3d_wrapper_app_HDRS
  )

include_directories(
  ${CMAKE_CURRENT_SOURCE_DIR}
  ${CMAKE_CURRENT_SOURCE_DIR}/../step3d_wrapper
  )

add_executable(step3d_wrapper_app ${step3d_wrapper_app_SRCS} ${step3d_wrapper_app_HDRS})
target_link_libraries(step3d_wrapper_app PRIVATE step3d_wrapper)




include_directories("C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/VC/Auxiliary/VS/UnitTest/include")
link_directories(   "C:/Program Files (x86)/Microsoft Visual Studio/2019/Professional/VC/Auxiliary/VS/UnitTest/lib/x64/Microsoft.VisualStudio.TestTools.CppUnitTestFramework.lib")
add_library(step3d_wrapper_tests SHARED unittests.cpp)
target_link_libraries(step3d_wrapper_tests PRIVATE step3d_wrapper)
target_compile_options(step3d_wrapper_tests PRIVATE "/std:c++17")

# Local Variables:
# tab-width: 8
# mode: cmake
# indent-tabs-mode: t
# End:
# ex: shiftwidth=2 tabstop=8

