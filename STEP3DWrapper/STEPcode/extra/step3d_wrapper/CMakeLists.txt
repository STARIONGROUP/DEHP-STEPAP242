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

set(BIN_INSTALL_DIR bin)
set(LIB_INSTALL_DIR lib)

message(STATUS "CMAKE_BUILD_TYPE=${CMAKE_BUILD_TYPE}")
message(STATUS "CONFIG=${CONFIG}")
message(STATUS "CMAKE_CONFIGURATION_TYPE=${CMAKE_CONFIGURATION_TYPE}")
message(STATUS "CMAKE_CONFIGURATION_TYPES=${CMAKE_CONFIGURATION_TYPES}")
message(STATUS "CMAKE_BINARY_DIR=${CMAKE_BINARY_DIR}")
message(STATUS "SC_INSTALL_PREFIX=${SC_INSTALL_PREFIX}")
message(STATUS "BIN_INSTALL_DIR=${BIN_INSTALL_DIR}")
message(STATUS "LIB_INSTALL_DIR=${LIB_INSTALL_DIR}")

install(FILES step3d_dllinterface.h step3d_wrapper.h
  DESTINATION ${SC_INSTALL_PREFIX}/step3d/include)
  
install(FILES 
  ${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${LIB_INSTALL_DIR}/step3d_wrapper.lib 
  ${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${LIB_INSTALL_DIR}/step3d_wrapper.exp
  DESTINATION ${SC_INSTALL_PREFIX}/step3d/lib)
  
set(step_deps
	${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${BIN_INSTALL_DIR}/base.dll
	${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${BIN_INSTALL_DIR}/stepeditor.dll
	${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${BIN_INSTALL_DIR}/stepcore.dll
	${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${BIN_INSTALL_DIR}/stepdai.dll
	${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${BIN_INSTALL_DIR}/steputils.dll
	${CMAKE_BINARY_DIR}/${CMAKE_BUILD_TYPE}/${BIN_INSTALL_DIR}/sdai_ap242.dll
  )

set(STEP3D_WRAPPER_DIR "${CMAKE_BINARY_DIR}/../../Step3DWrapperLibrary/${CMAKE_BUILD_TYPE}")
install(FILES ${CMAKE_BINARY_DIR}/${BIN_INSTALL_DIR}/step3d_wrapper.dll ${step_deps}
  DESTINATION ${STEP3D_WRAPPER_DIR}/bin)

#message(STATUS "step_deps=${step_deps}")


#message(STATUS "STEP3D_WRAPPER_DIR=${STEP3D_WRAPPER_DIR}")

add_custom_target(COPY_STEP3D 
	COMMAND ${CMAKE_COMMAND} -E make_directory "${STEP3D_WRAPPER_DIR}/include"
	COMMAND ${CMAKE_COMMAND} -E make_directory "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E make_directory "${STEP3D_WRAPPER_DIR}/lib"
	
  
	# Wrapper
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_CURRENT_SOURCE_DIR}/step3d_dllinterface.h" "${STEP3D_WRAPPER_DIR}/include"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_CURRENT_SOURCE_DIR}/step3d_wrapper.h" "${STEP3D_WRAPPER_DIR}/include"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/step3d_wrapper.dll" "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/lib/step3d_wrapper.lib" "${STEP3D_WRAPPER_DIR}/lib"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/lib/step3d_wrapper.exp" "${STEP3D_WRAPPER_DIR}/lib"
	
	# STEPcode runtime dependencies
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/base.dll" "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/stepeditor.dll" "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/stepcore.dll" "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/stepdai.dll" "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/steputils.dll" "${STEP3D_WRAPPER_DIR}/bin"
	COMMAND ${CMAKE_COMMAND} -E copy_if_different "${CMAKE_BINARY_DIR}/$<CONFIGURATION>/bin/sdai_ap242.dll" "${STEP3D_WRAPPER_DIR}/bin"
 
	COMMENT "Copying step3d_wrapper library files"
  COMMENT "${STEP3D_WRAPPER_DIR}/lib"
)


# Local Variables:
# tab-width: 8
# mode: cmake
# indent-tabs-mode: t
# End:
# ex: shiftwidth=2 tabstop=8

