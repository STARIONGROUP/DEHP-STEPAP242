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
