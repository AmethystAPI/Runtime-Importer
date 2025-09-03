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
	std::vector<bool> mHasVirtualMethod;
	ClassInfo* mLastVisited = nullptr;
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
	bool IsOnFilter(const fs::path& file);
	std::string GetCursorUSR(CXCursor cursor);
	FunctionInfo* GetFunctionInfoByMangledName(const std::string& mangledName);

	void ResolveAllClassBases();
	void ResolveAllClassFunctions();

private:
	CXChildVisitResult VisitClass(CXCursor cursor, const std::string& className, const fs::path& file, ClassVisitingData& visitingData);
};