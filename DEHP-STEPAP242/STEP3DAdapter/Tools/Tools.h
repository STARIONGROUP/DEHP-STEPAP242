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

#pragma once

using namespace System;

#include <string>


namespace STEP3DAdapter
{
    /// <summary>
    /// Provides static methods for conversion between managed/unmanaged code
    /// and other kind of string helpers.
    /// </summary>
    class Tools
    {
    public:

        /// <summary>
        /// Converts .NET String to std::string
        /// </summary>
        /// <param name="s">Managed string</param>
        /// <returns>Instance of standard string</returns>
        /// The .NET String uses a Unicode representation which is not 
        /// compatible with the Multibyte in the standard string class.
        static std::string toStdString(String^ s);

        /// <summary>
        /// Converts std::string into a .NET String
        /// </summary>
        /// <param name="s">Unmanaged string</param>
        /// <returns>Instance of .NET string</returns>
        static String^ toString(const std::string& s);

        /// <summary>
        /// Removes single quotes.
        /// </summary>
        /// <param name="s">Unmanaged string</param>
        /// <returns>New instance of .NET string</returns>
        static String^ toUnquotedString(const std::string& s);

        /// <summary>
        /// Removes single quotes.
        /// </summary>
        /// <param name="s">Managed string</param>
        /// <returns>New instance of .NET string</returns>
        static String^ toUnquotedString(String^ s);

        /// <summary>
        /// Removes parenthesis.
        /// </summary>
        /// <param name="s">Unmanaged string</param>
        /// <returns>New instance of .NET string</returns>
        static String^ toUnparenthesisString(const std::string& s);
        
        /// <summary>
        /// Removes parenthesis.
        /// </summary>
        /// <param name="s">Managed string</param>
        /// <returns>New instance of .NET string</returns>
        static String^ toUnparenthesisString(String^ s);

        /// <summary>
        /// Removes single quotes and parenthesis.
        /// </summary>
        /// <param name="s">Unmanaged string</param>
        /// <returns>New instance of .NET string</returns>
        static String^ toCleanString(const std::string& s);
        
        /// <summary>
        /// Removes single quotes and parenthesis.
        /// </summary>
        /// <param name="s">Managed string</param>
        /// <returns>New instance of .NET string</returns>
        static String^ toCleanString(String^ s);
    };

}
