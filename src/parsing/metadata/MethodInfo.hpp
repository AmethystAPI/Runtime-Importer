#pragma once
#include <string>
#include <optional>

#include "utils/PathUtils.hpp"

class ClassInfo;
class CursorLocation;
class MethodInfo {
public:
	std::string USR;
	std::string MangledName;
	std::string ShortName;

	ClassInfo* DeclaringClass = nullptr;
	CursorLocation* Location;

	std::optional<std::string> RawComment;

	bool IsDestructor = false;
	bool IsVirtual = false;
	bool HasBody = false;
	bool IsExternal = false;
	bool IsImported = false;
	int VirtualIndex = -1;
	std::optional<std::string> VirtualTableTarget = std::nullopt;

	MethodInfo* OverrideOf = nullptr;
	std::optional<std::string> OverrideOfUSR = std::nullopt;

	std::optional<std::pair<std::string, int>> AnnotationVirtualIndex = std::nullopt;
	std::optional<uintptr_t> AnnotationAddress = std::nullopt;
	std::optional<std::string> AnnotationSignature = std::nullopt;

	bool IsIndexResolved() const;
	MethodInfo* GetRootBase();
};