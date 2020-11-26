# DEHP STEP-3D DST adaptor
# Create Shared DLL for a simplified interface to STEP 3D files

set(step3d_SRCS
  step3d_wrapper.cpp
  Step3D_Wrapper_Imp.cpp
  TreeGraphGenerator_Imp.cpp
  )

set(step3d_HDRS
  step3d_dllinterface.h
  step3d_wrapper.h
  Step3D_Wrapper_Imp.h
  TreeGraphGenerator_Imp.h
  )

set(SCHEMA_LINK_NAME sdai_ap242)

include_directories(
  ${CMAKE_CURRENT_SOURCE_DIR}
  ${SC_SOURCE_DIR}/src/cleditor
  ${SC_SOURCE_DIR}/src/cldai
  ${SC_SOURCE_DIR}/src/clstepcore
  ${SC_SOURCE_DIR}/src/clutils
  ${SC_SOURCE_DIR}/src/base
  ${CMAKE_BINARY_DIR}/include
  ${CMAKE_BINARY_DIR}/schemas/${SCHEMA_LINK_NAME}
  )

set(_libdeps stepcore stepdai steputils base stepeditor ${SCHEMA_LINK_NAME})


add_library(step3d_wrapper SHARED ${step3d_SRCS} ${step3d_HDRS})
target_link_libraries(step3d_wrapper PRIVATE ${_libdeps})
target_compile_definitions(step3d_wrapper PRIVATE step3d_DLL_EXPORTS)




# Install files into specific location to make easier the configuration 
# of project using this interface

#message(STATUS "CMAKE_BINARY_DIR=${CMAKE_BINARY_DIR}")
#message(STATUS "BIN_INSTALL_DIR=${BIN_INSTALL_DIR}")
#message(STATUS "LIB_INSTALL_DIR=${LIB_INSTALL_DIR}")

install(FILES step3d_dllinterface.h step3d_wrapper.h
  DESTINATION ${SC_INSTALL_PREFIX}/step3d/include)
  
install(FILES ${CMAKE_BINARY_DIR}/${LIB_INSTALL_DIR}/step3d_wrapper.lib ${CMAKE_BINARY_DIR}/${LIB_INSTALL_DIR}/step3d_wrapper.exp
  DESTINATION ${SC_INSTALL_PREFIX}/step3d/lib)
  
set(step_deps
	${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/base.dll
	${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/stepeditor.dll
	${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/stepcore.dll
	${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/stepdai.dll
	${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/steputils.dll
	${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/sdai_ap242.dll
  )

install(FILES ${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/step3d_wrapper.dll ${step_deps}
  DESTINATION ${SC_INSTALL_PREFIX}/step3d/bin)

# Local Variables:
# tab-width: 8
# mode: cmake
# indent-tabs-mode: t
# End:
# ex: shiftwidth=2 tabstop=8

