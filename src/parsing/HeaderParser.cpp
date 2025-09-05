#include "parsing/HeaderParser.hpp"

#include <stdexcept>
#include <iostream>
#include <functional>

#include "utils/PathUtils.hpp"

HeaderParser::HeaderParser(const std::string& file, const std::vector<const char*>& arguments, unsigned int flags, const ParserContext& ctx, const std::vector<fs::path>& pathFilters) :
	PathFilters(pathFilters),
	Context(ctx)
{
	Index = clang_createIndex(0, 0);
	TranslationUnit = clang_parseTranslationUnit(
		Index,
		file.c_str(),
		arguments.data(),
		static_cast<int>(arguments.size()),
		nullptr,
		0,
		flags
	);

	if (!TranslationUnit) {
		clang_disposeIndex(Index);
		throw std::runtime_error("Failed to parse translation unit: " + file);
	}
}
HeaderParser::~HeaderParser()
{
	clang_disposeTranslationUnit(TranslationUnit);
	clang_disposeIndex(Index);
}
std::string HeaderParser::GetClassName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (ClassNameCache.find(usr) != ClassNameCache.end()) {
		return ClassNameCache[usr];
	}
	auto cxClassName = clang_getCursorSpelling(cursor);
	std::string className = clang_getCString(cxClassName);
	if (className.find("unnamed") != std::string::npos) {
		className = "unnamed_" + usr;
	}
	clang_disposeString(cxClassName);
	ClassNameCache[usr] = className;
	return className;
}
std::string HeaderParser::GetClassFullName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (ClassFullNameCache.find(usr) != ClassFullNameCache.end()) {
		return ClassFullNameCache[usr];
	}
	auto className = GetClassName(cursor);
	auto parent = clang_getCursorSemanticParent(cursor);
	if (clang_Cursor_isNull(parent)) {
		ClassFullNameCache[usr] = className;
		return className;
	}

	std::string fullName;
	if (clang_isTranslationUnit(clang_getCursorKind(parent))) {
		fullName = className;
	}
	else {
		fullName = GetClassFullName(parent) + "::" + className;
	}
	ClassFullNameCache[usr] = fullName;
	return fullName;
}
std::string HeaderParser::GetFunctionMangledName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (FunctionMangledNameCache.find(usr) != FunctionMangledNameCache.end()) {
		return FunctionMangledNameCache[usr];
	}
	else {
		auto mangledName = clang_Cursor_getMangling(cursor);
		std::string mangledNameStr = clang_getCString(mangledName);
		clang_disposeString(mangledName);
		FunctionMangledNameCache[usr] = mangledNameStr;
		return mangledNameStr;
	}
}
std::string HeaderParser::GetFunctionShortName(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (FunctionShortNameCache.find(usr) != FunctionShortNameCache.end()) {
		return FunctionShortNameCache[usr];
	}
	else {
		auto shortName = clang_getCursorSpelling(cursor);
		std::string shortNameStr = clang_getCString(shortName);
		clang_disposeString(shortName);
		FunctionShortNameCache[usr] = shortNameStr;
		return shortNameStr;
	}
}
CursorLocation* HeaderParser::GetCursorLocation(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (CursorLocationCache.find(usr) != CursorLocationCache.end()) {
		return &CursorLocationCache[usr];
	}
	else {
		CXFile cxFile;
		unsigned int line = -1;
		unsigned int column = -1;
		clang_getSpellingLocation(clang_getCursorLocation(clang_getCursorDefinition(cursor)), &cxFile, &line, &column, nullptr);
		if (!cxFile && line == -1 && column == -1) {
			return nullptr;
		}
		
		fs::path filePath;
		if (cxFile) {
			auto cxFileName = clang_getFileName(cxFile);
			auto csFileName = clang_getCString(cxFileName);
			if (csFileName) {
				filePath = fs::path(csFileName).generic_string();
			}
			clang_disposeString(cxFileName);
		}
		else {
			filePath = "unknown";
		}
		
		return &(CursorLocationCache[usr] = CursorLocation{
			.FilePath = filePath,
			.Line = line,
			.Column = column
		});
	}
	return nullptr;
}

