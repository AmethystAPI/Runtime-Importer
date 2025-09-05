#include "parsing/metadata/MethodInfo.hpp"

bool MethodInfo::IsIndexResolved() const
{
	return VirtualIndex != -1;
}

MethodInfo* MethodInfo::GetRootBase()
{
	if (!OverrideOf)
		return this;
	return OverrideOf->GetRootBase();
}
