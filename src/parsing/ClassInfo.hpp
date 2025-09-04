#pragma once
#include <string>
#include <vector>
#include <optional>

class FunctionInfo;
class VirtualPointerComment;
class ClassInfo {
public:
	std::string Name;

	std::vector<ClassInfo*> BaseClasses;
	std::vector<std::string> BaseClassNames;

	std::vector<FunctionInfo*> Functions;
	std::vector<std::string> FunctionNames;

	std::optional<std::string> Comment;
	std::vector<VirtualPointerComment> VirtualPointerComments;

	int NextVirtualIndex = 0;

	int GetNextVirtualIndex();
	bool HasNoBases() const;
	bool OwnsAtLeastOneVirtualFunction();
	bool HasAtLeastOneVirtualFunction();
	bool DoesMultiInheritance();
	std::vector<ClassInfo*> GetRootBases();
};