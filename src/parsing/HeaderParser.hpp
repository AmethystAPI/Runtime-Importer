#pragma once
#include <vector>
#include <string>
#include <filesystem>
#include <unordered_map>
#include <unordered_set>
#include <stack>

#include "clang-c/Index.h"

#include "parsing/metadata/ClassInfo.hpp"
#include "parsing/metadata/MethodInfo.hpp"
#include "parsing/CursorLocation.hpp"

namespace fs = std::filesystem;

class HeaderParser {
public:
	struct ParserContext {
		fs::path InputDirectory;
	};

	struct ClassVisitingData {
		HeaderParser& mParser;
		std::vector<ClassInfo*> mParents;
	};

	struct FunctionVisitingData {
		HeaderParser& mParser;
		ClassVisitingData* mClassData;
	};

	CXIndex Index;
	CXTranslationUnit TranslationUnit;
	std::vector<fs::path> PathFilters;
	ParserContext Context;

	std::unordered_map<std::string, std::string> ClassNameCache;
	std::unordered_map<std::string, std::string> ClassFullNameCache;
	std::unordered_map<std::string, std::string> FunctionMangledNameCache;
	std::unordered_map<std::string, std::string> FunctionShortNameCache;
	std::unordered_map<std::string, std::string> CommentsCache;

	std::unordered_map<std::string, CursorLocation> CursorLocationCache;

	std::unordered_map<fs::path, bool> ShouldProcessCache;

	std::unordered_map<std::string, ClassInfo> Classes;
	std::unordered_map<std::string, MethodInfo> Methods;

	std::unordered_set<std::string> VisitedDefinitions;

	HeaderParser(const std::string& file, const std::vector<const char*>& arguments, unsigned int flags, const ParserContext& ctx, const std::vector<fs::path>& pathFilters = {});
	~HeaderParser();

	void VisitAll();
	std::string GetClassName(CXCursor cursor);
	std::string GetClassFullName(CXCursor cursor);
	std::string GetFunctionMangledName(CXCursor cursor);
	std::string GetFunctionShortName(CXCursor cursor);
	CursorLocation* GetCursorLocation(CXCursor cursor);
	std::optional<std::string> GetCommentForCursor(CXCursor cursor);
	std::string GetCursorUSR(CXCursor cursor);
	MethodInfo* GetMethodInfo(const std::string& usr);
	ClassInfo* GetClassInfo(const std::string& usr);

	bool HasClass(const std::string& usr);
	bool HasFunction(const std::string& usr);
	bool IsOnFilter(const fs::path& file);
	bool WasDefinitionVisited(const std::string& usr);
	bool HasFunctionBody(CXCursor cursor);

	void SortAllClassesTopologically();
	void SortAllMethodsTopologically();
	void ResolveAllClassBases();
	void ResolveAllClassFunctions();
	void ResolveAllFunctionOverrides();
	void ResolveBaseClassVirtualFunctionsIndex();
	void ResolveNewVirtualFunctionIndices();
	void ResolveAllFunctionOverridesIndices();
	void PrintTranslationUnitErrors();
	void DoStuff();

	bool HasAnyErrors();

private:
	std::vector<CXCursor> GetMethodsInClass(CXCursor classCursor);
	std::vector<CXCursor> GetBaseClassesOfClass(CXCursor classCursor);
	CXChildVisitResult VisitClass(CXCursor cursor, ClassVisitingData& visitingData);
	CXChildVisitResult VisitMethod(CXCursor cursor, FunctionVisitingData& visitingData);
};