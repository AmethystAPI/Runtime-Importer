#include <iostream>
#include <unordered_set>
#include <stack>
#include "clang-c/Index.h"
#include "CLI11.hpp"
#include "Json.hpp"
#include "Utils.hpp"
#include "xxhash/xxhash.h"
#include "PathUtils.hpp"
#include "SymbolGenerator.hpp"
#include <algorithm>

using namespace nlohmann;
namespace fs = std::filesystem;

struct VirtualMethod {
    std::string mName;
	VirtualMethod* mOverrideOf;
    int mIndex = 0;

	bool IsOverride() const { return mOverrideOf != nullptr; }
};

struct ClassVirtualTable {
    std::string mClassName;
    std::vector<VirtualMethod*> mMethods;
	int mIndirectBaseCount = 0;
    int mDirectBaseCount = 0;
    std::vector<std::string> mIndirectBases;
	std::vector<std::string> mDirectBases;
};

struct SymbolGeneratorContext {
    fs::path InputDirectory;
	fs::path GeneratedDirectory;
	std::vector<fs::path> Filters;
	std::vector<std::string> ClangArgs;
    std::unordered_map<fs::path, fs::path> PathRelativeOfCache;
    std::unordered_map<fs::path, bool> ShouldProcessFileCache;
    std::unordered_set<std::string> VirtualClassesCache;
	std::unordered_map<std::string, ClassVirtualTable> VirtualTables;
	std::unordered_map<std::string, VirtualMethod> VirtualMethods;
} Context;

struct VisitorData {

};

void VisitCursor(CXCursor cursor, VisitorData& visitorData);
void VisitChildren(CXCursor cursor, VisitorData& visitorData) {
    clang_visitChildren(cursor,
        [](CXCursor c, CXCursor parent, CXClientData client_data) {
			VisitorData& visitorData = *reinterpret_cast<VisitorData*>(client_data);
            VisitCursor(c, visitorData);
            return CXChildVisit_Continue;
        },
		&visitorData
    );
}

bool ClassHasVirtualMethods(CXCursor classCursor) {
	CXString cxClassName = clang_getCursorSpelling(classCursor);
	std::string className = clang_getCString(cxClassName);
	clang_disposeString(cxClassName);

    if (Context.VirtualClassesCache.find(className) != Context.VirtualClassesCache.end()) {
        return true;
    }

    bool hasVirtual = false;
    clang_visitChildren(
        classCursor,
        [](CXCursor c, CXCursor parent, CXClientData client_data) {
            bool* hasVirtual = reinterpret_cast<bool*>(client_data);
            CXCursorKind kind = clang_getCursorKind(c);

            if (kind == CXCursor_CXXMethod) {
                if (clang_CXXMethod_isVirtual(c)) {
                    *hasVirtual = true;
                    return CXChildVisit_Break;
                }
            }
            else if (kind == CXCursor_CXXBaseSpecifier) {
                CXType baseType = clang_getCursorType(c);
                CXCursor baseClassCursor = clang_getTypeDeclaration(baseType);

                if (ClassHasVirtualMethods(baseClassCursor)) {
                    *hasVirtual = true;
                    return CXChildVisit_Break;
                }
            }

            return CXChildVisit_Recurse;
        },
        &hasVirtual
    );

    if (hasVirtual) {
        Context.VirtualClassesCache.insert(className);
	}
    return hasVirtual;
}

struct VirtualClassVisitorData {
	VisitorData& mData;
    std::stack<std::string> mParentNames;
    std::stack<std::vector<VirtualMethod*>> mFunctions;
	std::unordered_map<std::string, int> mAllIndirectBaseCounts;
	std::unordered_map<std::string, int> mAllDirectBaseCounts;
	std::unordered_map<std::string, std::vector<std::string>> mAllIndirectBases;
	std::unordered_map<std::string, std::vector<std::string>> mAllDirectBases;
	int mCurrentFunctionIndex = 0;
	int mBaseCount = 0;
};

struct VirtualMethodVisitorData {
    VirtualClassVisitorData& mData;
	std::vector<VirtualMethod*> mMethods;
};

void GetAllIndirectBasesFor(const std::string& className, std::vector<std::string>& allBases) {
    if (Context.VirtualTables.find(className) == Context.VirtualTables.end())
        return;
    auto& vtable = Context.VirtualTables[className];
    for (auto& base : vtable.mDirectBases) {
		allBases.push_back(base);
        GetAllIndirectBasesFor(base, allBases);
    }
}

