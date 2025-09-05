#pragma once
#include <string>

#include "parsing/annotations/Annotation.hpp"

class ClassInfo;
class MethodInfo;
class AnnotationProcessor {
public:
	constexpr static const char* TAG_VIRTUAL_INDEX = "VirtualIndex";
	constexpr static const char* TAG_VIRTUAL_TABLE = "VirtualTable";
	constexpr static const char* TAG_ADDRESS = "Address";
	constexpr static const char* TAG_SIGNATURE = "Signature";

	static bool IsTagValid(const std::string& tag);
	static bool IsClassTag(const std::string& tag);
	static bool IsMethodTag(const std::string& tag);
	static bool Apply(ClassInfo* classInfo, const Annotation& annotation);
	static bool Apply(MethodInfo* classInfo, const Annotation& annotation);
};