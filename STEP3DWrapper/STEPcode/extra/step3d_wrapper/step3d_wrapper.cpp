// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DstController.cs" company="Open Engineering S.A.">
//    Copyright (c) 2020-2021 Open Engineering S.A.
// 
//    Author: Juan Pablo Hernandez Vogt
//
//    This file is part of DEHP STEP-AP242 (STEP 3D CAD) adapter project.
// 
//    The DEHP STEP-AP242 is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 3 of the License, or (at your option) any later version.
// 
//    The DEHP STEP-AP242 is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
// 
//    You should have received a copy of the GNU Lesser General Public License
//    along with this program; if not, write to the Free Software Foundation,
//    Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

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
