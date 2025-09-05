#pragma once
#include <string>
#include <vector>
#include <optional>

#include "utils/PathUtils.hpp"

class MethodInfo;
class CursorLocation;
class ClassInfo {
public:
	std::string USR;
	std::string Name;

	CursorLocation* Location;

	std::vector<ClassInfo*> BaseClasses;
	std::vector<std::string> BaseClassUSRs;

	std::vector<MethodInfo*> Methods;
	std::vector<std::string> MethodUSRs;

	std::optional<std::string> RawComment;

	int NextVirtualIndex = 0;
	bool IsDefinition = false;

	std::vector<std::pair<std::string, uintptr_t>> AnnotationVirtualTables;

	int GetNextVirtualIndex();
	bool HasNoBases() const;
	bool OwnsAtLeastOneVirtualMethod();
	bool HasAtLeastOneVirtualMethod();
	bool DoesMultiInheritance();
	std::vector<ClassInfo*> GetRootBases();
};