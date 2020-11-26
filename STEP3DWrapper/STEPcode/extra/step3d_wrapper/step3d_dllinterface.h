#pragma once


#if defined(step3d_DLL_EXPORTS) // inside DLL
#   define STEP3D_DLLAPI __declspec(dllexport)
#else // outside DLL
#   define STEP3D_DLLAPI __declspec(dllimport)
#endif  // CPPEXAMPLEDLL_EXPORTS
