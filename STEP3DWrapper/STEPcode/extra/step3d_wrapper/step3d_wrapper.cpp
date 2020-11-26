/**
* Implement Public Interface of the STEP-3D Wrapper library
* 
* Linked to Stepcode shared libraries.
*/


#include "step3d_wrapper.h"

#include "sc_version_string.h"


char* getStepcodeVersion()
{
    return sc_version;
}


// Implementations
#include "Step3D_Wrapper_Imp.h";
#include "TreeGraphGenerator_Imp.h"


IStep3D_Wrapper* CreateIStep3D_Wrapper()
{
    return new Step3D_Wrapper_Imp();
}

ITreeGraphGenerator_Wrapper* CreateITreeGraphGenerator_Wrapper()
{
    return new TreeGraphGenerator_Wrapper_Imp();
}
