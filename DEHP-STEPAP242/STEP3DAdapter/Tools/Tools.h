#pragma once

using namespace System;

#include <string>


namespace STEP3DAdapter
{
    /// <summary>
    /// Provides static methods for conversion between managed/unmanaged code
    /// as well other kind of kelpers.
    /// </summary>
    class Tools
    {
    public:

        /// <summary>
        /// Convert .NET String to std::string
        /// </summary>
        /// <param name="s">managed string</param>
        /// <returns>standard string</returns>
        /// The .NET String uses a Unicode representation which is not 
        /// compatible with the Multibyte in the standard string class.
        static std::string toStdString(String^ s);

        static String^ toString(std::string s);

        static String^ toUnquotedString(const std::string& s);

        static String^ toUnparenthesisString(const std::string& s);

        static String^ toCleanString(const std::string& s);

        static String^ removeQuotes(String^ s);
        
        static String^ removeParenthesis(String^ s);

        //static void removeQuotes(String^% s);
        //static void removeParenthesis(String^% s);
    };

}
