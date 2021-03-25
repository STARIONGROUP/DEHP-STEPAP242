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


#include "pch.h"
#include "Tools.h"

using namespace System::Runtime::InteropServices;

using namespace STEP3DAdapter;


std::string Tools::toStdString(String^ s)
{
    IntPtr p = Marshal::StringToHGlobalAnsi(s);
    const char* message = static_cast<char*>(p.ToPointer());
    std::string msg = message;
    Marshal::FreeHGlobal(p);
    return msg;
}

String^ STEP3DAdapter::Tools::toString(const std::string& s)
{
    return gcnew String(s.c_str());
}

String^ STEP3DAdapter::Tools::toUnquotedString(const std::string& s)
{
    return toUnquotedString(toString(s));
}

String^ Tools::toUnquotedString(String^ s)
{
    return s->Replace("'", "");
}

String^ STEP3DAdapter::Tools::toUnparenthesisString(const std::string& s)
{
    return toUnparenthesisString(toString(s));
}

String^ Tools::toUnparenthesisString(String^ s)
{
    return s->Replace("(", "")->Replace(")", "");
}

String^ STEP3DAdapter::Tools::toCleanString(const std::string& s)
{
    return toUnquotedString(toUnparenthesisString(s));
}

String^ STEP3DAdapter::Tools::toCleanString(String^ s)
{
    return toUnquotedString(toUnparenthesisString(s));
}
