#pragma once
#include <vector>
#include <string>
#include "clang-c/Index.h"
#include <filesystem>
#include <unordered_map>
#include <unordered_set>
#include <stack>
#include "ClassInfo.hpp"
#include "FunctionInfo.hpp"
namespace fs = std::filesystem;

struct ParsingData {
	fs::path mInputDirectory;
};

class HeaderParser;
struct ClassVisitingData {
	HeaderParser& mParser;
	std::vector<ClassInfo*> mParents;
};

struct FunctionVisitingData {
	HeaderParser& mParser;
	ClassVisitingData& mClassData;
};

class HeaderParser {
public:
	CXIndex mIndex;
	CXTranslationUnit mTranslationUnit;
	std::vector<fs::path> mPathFilters;
	ParsingData mParsingData;

	std::unordered_map<std::string, std::string> mClassNameCache;
	std::unordered_map<std::string, std::string> mClassFullNameCache;
	std::unordered_map<std::string, std::string> mFunctionMangledNameCache;
	std::unordered_map<std::string, std::string> mCommentsCache;
	std::unordered_map<std::string, fs::path> mHeaderPathCache;
	std::unordered_map<std::string, fs::path> mHeaderRelativePathCache;
	std::unordered_map<fs::path, bool> mShouldProcessCache;

	std::unordered_map<std::string, ClassInfo> mClasses;
	std::unordered_map<std::string, FunctionInfo> mFunctions;

	HeaderParser(const std::string& file, const std::vector<const char*>& arguments, unsigned int flags, const ParsingData& data, const std::vector<fs::path>& pathFilters = {});
	~HeaderParser();

	void VisitAll();
	std::string GetClassName(CXCursor cursor);
	std::string GetClassFullName(CXCursor cursor);
	std::string GetFunctionMangledName(CXCursor cursor);
	std::optional<fs::path> GetFilePathForCursor(CXCursor cursor);
	std::optional<std::string> GetCommentForCursor(CXCursor cursor);
	bool IsOnFilter(const fs::path& file);
	std::string GetCursorUSR(CXCursor cursor);
	FunctionInfo* GetFunctionInfoByMangledName(const std::string& mangledName);
	bool HasClass(const std::string& className);
	bool HasFunction(const std::string& mangledName);
	

	void SortAllClassesTopologically();
	void SortAllFunctionsTopologically();
	void ResolveAllClassBases();
	void ResolveAllClassFunctions();
	void ResolveAllFunctionOverrides();
	void ResolveBaseClassVirtualFunctionsIndex();
	void ResolveNewVirtualFunctionIndices();
	void ResolveAllFunctionOverridesIndices();

	void PrintTranslationUnitDiagnostics();

private:
	CXChildVisitResult VisitClass(CXCursor cursor, const std::string& className, const fs::path& file, ClassVisitingData& visitingData);
	CXChildVisitResult VisitFunction(CXCursor cursor, const std::string& mangledName, FunctionVisitingData& visitingData);
};