ClassVirtualTable& GetClassVirtualTable(CXCursor cursor, VirtualClassVisitorData& visitorData) {
	auto cxClassName = clang_getCursorSpelling(cursor);
	std::string className = clang_getCString(cxClassName);
	clang_disposeString(cxClassName);

    if (Context.VirtualTables.find(className) != Context.VirtualTables.end()) {
        return Context.VirtualTables[className];
	}

    ClassVirtualTable vtable = {
        .mClassName = className,
        .mMethods = {},
        .mIndirectBaseCount = 0
    };

    visitorData.mParentNames.push(className);
    clang_visitChildren(
        cursor,
        [](CXCursor c, CXCursor parent, CXClientData client_data) {
            VirtualClassVisitorData& visitorData = *reinterpret_cast<VirtualClassVisitorData*>(client_data);
            CXCursorKind kind = clang_getCursorKind(c);
            if (kind == CXCursor_CXXBaseSpecifier) {
				if (!ClassHasVirtualMethods(c))
					return CXChildVisit_Continue;

				int prevFunctionIndex = visitorData.mCurrentFunctionIndex;
				visitorData.mCurrentFunctionIndex = 0;

				visitorData.mAllIndirectBaseCounts[visitorData.mParentNames.top()]++;
				visitorData.mAllDirectBaseCounts[visitorData.mParentNames.top()]++;
				auto& vtable = GetClassVirtualTable(c, visitorData);
				visitorData.mAllIndirectBases[visitorData.mParentNames.top()].push_back(vtable.mClassName);
                auto& indirectBases = visitorData.mAllIndirectBases[visitorData.mParentNames.top()];
				GetAllIndirectBasesFor(vtable.mClassName, indirectBases);
				visitorData.mAllDirectBases[visitorData.mParentNames.top()].push_back(vtable.mClassName);
                visitorData.mAllIndirectBaseCounts[visitorData.mParentNames.top()] += vtable.mIndirectBaseCount;

                visitorData.mCurrentFunctionIndex = prevFunctionIndex;
            }
            return CXChildVisit_Continue;
        },
        &visitorData
	);

    VirtualMethodVisitorData visitorDataMethods = { .mData = visitorData, .mMethods = {} };
    clang_visitChildren(
        cursor,
        [](CXCursor c, CXCursor parent, CXClientData client_data) {
            VirtualMethodVisitorData& visitorData = *reinterpret_cast<VirtualMethodVisitorData*>(client_data);
            CXCursorKind kind = clang_getCursorKind(c);
            if (kind == CXCursor_CXXMethod) {
                auto cxMethodMangledName = clang_Cursor_getMangling(c);
                std::string methodMangledName = clang_getCString(cxMethodMangledName);
                clang_disposeString(cxMethodMangledName);
                if (Context.VirtualMethods.find(methodMangledName) != Context.VirtualMethods.end()) {
                    visitorData.mMethods.push_back(&Context.VirtualMethods[methodMangledName]);
                    return CXChildVisit_Continue;
				}

                if (clang_CXXMethod_isVirtual(c)) {
					CXCursor* overridden = nullptr;
					unsigned numOverridden = 0;
					clang_getOverriddenCursors(c, &overridden, &numOverridden);

					VirtualMethod* overrideOf = nullptr;
                    int index = 0;
                    if (numOverridden > 0) {
                        auto cxOverridenMangledName = clang_Cursor_getMangling(overridden[0]);
						std::string overridenMangledName = clang_getCString(cxOverridenMangledName);
						clang_disposeString(cxOverridenMangledName);
						auto* method = &Context.VirtualMethods[overridenMangledName];
						overrideOf = method;
                        index = method->mIndex;
                    }
                    else {
						index = visitorData.mData.mCurrentFunctionIndex++;
                    }

					VirtualMethod method = { methodMangledName, overrideOf, index };
					Context.VirtualMethods[methodMangledName] = method;
                    visitorData.mMethods.push_back(&Context.VirtualMethods[methodMangledName]);
                }
            }
            return CXChildVisit_Continue;
        },
        &visitorDataMethods
    );

	vtable.mMethods = visitorDataMethods.mMethods;
	visitorData.mFunctions.push(visitorDataMethods.mMethods);
	vtable.mIndirectBaseCount = visitorData.mAllIndirectBaseCounts[className];
    vtable.mDirectBaseCount = visitorData.mAllDirectBaseCounts[className];
	vtable.mIndirectBases = visitorData.mAllIndirectBases[className];
	vtable.mDirectBases = visitorData.mAllDirectBases[className];
    Context.VirtualTables[className] = vtable;
	return Context.VirtualTables[className];
}

void ProcessClass(CXCursor cursor, VisitorData& visitorData) {
    if (ClassHasVirtualMethods(cursor)) {
		VirtualClassVisitorData vdata = { .mData = visitorData };
        GetClassVirtualTable(cursor, vdata);
		
    }
}

