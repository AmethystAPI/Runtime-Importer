#pragma once
#include <string>
#include <optional>
#include "parsing/CommentParser.hpp"

class FunctionInfo {
public:
	std::string MangledName;

	ClassInfo* DeclaringClass = nullptr;

	std::optional<std::string> Comment;
	std::optional<VirtualIndexComment> VirtualIndexComment;

	bool IsDestructor = false;
	bool IsVirtual = false;
	int VirtualIndex = -1;
	std::optional<std::string> VirtualTableTarget = std::nullopt;
	std::optional<std::string> OverrideOfName = std::nullopt;
	FunctionInfo* OverrideOf = nullptr;

	bool IsIndexResolved() const;
	FunctionInfo* GetRootBase();
};