std::optional<std::string> HeaderParser::GetCommentForCursor(CXCursor cursor)
{
	auto usr = GetCursorUSR(cursor);
	if (CommentsCache.find(usr) != CommentsCache.end())
		return CommentsCache[usr];
	auto cxComment = clang_Cursor_getRawCommentText(cursor);
	auto csComment = clang_getCString(cxComment);
	if (!csComment)
	{
		clang_disposeString(cxComment);
		return std::nullopt;
	}
	std::string comment = csComment;
	clang_disposeString(cxComment);
	return CommentsCache[usr] = comment;
}
bool HeaderParser::IsOnFilter(const fs::path& file)
{
	if (!PathFilters.empty()) {
		for (const auto& filter : PathFilters) {
			if (PathUtils::CheapIsFrom(file, filter)) {
				return true;
			}
		}
		return false;
	}
	return true;
}
bool HeaderParser::WasDefinitionVisited(const std::string& usr)
{
	return VisitedDefinitions.count(usr);
}
bool HeaderParser::HasFunctionBody(CXCursor cursor) {
	CXCursor def = clang_getCursorDefinition(cursor);
	if (!clang_Cursor_isNull(def)) {
		bool hasBody = false;
		clang_visitChildren(def,
			[](CXCursor c, CXCursor parent, CXClientData client_data) {
				if (clang_getCursorKind(c) == CXCursor_CompoundStmt) {
					*reinterpret_cast<bool*>(client_data) = true;
					return CXChildVisit_Break;
				}
				return CXChildVisit_Recurse;
			},
			&hasBody);
		return hasBody;
	}

	bool hasBody = false;
	clang_visitChildren(cursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			if (clang_getCursorKind(c) == CXCursor_CompoundStmt) {
				*reinterpret_cast<bool*>(client_data) = true;
				return CXChildVisit_Break;
			}
			return CXChildVisit_Recurse;
		},
		&hasBody);
	return hasBody;
}
std::string HeaderParser::GetCursorUSR(CXCursor cursor)
{
	auto usr = clang_getCursorUSR(cursor);
	std::string usrStr = clang_getCString(usr);
	clang_disposeString(usr);
	return usrStr;
}
MethodInfo* HeaderParser::GetMethodInfo(const std::string& usr)
{
	if (Methods.find(usr) != Methods.end()) {
		return &Methods[usr];
	}
	return nullptr;
}
ClassInfo* HeaderParser::GetClassInfo(const std::string& usr)
{
	if (Classes.find(usr) != Classes.end()) {
		return &Classes[usr];
	}
	return nullptr;
}
bool HeaderParser::HasClass(const std::string& usr)
{
	return Classes.find(usr) != Classes.end();
}
bool HeaderParser::HasFunction(const std::string& mangledName)
{
	return Methods.find(mangledName) != Methods.end();
}
void HeaderParser::SortAllClassesTopologically()
{
	std::unordered_map<std::string, ClassInfo*> classMap;
	for (auto& [usr, cls] : Classes)
		classMap[usr] = &cls;

	std::unordered_set<ClassInfo*> visited;
	std::stack<ClassInfo*> sortedStack;
	std::function<void(ClassInfo*)> VisitClassDFS = [&](ClassInfo* cls) {
		if (!cls || visited.count(cls)) return;
		visited.insert(cls);

		for (const auto& baseUsr : cls->BaseClassUSRs) {
			auto it = classMap.find(baseUsr);
			if (it != classMap.end()) {
				VisitClassDFS(it->second);
			}
		}

		sortedStack.push(cls);
	};

	for (auto& [usr, cls] : Classes) {
		VisitClassDFS(&cls);
	}

	std::vector<std::pair<std::string, ClassInfo>> sortedClasses;
	while (!sortedStack.empty()) {
		ClassInfo* cls = sortedStack.top();
		sortedStack.pop();
		sortedClasses.emplace_back(cls->USR, *cls);
	}

	std::reverse(sortedClasses.begin(), sortedClasses.end());

	Classes.clear();
	for (auto& pair : sortedClasses) {
		Classes[pair.first] = std::move(pair.second);
	}
}
void HeaderParser::SortAllMethodsTopologically()
{
	std::unordered_map<std::string, MethodInfo*> methodsMap;
	for (auto& [usr, method] : Methods)
		methodsMap[usr] = &method;

	std::unordered_set<MethodInfo*> visited;
	std::vector<MethodInfo*> sortedMethods;

	std::function<void(MethodInfo*)> VisitMethodDFS = [&](MethodInfo* method) {
		if (!method || visited.count(method)) return;
		visited.insert(method);
		if (method->OverrideOfUSR.has_value()) {
			auto& overrideUsr = method->OverrideOfUSR.value();
			auto it = methodsMap.find(overrideUsr);
			if (it != methodsMap.end()) {
				VisitMethodDFS(it->second);
			}
		}
		sortedMethods.push_back(method);
	};

	for (auto& [name, method] : Methods) {
		VisitMethodDFS(&method);
	}

	std::unordered_map<std::string, MethodInfo> newMethods;
	for (auto* method : sortedMethods) {
		newMethods[method->USR] = std::move(*method);
	}

	Methods = std::move(newMethods);
}
void HeaderParser::ResolveAllClassBases()
{
	for (auto& [usr, classInfo] : Classes) {
		for (auto& baseUsr : classInfo.BaseClassUSRs) {
			if (auto* base = GetClassInfo(baseUsr)) {
				classInfo.BaseClasses.push_back(base);
			}
		}
	}
}
void HeaderParser::ResolveAllClassFunctions()
{
	for (auto& [classUsr, classInfo] : Classes) {
		for (auto& funcUsr : classInfo.MethodUSRs) {
			if (auto* method = GetMethodInfo(funcUsr)) {
				method->DeclaringClass = &classInfo;
				method->Location->FilePath = classInfo.Location->FilePath;
				classInfo.Methods.push_back(method);
			}
		}
	}
}
void HeaderParser::ResolveAllFunctionOverrides()
{
	for (auto& [funcUsr, funcInfo] : Methods) {
		if (funcInfo.OverrideOfUSR.has_value()) {
			auto& baseUsr = funcInfo.OverrideOfUSR.value();
			funcInfo.OverrideOf = GetMethodInfo(baseUsr);
		}
	}
}
void HeaderParser::ResolveBaseClassVirtualFunctionsIndex()
{
	for (auto& [className, classInfo] : Classes) {
		for (auto* func : classInfo.Methods) {
			auto* declaringClass = &classInfo;
			if (func->IsIndexResolved() || !declaringClass || !declaringClass->HasNoBases() || !func->IsVirtual)
				continue;
			int index = declaringClass->GetNextVirtualIndex();
			if (!func->IsDestructor)
				func->VirtualIndex = index;
			else {
				func->VirtualIndex = 0; // Destructors are always at index 0 of "vtable for this"
			}
			func->VirtualTableTarget = declaringClass->Name + "_vtable_for_this";
		}
	}
}
void HeaderParser::ResolveNewVirtualFunctionIndices()
{
	for (auto& [className, classInfo] : Classes) {
		for (auto* func : classInfo.Methods) {
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
	for (auto& [funcName, func] : Methods) {
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
void HeaderParser::PrintTranslationUnitErrors()
{
	unsigned numDiagnostics = clang_getNumDiagnostics(TranslationUnit);
	for (unsigned i = 0; i < numDiagnostics; ++i) {
		CXDiagnostic diag = clang_getDiagnostic(TranslationUnit, i);
		CXDiagnosticSeverity severity = clang_getDiagnosticSeverity(diag);
		if (severity != CXDiagnostic_Error && severity != CXDiagnostic_Fatal)
			continue;
		CXString diagStr = clang_formatDiagnostic(diag, clang_defaultDiagnosticDisplayOptions());
		std::cout << std::format("[Error] {}", clang_getCString(diagStr)) << std::endl;
		clang_disposeString(diagStr);
		clang_disposeDiagnostic(diag);
	}
}
void HeaderParser::DoStuff()
{
	VisitAll();
	SortAllClassesTopologically();
	SortAllMethodsTopologically();
	ResolveAllClassBases();
	ResolveAllClassFunctions();
	ResolveAllFunctionOverrides();
	ResolveBaseClassVirtualFunctionsIndex();
	ResolveNewVirtualFunctionIndices();
	ResolveAllFunctionOverridesIndices();
}
bool HeaderParser::HasAnyErrors()
{
	unsigned numDiagnostics = clang_getNumDiagnostics(TranslationUnit);
	for (unsigned i = 0; i < numDiagnostics; ++i) {
		CXDiagnostic diag = clang_getDiagnostic(TranslationUnit, i);
		CXDiagnosticSeverity severity = clang_getDiagnosticSeverity(diag);
		if (severity != CXDiagnostic_Error && severity != CXDiagnostic_Fatal)
			continue;
		return true;
	}
	return false;
}
std::vector<CXCursor> HeaderParser::GetMethodsInClass(CXCursor classCursor)
{
	std::vector<CXCursor> methods;
	clang_visitChildren(
		classCursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			auto& methods = *reinterpret_cast<std::vector<CXCursor>*>(client_data);
			CXCursorKind kind = clang_getCursorKind(c);
			if (kind == CXCursor_CXXMethod || kind == CXCursor_Constructor || kind == CXCursor_Destructor) {
				methods.push_back(c);
			}
			return CXChildVisit_Continue;
		},
		&methods
	);
	return methods;
}
std::vector<CXCursor> HeaderParser::GetBaseClassesOfClass(CXCursor classCursor)
{
	std::vector<CXCursor> bases;
	clang_visitChildren(
		classCursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			auto& bases = *reinterpret_cast<std::vector<CXCursor>*>(client_data);
			CXCursorKind kind = clang_getCursorKind(c);
			if (kind == CXCursor_CXXBaseSpecifier) {
				CXType type = clang_getCursorType(c);
				CXCursor declaration = clang_getTypeDeclaration(type);
				declaration = clang_getCanonicalCursor(declaration);
				declaration = clang_getCursorDefinition(declaration);
				if (!clang_Cursor_isNull(declaration))
					bases.push_back(declaration);
			}
			return CXChildVisit_Continue;
		},
		&bases
	);
	return bases;
}

void HeaderParser::VisitAll()
{
	CXCursor rootCursor = clang_getTranslationUnitCursor(TranslationUnit);
	clang_visitChildren(
		rootCursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			auto cursorHash = clang_hashCursor(c);
			HeaderParser& parser = *reinterpret_cast<HeaderParser*>(client_data);
			CXCursorKind kind = clang_getCursorKind(c);
			auto location = parser.GetCursorLocation(c);

			if (!location || !parser.IsOnFilter(location->FilePath))
				return CXChildVisit_Continue;

			if (kind == CXCursor_ClassDecl || kind == CXCursor_StructDecl) {
				ClassVisitingData data{ .mParser = parser, .mParents = {} };
				return parser.VisitClass(c, data);
			}
			else if (kind == CXCursor_FunctionDecl) {
				FunctionVisitingData data{ .mParser = parser, .mClassData = nullptr };
				return parser.VisitMethod(c, data);
			}
			return CXChildVisit_Recurse;
		},
		this
	);
}

CXChildVisitResult HeaderParser::VisitClass(CXCursor cursor, ClassVisitingData& data)
{
	std::string usr = GetCursorUSR(cursor);
	if (WasDefinitionVisited(usr) || HasClass(usr))
		return CXChildVisit_Continue;

	bool isDefinition = clang_isCursorDefinition(cursor);
	if (!isDefinition)
		return CXChildVisit_Continue;
	
	VisitedDefinitions.insert(usr);
	ClassInfo& classInfo = (Classes[usr] = ClassInfo{});
	classInfo.USR = usr;
	classInfo.Name = GetClassName(cursor);
	classInfo.Location = GetCursorLocation(cursor);
	classInfo.RawComment = GetCommentForCursor(cursor);
	classInfo.IsDefinition = isDefinition;

	auto baseClasses = GetBaseClassesOfClass(cursor);
	for (const auto& baseCursor : baseClasses) {
		std::string baseUsr = GetCursorUSR(baseCursor);
		classInfo.BaseClassUSRs.push_back(baseUsr);
	}

	auto methods = GetMethodsInClass(cursor);
	for (const auto& methodCursor : methods) {
		std::string methodUsr = GetCursorUSR(methodCursor);
		FunctionVisitingData funcData{ .mParser = data.mParser, .mClassData = &data };
		VisitMethod(methodCursor, funcData);
		classInfo.MethodUSRs.push_back(methodUsr);
	}

	return CXChildVisit_Continue;
}

CXChildVisitResult HeaderParser::VisitMethod(CXCursor cursor, FunctionVisitingData& data)
{
	std::string usr = GetCursorUSR(cursor);
	if (WasDefinitionVisited(usr) || HasFunction(usr))
		return CXChildVisit_Continue;

	VisitedDefinitions.insert(usr);
	MethodInfo& methodInfo = (Methods[usr] = MethodInfo{});
	methodInfo.USR = usr;
	methodInfo.MangledName = GetFunctionMangledName(cursor);
	methodInfo.ShortName = GetFunctionShortName(cursor);
	methodInfo.Location = GetCursorLocation(cursor);
	methodInfo.RawComment = GetCommentForCursor(cursor);
	methodInfo.IsDestructor = clang_getCursorKind(cursor) == CXCursor_Destructor;
	methodInfo.IsVirtual = clang_CXXMethod_isVirtual(cursor);
	methodInfo.IsExternal = clang_Cursor_isExternalSymbol(cursor, nullptr, nullptr, nullptr);
	methodInfo.HasBody = HasFunctionBody(cursor);

	bool isImported = false;
	clang_visitChildren(
		cursor,
		[](CXCursor c, CXCursor parent, CXClientData client_data) {
			bool& isImport = *reinterpret_cast<bool*>(client_data);
			CXCursorKind kind = clang_getCursorKind(c);
			if (kind == CXCursor_DLLImport) {
				isImport = true;
				return CXChildVisit_Break;
			}
			return CXChildVisit_Continue;
		},
		&isImported
	);

	methodInfo.IsImported = isImported;

	CXCursor* overriden;
	uint32_t numOverriden;
	clang_getOverriddenCursors(cursor, &overriden, &numOverriden);
	if (numOverriden > 0) {
		auto baseUsr = GetCursorUSR(overriden[0]);
		methodInfo.OverrideOfUSR = baseUsr;
	}
	return CXChildVisit_Continue;
}
