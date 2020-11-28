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

String^ STEP3DAdapter::Tools::toString(std::string s)
{
    return gcnew String(s.c_str());
}

String^ STEP3DAdapter::Tools::toUnquotedString(const std::string& s)
{
    return removeQuotes(toString(s));
}

String^ STEP3DAdapter::Tools::toUnparenthesisString(const std::string& s)
{
    return removeParenthesis(toString(s));
}

String^ STEP3DAdapter::Tools::toCleanString(const std::string& s)
{
    return removeQuotes(toUnparenthesisString(s));
}

String^ Tools::removeQuotes(String^ s)
{
    return s->Replace("'", "");
}

String^ Tools::removeParenthesis(String^ s)
{
    return s->Replace("(", "")->Replace(")", "");
}

/*
void Tools::removeQuotes(String^% s)
{
    s = s->Replace("'", "");
}

void Tools::removeParenthesis(String^% s)
{
    s = s->Replace("(", "")->Replace(")", "");
}
*/