#include "parsing/FunctionInfo.hpp"

bool FunctionInfo::IsIndexResolved() const
{
	return VirtualIndex != -1;
}

FunctionInfo* FunctionInfo::GetRootBase()
{
	if (!OverrideOf)
		return this;
	return OverrideOf->GetRootBase();
}