void VisitCursor(CXCursor cursor, VisitorData& visitorData) {
    CXCursorKind kind = clang_getCursorKind(cursor);
    CXSourceLocation loc = clang_getCursorLocation(cursor);

    CXFile file;
    unsigned line, column, offset;

    clang_getSpellingLocation(loc, &file, &line, &column, &offset);
    if (!file) {
		VisitChildren(cursor, visitorData);
        return;
    }

	CXString filename = clang_getFileName(file);
	fs::path filenamePath = clang_getCString(filename);
	clang_disposeString(filename);


    fs::path headerRelativeToInput;
    if (Context.PathRelativeOfCache.find(filenamePath) != Context.PathRelativeOfCache.end()) {
        headerRelativeToInput = Context.PathRelativeOfCache[filenamePath];
    }
    else {
        headerRelativeToInput = fs::relative(filenamePath, Context.InputDirectory);
        Context.PathRelativeOfCache[filenamePath] = headerRelativeToInput;
	}

    bool shouldProcess;
    if (Context.ShouldProcessFileCache.find(filenamePath) != Context.ShouldProcessFileCache.end()) {
        shouldProcess = Context.ShouldProcessFileCache[filenamePath];
    }
    else {
        shouldProcess = false;
        if (Context.Filters.empty()) {
            shouldProcess = true;
        }
        else {
            for (auto& filter : Context.Filters) {
                if (PathUtils::StartsWith(headerRelativeToInput, filter)) {
                    shouldProcess = true;
                    break;
                }
            }
        }
        Context.ShouldProcessFileCache[filenamePath] = shouldProcess;
    }

    if (!shouldProcess) {
        return;
    }

    // Namespaces
    if (kind == CXCursor_Namespace) {
        VisitChildren(cursor, visitorData);
        return;
    }

    // Classes/structs
    if (kind == CXCursor_ClassDecl || kind == CXCursor_StructDecl) {
		ProcessClass(cursor, visitorData);
        VisitChildren(cursor, visitorData);
        return;
    }

	// C++ methods
    if (kind == CXCursor_CXXMethod) {
        
    }

	// Free functions
    if (kind == CXCursor_FunctionDecl) {

    }
}

void PrintMethod(VirtualMethod* method, int indent) {
    std::cout << std::string(indent, ' ') << "Method: '" << method->mName << "' of Index: " << method->mIndex << "\n";
    if (method->IsOverride()) {
        std::cout << std::string(indent + 2, ' ') << "Overrides:\n";
        PrintMethod(method->mOverrideOf, indent + 4);
    }
}

