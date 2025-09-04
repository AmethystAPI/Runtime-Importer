#pragma once
#include <string>
#include <vector>
#include <optional>
#include "PathUtils.hpp"

class FunctionInfo;
class VirtualPointerComment;
class ClassInfo {
public:
	fs::path DefinedIn;

	std::string Name;

	std::vector<ClassInfo*> BaseClasses;
	std::vector<std::string> BaseClassNames;

	std::vector<FunctionInfo*> Functions;
	std::vector<std::string> FunctionNames;

	std::optional<std::string> Comment;
	std::vector<VirtualPointerComment> VirtualPointerComments;

	int NextVirtualIndex = 0;
	bool IsDefinition = false;

	int GetNextVirtualIndex();
	bool HasNoBases() const;
	bool OwnsAtLeastOneVirtualFunction();
	bool HasAtLeastOneVirtualFunction();
	bool DoesMultiInheritance();
	std::vector<ClassInfo*> GetRootBases();
};