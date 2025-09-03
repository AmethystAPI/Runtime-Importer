#include "parsing/HeaderParser.hpp"
#include "PathUtils.hpp"
#include <stdexcept>
#include <iostream>

HeaderParser::HeaderParser(const std::string& file, const std::vector<const char*>& arguments, unsigned int flags, const ParsingData& data, const std::vector<fs::path>& pathFilters) :
	mPathFilters(pathFilters),
	mParsingData(data)
{
	for (auto& filter : mPathFilters) {
		filter = (mParsingData.mInputDirectory / filter).generic_string();
	}
	mIndex = clang_createIndex(0, 0);
	mTranslationUnit = clang_parseTranslationUnit(
		mIndex,
		file.c_str(),
		arguments.data(),
		static_cast<int>(arguments.size()),
		nullptr,
		0,
		flags
	);

	if (!mTranslationUnit) {
		clang_disposeIndex(mIndex);
		throw std::runtime_error("Failed to parse translation unit: " + file);
	}
}

HeaderParser::~HeaderParser()
{
	clang_disposeTranslationUnit(mTranslationUnit);
	clang_disposeIndex(mIndex);
}

void HeaderParser::VisitAll()
{
	CXCursor rootCursor = clang_getTranslationUnitCursor(mTranslationUnit);
	clang_visitChildren(
		rootCursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			auto cursorHash = clang_hashCursor(c);
			HeaderParser& parser = *reinterpret_cast<HeaderParser*>(client_data);
			CXCursorKind kind = clang_getCursorKind(c);
			auto filePath = parser.GetFilePathForCursor(c);

			// Skip cursors that are not in a file or not on the filter
			if (!filePath.has_value() || !parser.IsOnFilter(*filePath))
				return CXChildVisit_Continue;

			// Match classes and structs
			if (kind == CXCursor_ClassDecl || kind == CXCursor_StructDecl) {
				auto className = parser.GetClassFullName(c);
				if (parser.mClasses.find(className) != parser.mClasses.end()) {
					return CXChildVisit_Continue;
				}
				ClassVisitingData data{ .mParser = parser, .mParents = {} };
				return parser.VisitClass(c, className, *filePath, data);
			}
			return CXChildVisit_Recurse;
		},
		this
	);
}

std::string HeaderParser::GetClassName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (mClassNameCache.find(usr) != mClassNameCache.end()) {
		return mClassNameCache[usr];
	}
	auto cxClassName = clang_getCursorSpelling(cursor);
	std::string className = clang_getCString(cxClassName);
	if (className.find("unnamed") != std::string::npos) {
		className = "unnamed_" + usr;
	}
	clang_disposeString(cxClassName);
	mClassNameCache[usr] = className;
	return className;
}

std::string HeaderParser::GetClassFullName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (mClassFullNameCache.find(usr) != mClassFullNameCache.end()) {
		return mClassFullNameCache[usr];
	}
	auto className = GetClassName(cursor);
	auto parent = clang_getCursorSemanticParent(cursor);
	if (clang_Cursor_isNull(parent)) {
		mClassFullNameCache[usr] = className;
		return className;
	}

	std::string fullName;
	if (clang_isTranslationUnit(clang_getCursorKind(parent))) {
		fullName = className;
	}
	else {
		fullName = GetClassFullName(parent) + "::" + className;
	}
	mClassFullNameCache[usr] = fullName;
	return fullName;
}

std::string HeaderParser::GetFunctionMangledName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (mFunctionMangledNameCache.find(usr) != mFunctionMangledNameCache.end()) {
		return mFunctionMangledNameCache[usr];
	}
	else {
		auto mangledName = clang_Cursor_getMangling(cursor);
		std::string mangledNameStr = clang_getCString(mangledName);
		clang_disposeString(mangledName);
		mFunctionMangledNameCache[usr] = mangledNameStr;
		return mangledNameStr;
	}
}

