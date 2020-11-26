# DEHP STEP-3D DST adaptor
# Create Shared DLL for a simplified interface to STEP 3D files

set(step3d_wrapper_test_SRCS
  main.cpp
  )

set(step3d_wrapper_test_HDRS
  )

include_directories(
  ${CMAKE_CURRENT_SOURCE_DIR}
  ${CMAKE_CURRENT_SOURCE_DIR}/../step3d_wrapper
  )

add_executable(step3d_wrapper_test ${step3d_wrapper_test_SRCS} ${step3d_wrapper_test_HDRS})
target_link_libraries(step3d_wrapper_test PRIVATE step3d_wrapper)

# Local Variables:
# tab-width: 8
# mode: cmake
# indent-tabs-mode: t
# End:
# ex: shiftwidth=2 tabstop=8

