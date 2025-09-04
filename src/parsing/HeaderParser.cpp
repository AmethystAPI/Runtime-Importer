#include "parsing/HeaderParser.hpp"
#include "PathUtils.hpp"
#include <stdexcept>
#include <iostream>
#include <functional>

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

std::optional<std::string> HeaderParser::GetCommentForCursor(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (mCommentsCache.find(usr) != mCommentsCache.end())
		return mCommentsCache[usr];
	auto cxComment = clang_Cursor_getRawCommentText(cursor);
	auto csComment = clang_getCString(cxComment);
	if (!csComment)
	{
		clang_disposeString(cxComment);
		return std::nullopt;
	}
	std::string comment = csComment;
	clang_disposeString(cxComment);
	return mCommentsCache[usr] = comment;
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

bool HeaderParser::HasClass(const std::string& className)
{
	return mClasses.find(className) != mClasses.end();
}

bool HeaderParser::HasFunction(const std::string& mangledName)
{
	return mFunctions.find(mangledName) != mFunctions.end();
}

void HeaderParser::SortAllClassesTopologically()
{
	std::unordered_map<std::string, ClassInfo*> classMap;
	for (auto& [name, cls] : mClasses)
		classMap[name] = &cls;

	std::unordered_set<std::string> visited;
	std::stack<ClassInfo*> sortedStack;
	std::function<void(ClassInfo*)> visitClass = [&](ClassInfo* cls) {
		if (!cls || visited.count(cls->Name)) return;
		visited.insert(cls->Name);

		for (const auto& baseName : cls->BaseClassNames) {
			auto it = classMap.find(baseName);
			if (it != classMap.end()) {
				visitClass(it->second);
			}
		}

		sortedStack.push(cls);
	};

	for (auto& [name, cls] : mClasses) {
		visitClass(&cls);
	}

	std::vector<std::pair<std::string, ClassInfo>> sortedClasses;
	while (!sortedStack.empty()) {
		ClassInfo* cls = sortedStack.top();
		sortedStack.pop();
		sortedClasses.emplace_back(cls->Name, *cls);
	}

	std::reverse(sortedClasses.begin(), sortedClasses.end());

	mClasses.clear();
	for (auto& pair : sortedClasses) {
		mClasses[pair.first] = std::move(pair.second);
	}
}

void HeaderParser::SortAllFunctionsTopologically()
{
	std::unordered_map<std::string, FunctionInfo*> funcMap;
	for (auto& [name, func] : mFunctions)
		funcMap[name] = &func;

	std::unordered_set<FunctionInfo*> visited;
	std::vector<FunctionInfo*> sortedFuncs;

	std::function<void(FunctionInfo*)> visitFunc = [&](FunctionInfo* func) {
		if (!func || visited.count(func)) return;
		visited.insert(func);
		if (func->OverrideOfName.has_value()) {
			auto& baseName = func->OverrideOfName.value();
			auto it = funcMap.find(baseName);
			if (it != funcMap.end()) {
				visitFunc(it->second);
			}
		}
		sortedFuncs.push_back(func);
	};

	for (auto& [name, func] : mFunctions) {
		visitFunc(&func);
	}

	std::unordered_map<std::string, FunctionInfo> newFuncs;
	for (auto* func : sortedFuncs) {
		newFuncs[func->MangledName] = std::move(*func);
	}

	mFunctions = std::move(newFuncs);
}


void HeaderParser::ResolveAllClassBases()
{
	for (auto& [className, classInfo] : mClasses) {
		for (auto& baseName : classInfo.BaseClassNames) {
			if (mClasses.find(baseName) != mClasses.end()) {
				classInfo.BaseClasses.push_back(&mClasses[baseName]);
			}
		}
	}
}

void HeaderParser::ResolveAllClassFunctions()
{
	for (auto& [className, classInfo] : mClasses) {
		for (auto& funcName : classInfo.FunctionNames) {
			if (mFunctions.find(funcName) != mFunctions.end()) {
				auto* func = &mFunctions[funcName];
				func->DeclaringClass = &classInfo;
				classInfo.Functions.push_back(func);
			}
		}
	}
}

void HeaderParser::ResolveAllFunctionOverrides()
{
	for (auto& [funcName, funcInfo] : mFunctions) {
		if (funcInfo.OverrideOfName.has_value()) {
			auto& baseName = funcInfo.OverrideOfName.value();
			funcInfo.OverrideOf = GetFunctionInfoByMangledName(baseName);
		}
	}
}

void HeaderParser::ResolveBaseClassVirtualFunctionsIndex()
{
	for (auto& [funcName, func] : mFunctions) {
		auto* declaringClass = func.DeclaringClass;
		if (func.IsIndexResolved() || !declaringClass || !declaringClass->HasNoBases() || !func.IsVirtual)
			continue;
		if (!func.IsDestructor)
			func.VirtualIndex = declaringClass->GetNextVirtualIndex();
		else
			func.VirtualIndex = 0; // Destructors are always at index 0 of "vtable for this"
		func.VirtualTableTarget = declaringClass->Name + "_vtable_for_this";
	}
}
void HeaderParser::ResolveNewVirtualFunctionIndices()
{
	for (auto& [className, classInfo] : mClasses) {
		for (auto* func : classInfo.Functions) {
			auto* declaringClass = &classInfo;
			if (func->IsIndexResolved() || !declaringClass || !func->IsVirtual || func->OverrideOf)
				continue;
			func->VirtualIndex = declaringClass->GetNextVirtualIndex();
			if (!declaringClass->DoesMultiInheritance())
				func->VirtualIndex += declaringClass->GetRootBases()[0]->NextVirtualIndex;
			func->VirtualTableTarget = declaringClass->Name + "_vtable_for_this";
		}
	}
	
}
void HeaderParser::ResolveAllFunctionOverridesIndices()
{
	for (auto& [funcName, func] : mFunctions) {
		auto* declaringClass = func.DeclaringClass;
		if (func.IsIndexResolved() || !declaringClass || declaringClass->HasNoBases() || !func.IsVirtual || !func.OverrideOf)
			continue;
		auto* baseFunc = func.GetRootBase();
		func.VirtualIndex = baseFunc->VirtualIndex;
		if (!declaringClass->DoesMultiInheritance() || func.IsDestructor)
			func.VirtualTableTarget = declaringClass->Name + "_vtable_for_this";
		else 
			func.VirtualTableTarget = declaringClass->Name + "_vtable_for_" + baseFunc->DeclaringClass->Name;
	}
}

void HeaderParser::PrintTranslationUnitDiagnostics()
{
	unsigned numDiagnostics = clang_getNumDiagnostics(mTranslationUnit);
	for (unsigned i = 0; i < numDiagnostics; ++i) {
		CXDiagnostic diag = clang_getDiagnostic(mTranslationUnit, i);

		// Get severity
		CXDiagnosticSeverity severity = clang_getDiagnosticSeverity(diag);

		// Convert severity to string if you like
		const char* severityStr = "";
		switch (severity) {
		case CXDiagnostic_Ignored: severityStr = "Ignored"; break;
		case CXDiagnostic_Note: severityStr = "Note"; break;
		case CXDiagnostic_Warning: severityStr = "Warning"; break;
		case CXDiagnostic_Error: severityStr = "Error"; break;
		case CXDiagnostic_Fatal: severityStr = "Fatal"; break;
		}

		// Get diagnostic string
		CXString diagStr = clang_formatDiagnostic(diag, clang_defaultDiagnosticDisplayOptions());
		printf("[%s] %s\n", severityStr, clang_getCString(diagStr));
		clang_disposeString(diagStr);

		// Dispose the diagnostic object
		clang_disposeDiagnostic(diag);
	}
}

CXChildVisitResult HeaderParser::VisitClass(CXCursor cursor, const std::string& className, const fs::path& file, ClassVisitingData& visitingData)
{
	if (HasClass(className) || !IsOnFilter(file))
		return CXChildVisit_Continue;

	ClassInfo& classInfo = (mClasses[className] = ClassInfo{ .Name = className });
	classInfo.Comment = GetCommentForCursor(cursor);
	visitingData.mParents.push_back(&classInfo);
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
					data.mParents.back()->BaseClassNames.push_back(baseName);
				
				return data.mParser.VisitClass(typeDecl, baseName, *filePath, data);
			}
			else if (kind == CXCursor_CXXMethod || kind == CXCursor_Destructor) {
				auto mangledName = data.mParser.GetFunctionMangledName(c);
				auto filePath = data.mParser.GetFilePathForCursor(c);
				FunctionVisitingData visData{ .mParser = data.mParser, .mClassData = data };
				return data.mParser.VisitFunction(c, mangledName, visData);
			}
			return CXChildVisit_Continue;
		},
		&visitingData
	);

	visitingData.mParents.pop_back();
	return CXChildVisit_Continue;
}

CXChildVisitResult HeaderParser::VisitFunction(CXCursor cursor, const std::string& mangledName, FunctionVisitingData& data)
{
	if (HasFunction(mangledName)) {
		return CXChildVisit_Continue;
	}

	FunctionInfo& functionInfo = (data.mParser.mFunctions[mangledName] = FunctionInfo{ .MangledName = mangledName });
	functionInfo.Comment = data.mParser.GetCommentForCursor(cursor);
	data.mClassData.mParents.back()->FunctionNames.push_back(mangledName);
	if (clang_CXXMethod_isVirtual(cursor)) {
		bool isDestructor = clang_getCursorKind(cursor) == CXCursor_Destructor;
		CXCursor* overridenCursors;
		unsigned int numOverridenCursors;
		clang_getOverriddenCursors(cursor, &overridenCursors, &numOverridenCursors);
		if (numOverridenCursors > 0) {
			auto overridenName = data.mParser.GetFunctionMangledName(overridenCursors[0]);
			functionInfo.OverrideOfName = overridenName;
		}

		functionInfo.IsVirtual = true;
		functionInfo.IsDestructor = isDestructor;
	}
	return CXChildVisit_Continue;
}
