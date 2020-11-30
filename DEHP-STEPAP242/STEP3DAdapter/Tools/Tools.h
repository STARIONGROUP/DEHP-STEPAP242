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