int main(int argc, char** argv) {
	CLI::App app{ "Amethyst Symbol Generator v0.0.1" };

	// Add options
	app.add_option("--input-directory", Context.InputDirectory, "The input directory to look for header files.");
	app.add_option("--generated-directory", Context.GeneratedDirectory, "The output directory to write generated files to.");
    app.add_option("--filters", Context.Filters, "Only process headers that have the relative path starting with those filters.")->expected(1, -1);
	app.allow_extras();
	app.set_help_all_flag("--help-all", "Expand all help");

	// Parse command line arguments
	CLI11_PARSE(app, argc, argv);

	// Collect remaining arguments as clang args
    Context.ClangArgs = app.remaining();

    fs::path checksumFile = "checksums.json";
    fs::path checksumPath = Context.GeneratedDirectory / checksumFile;

	// Load existing checksums if present
	std::unordered_map<std::string, uint64_t> oldHashes;
    if (fs::exists(checksumPath))
    {
        std::ifstream in(checksumPath);
        json j;
        in >> j;
        for (auto& [key, val] : j.items())
            oldHashes[key] = val.get<uint64_t>();
    }

    // Collect all headers
    std::vector<fs::path> headers;
    if (Context.Filters.empty()) {
        for (auto& p : fs::recursive_directory_iterator(Context.InputDirectory)) {
            if (p.path().extension() == ".h" || p.path().extension() == ".hpp")
                headers.push_back(fs::relative(p.path(), Context.InputDirectory));
        }
    }
    else {
        for (auto& filter : Context.Filters) {
            for (auto& p : fs::recursive_directory_iterator(Context.InputDirectory / filter)) {
                if (p.path().extension() == ".h" || p.path().extension() == ".hpp")
                    headers.push_back(fs::relative(p.path(), Context.InputDirectory));
            }
        }
    }

    std::vector<std::string> tuIncludes;

    std::unordered_map<std::string, uint64_t> newHashes;
    for (auto& h : headers)
    {
        uint64_t hHash = Utils::GetHashForFile(Context.InputDirectory / h);
        newHashes[h.string()] = hHash;

        // Decide if we need to reparse
        if (oldHashes.find(h.string()) == oldHashes.end() || oldHashes[h.string()] != hHash)
        {
            std::cout << "[Changed/Added] " << h << "\n";
			tuIncludes.push_back(h.string());
        }
    }

    for (auto& [oldPath, oldHash] : oldHashes) {
        if (newHashes.find(oldPath) == newHashes.end()) {
            std::cout << "[Deleted] " << oldPath << "\n";
			SymbolGenerator::DeleteSymbolsFor(Context.GeneratedDirectory / "symbols", oldPath);
        }
    }

    // Save updated checksums
    json out;
    for (auto& [path, h] : newHashes)
        out[path] = h;

    std::ofstream o(checksumPath);
    o << out.dump(4);
	o.close();

	std::ofstream tuFile(Context.GeneratedDirectory / "generated.cpp");
	tuFile << "// This file was generated by Amethyst Symbol Generator.\n\n";
    for (auto& inc : tuIncludes)
		tuFile << "#include \"" << inc << "\"\n";
    tuFile.close();
	std::cout << "Generated translation unit: " << (Context.GeneratedDirectory / "generated.cpp") << "\n";

	// Set up Clang index
	CXIndex index = clang_createIndex(0, 0);
    if (!index) {
        std::cerr << "Failed to create Clang index.\n";
        return 1;
	}

	// Set up Clang translation unit
    std::vector<const char*> clangArgCStrs;
    for (auto& arg : Context.ClangArgs)
		clangArgCStrs.push_back(arg.c_str());
    auto start = std::chrono::high_resolution_clock::now();
	CXTranslationUnit tu = clang_parseTranslationUnit(
        index,
        (Context.GeneratedDirectory / "generated.cpp").string().c_str(),
        clangArgCStrs.data(), static_cast<int>(clangArgCStrs.size()),
        nullptr, 0,
		CXTranslationUnit_DetailedPreprocessingRecord |
        CXTranslationUnit_SkipFunctionBodies);

	if (!tu) {
        std::cerr << "Failed to parse translation unit.\n";
        clang_disposeIndex(index);
        return 1;
	}

    CXCursor rootCursor = clang_getTranslationUnitCursor(tu);
	VisitorData visitorData;
    VisitCursor(rootCursor, visitorData);
    

    auto& vtables = Context.VirtualTables;
    for (auto& [className, vtable] : vtables) {
        if (vtable.mIndirectBaseCount == 1) {
			auto& base = Context.VirtualTables[vtable.mIndirectBases[0]];
			int maxIndex = -1;
            for (auto& method : base.mMethods) {
				maxIndex = std::max(maxIndex, method->mIndex);
            }
            for (auto& method : vtable.mMethods) {
                if (!method->IsOverride()) {
					method->mIndex += maxIndex + 1;
                }
            }
        }
		std::cout << "Class: " << className << " (" << vtable.mMethods.size() << " methods)" << " - " << "(" << vtable.mIndirectBaseCount << "/" << vtable.mDirectBaseCount << " indirect/direct bases)\n";
        for (auto& base : vtable.mIndirectBases) {
            std::cout << "  Indirect Base: " << base << "\n";
		}
        for (auto& method : vtable.mMethods) {
			PrintMethod(method, 6);
        }
    }

    bool anyErrors = false;
    unsigned numDiags = clang_getNumDiagnostics(tu);


    for (unsigned i = 0; i < numDiags; ++i) {
        CXDiagnostic diag = clang_getDiagnostic(tu, i);
        CXDiagnosticSeverity sev = clang_getDiagnosticSeverity(diag);

        CXString str = clang_formatDiagnostic(diag, clang_defaultDiagnosticDisplayOptions());
        std::string error = clang_getCString(str);
        clang_disposeString(str);

        if (sev == CXDiagnostic_Fatal || sev == CXDiagnostic_Error) {
            bool isMissingHeader = (error.find("file not found") != std::string::npos);
            bool isUnknownType = (error.find("unknown type") != std::string::npos);

            if (true) {
                CXSourceLocation loc = clang_getDiagnosticLocation(diag);
                CXFile file;
                unsigned line, col, offset;
                clang_getSpellingLocation(loc, &file, &line, &col, &offset);

                CXString fname = file ? clang_getFileName(file) : clang_getDiagnosticSpelling(diag);

                std::cerr << "[Fatal Error] " << error;
                if (file)
                    std::cerr << " in " << clang_getCString(fname) << ":" << line << ":" << col;
                std::cerr << "\n";

                clang_disposeString(fname);
                anyErrors = true;
            }
        }

        clang_disposeDiagnostic(diag);
    }


    

	// Dispose of Clang resources
	clang_disposeTranslationUnit(tu);
    clang_disposeIndex(index);

    auto end = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double> diff = end - start;

    std::cout << "Parsing + visiting took " << diff.count() << " seconds\n";

    if (anyErrors)
        return -1;

    // Pause before exit
	std::cout << "Press Enter to exit...";
	std::cin.get();
}