std::optional<fs::path> HeaderParser::GetFilePathForCursor(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (mHeaderPathCache.find(usr) != mHeaderPathCache.end()) {
		return mHeaderPathCache[usr];
	}
	else {
		CXFile cxFile;
		clang_getSpellingLocation(clang_getCursorLocation(cursor), &cxFile, nullptr, nullptr, nullptr);
		if (!cxFile) {
			return std::nullopt;
		}
		auto cxFileName = clang_getFileName(cxFile);
		std::string fileName = clang_getCString(cxFileName);
		clang_disposeString(cxFileName);
		auto filePath = fs::path(fileName).generic_string();
		mHeaderPathCache[usr] = filePath;
		return filePath;
	}
}

bool HeaderParser::IsOnFilter(const fs::path& file)
{
	if (!mPathFilters.empty()) {
		for (const auto& filter : mPathFilters) {
			if (PathUtils::CheapIsFrom(file, filter)) {
				return true;
			}
		}
		return false;
	}
	return true;
}

std::string HeaderParser::GetCursorUSR(CXCursor cursor)
{
	auto usr = clang_getCursorUSR(cursor);
	std::string usrStr = clang_getCString(usr);
	clang_disposeString(usr);
	return usrStr;
}

FunctionInfo* HeaderParser::GetFunctionInfoByMangledName(const std::string& mangledName)
{
	if (mFunctions.find(mangledName) != mFunctions.end()) {
		return &mFunctions[mangledName];
	}
	return nullptr;
}

void HeaderParser::ResolveAllClassBases()
{
	for (auto& [className, classInfo] : mClasses) {
		for (auto& baseName : classInfo.mDirectBases) {
			if (mClasses.find(baseName) != mClasses.end()) {
				classInfo.mDirectBaseInfos.push_back(&mClasses[baseName]);
			}
		}
	}
}

void HeaderParser::ResolveAllClassFunctions()
{
	for (auto& [className, classInfo] : mClasses) {
		for (auto& funcName : classInfo.mAllFunctions) {
			if (mFunctions.find(funcName) != mFunctions.end()) {
				classInfo.mAllFunctionInfos.push_back(&mFunctions[funcName]);
			}
		}
	}
}

CXChildVisitResult HeaderParser::VisitClass(CXCursor cursor, const std::string& className, const fs::path& file, ClassVisitingData& visitingData)
{
	ClassInfo& classInfo = (mClasses[className] = ClassInfo{ .mName = className });
	visitingData.mParents.push_back(&classInfo);
	visitingData.mLastVisited = &classInfo;
	clang_visitChildren(
		cursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			ClassVisitingData& data = *reinterpret_cast<ClassVisitingData*>(client_data);
			CXCursorKind kind = clang_getCursorKind(c);
			if (kind == CXCursor_CXXBaseSpecifier) {
				auto type = clang_getCursorType(c);
				auto typeDecl = clang_getTypeDeclaration(type);
				auto baseName = data.mParser.GetClassFullName(typeDecl);
				auto filePath = data.mParser.GetFilePathForCursor(typeDecl);
				if (data.mParents.size() > 0)
					data.mParents.back()->mDirectBases.push_back(baseName);
				return data.mParser.VisitClass(typeDecl, baseName, *filePath, data);
			}
			else if (kind == CXCursor_CXXMethod) {
				auto mangledName = data.mParser.GetFunctionMangledName(c);
				FunctionInfo* functionInfo;
				if ((functionInfo = data.mParser.GetFunctionInfoByMangledName(mangledName)) == nullptr) {
					functionInfo = &(data.mParser.mFunctions[mangledName] = FunctionInfo{ .mMangledName = mangledName });
				}

				data.mParents.back()->mAllFunctions.push_back(mangledName);
				if (clang_CXXMethod_isVirtual(c)) {
					functionInfo->isVirtual = true;
					data.mLastVisited->isVirtual = true;
					for (auto* parent : data.mParents) {
						parent->isVirtual = true;
					}
				}
				return CXChildVisit_Continue;
			}
			return CXChildVisit_Continue;
		},
		&visitingData
	);

	visitingData.mParents.pop_back();
	return CXChildVisit_Continue;
}
