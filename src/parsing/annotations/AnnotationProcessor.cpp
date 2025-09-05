#include "AnnotationProcessor.hpp"
#include <iostream>
#include <regex>

#include "parsing/metadata/ClassInfo.hpp"
#include "parsing/metadata/MethodInfo.hpp"
#include "parsing/HeaderParser.hpp"

bool AnnotationProcessor::IsTagValid(const std::string& tag)
{
    if (tag == TAG_VIRTUAL_INDEX ||
        tag == TAG_VIRTUAL_TABLE ||
        tag == TAG_ADDRESS ||
        tag == TAG_SIGNATURE)
		return true;
    return false;
}

bool AnnotationProcessor::IsClassTag(const std::string& tag)
{
    if (tag == TAG_VIRTUAL_TABLE)
		return true;
    return false;
}

bool AnnotationProcessor::IsMethodTag(const std::string& tag)
{
    if (tag == TAG_VIRTUAL_INDEX ||
        tag == TAG_ADDRESS ||
		tag == TAG_SIGNATURE)
		return true;
    return false;
}

bool AnnotationProcessor::Apply(ClassInfo* classInfo, const Annotation& annotation)
{
	std::string location = classInfo->Location ? classInfo->Location->FilePath.string() : "unknown";
	uint32_t line = classInfo->Location ? classInfo->Location->Line : 0;
	uint32_t column = classInfo->Location ? classInfo->Location->Column : 0;

    if (!IsTagValid(annotation.Tag))
		return false;
    if (!IsClassTag(annotation.Tag)) {
		std::cout << std::format("[Warning] Annotation @{} is not applicable to classes. at:\n  {}:{}:{}", annotation.Tag, location, line, column) << std::endl;
        return false;
    }
    if (annotation.Tag == TAG_VIRTUAL_TABLE) {
        std::regex re(R"(^\s*([^,]+?)\s*,\s*([^,]+?)\s*$)");
		std::smatch match;
        if (std::regex_match(annotation.Value, match, re)) {
            std::string addressStr = match[1].str();
            std::string tableName = match[2].str();
			std::string fullTableName = classInfo->Name + "_vtable_for_" + tableName;

            if (std::find_if(classInfo->AnnotationVirtualTables.begin(), classInfo->AnnotationVirtualTables.end(),
                [&](const auto& pair) {
                    return pair.first == fullTableName;
                }) != classInfo->AnnotationVirtualTables.end())
            {
                std::cout << std::format("[Warning] Duplicate of @{} annotation in class with the same table name. at\n  {}:{}:{}", TAG_VIRTUAL_TABLE, location, line, column) << std::endl;
                return false;
            }

            uintptr_t address = 0;
            try {
                address = std::stoull(addressStr, nullptr, 16);
            } catch (const std::exception& e) {
                std::cout << std::format("[Error] Invalid address in @{} annotation: {}. at\n  {}:{}:{}", TAG_VIRTUAL_TABLE, e.what(), location, line, column) << std::endl;
                return false;
            }
            classInfo->AnnotationVirtualTables.emplace_back(fullTableName, address);
            return true;
        } else {
            std::cout << std::format("[Error] Invalid format for @{} annotation. Expected format: @{} {{0xADDRESS, TableName}}. at\n  {}:{}:{}", TAG_VIRTUAL_TABLE, TAG_VIRTUAL_TABLE, location, line, column) << std::endl;
            return false;
		}
    }
    return false;
}

bool AnnotationProcessor::Apply(MethodInfo* methodInfo, const Annotation& annotation)
{
    std::string location = methodInfo->Location ? methodInfo->Location->FilePath.string() : "unknown";
    uint32_t line = methodInfo->Location ? methodInfo->Location->Line : 0;
    uint32_t column = methodInfo->Location ? methodInfo->Location->Column : 0;

    if (!IsTagValid(annotation.Tag))
        return false;
    if (!IsMethodTag(annotation.Tag)) {
        std::cout << std::format("[Warning] Annotation @{} is not applicable to methods. at:\n  {}:{}:{}", annotation.Tag, location, line, column) << std::endl;
		return false;
    }
    if (annotation.Tag == TAG_VIRTUAL_INDEX) {
        if (!methodInfo->DeclaringClass) {
            std::cout << std::format("[Error] Method with @{} annotation is not a class member. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
            return false;
        }
        if (methodInfo->IsDestructor) {
            std::cout << std::format("[Error] Method with @{} annotation is a destructor. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
            return false;
        }
        if (!methodInfo->IsVirtual) {
            std::cout << std::format("[Error] Method with @{} annotation is not virtual. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
            return false;
        }
        if (methodInfo->AnnotationVirtualIndex.has_value()) {
            std::cout << std::format("[Warning] Method already has a annotation virtual index assigned. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
            return false;
        }

        std::regex re(R"(^\s*([0-9]+)\s*,\s*([^,]+)\s*$)");
		std::smatch match;
        if (std::regex_match(annotation.Value, match, re)) {
            std::string indexStr = match[1].str();
            std::string tableName = match[2].str();
            std::string fullTableName = methodInfo->DeclaringClass->Name + "_vtable_for_" + tableName;
            if (methodInfo->AnnotationVirtualIndex.has_value()) {
                std::cout << std::format("[Warning] Duplicate of @{} annotation in method. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
                return false;
            }
            int index = -1;
            try {
                index = std::stoi(indexStr);
            } catch (const std::exception& e) {
                std::cout << std::format("[Error] Invalid virtual index in @{} annotation: {}. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, e.what(), location, line, column) << std::endl;
                return false;
            }
            if (index < 0) {
                std::cout << std::format("[Error] Negative virtual index in @{} annotation. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
                return false;
            }
            methodInfo->AnnotationVirtualIndex = { fullTableName, index };
            return true;
        } else {
            std::cout << std::format("[Error] Invalid format for @{} annotation. Expected format: @{} {{Index, TableName}}. at\n  {}:{}:{}", TAG_VIRTUAL_INDEX, TAG_VIRTUAL_INDEX, location, line, column) << std::endl;
            return false;
		}
    }
    return false;
}
