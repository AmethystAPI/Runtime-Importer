#include <iostream>
#include "clang-c/Index.h"
#include "CLI11.hpp"
#include "Json.hpp"
#include "xxhash/xxhash.h"

using namespace nlohmann;
namespace fs = std::filesystem;


inline static fs::path InputDirectory;
inline static std::string Filter;

bool startsWith(const fs::path& path, const fs::path& base) {
    auto rel = path.lexically_relative(base);
    // If `rel` does NOT start with ".." and is not empty, `path` is inside `base`
    return !rel.empty() && rel.native().substr(0, 2) != L"..";
}

uint64_t hashFile(const fs::path& path)
{
	std::ifstream file(path, std::ios::binary);
	if (!file) return 0;

	std::vector<char> buffer((std::istreambuf_iterator<char>(file)),
		std::istreambuf_iterator<char>());

	return XXH64(buffer.data(), buffer.size(), 0);
}

void createSymbolFile(const fs::path& headerPath, const fs::path& outputPath, const std::string& content)
{
	fs::path headerName = headerPath.stem();
	fs::path parentPath = headerPath.parent_path();
	fs::path fullOutputPath = outputPath / parentPath / (headerName.string() + ".symbols.json");
	fs::create_directories(fullOutputPath.parent_path());
	std::ofstream outFile(fullOutputPath);
	outFile << content;
	outFile.close();
	std::cout << "Generated symbol file: " << fullOutputPath << "\n";
}

void deleteSymbolFile(const fs::path& headerPath, const fs::path& outputPath)
{
    fs::path headerName = headerPath.stem();
    fs::path parentPath = headerPath.parent_path();
    fs::path fullOutputPath = outputPath / parentPath / (headerName.string() + ".symbols.json");
    if (fs::exists(fullOutputPath)) {
        fs::remove(fullOutputPath);
        std::cout << "Deleted symbol file: " << fullOutputPath << "\n";
    }
}

void visitCursor(CXCursor cursor, std::vector<std::string>& scope) {
    CXCursorKind kind = clang_getCursorKind(cursor);
    CXSourceLocation loc = clang_getCursorLocation(cursor);
    CXFile file;
    unsigned line, column, offset;
    clang_getSpellingLocation(loc, &file, &line, &column, &offset);

    if (file) {
        CXString fileName = clang_getFileName(file);
        std::string path = clang_getCString(fileName);
        clang_disposeString(fileName);
		fs::path relativePathOfHeader = fs::relative(path, InputDirectory.string());
        if (!startsWith(relativePathOfHeader, Filter)) {
            // recurse on children
            clang_visitChildren(cursor,
                [](CXCursor c, CXCursor parent, CXClientData client_data) {
                    auto* scope = static_cast<std::vector<std::string>*>(client_data);
                    visitCursor(c, *scope);
                    return CXChildVisit_Continue;
                },
                &scope
            );
            return;
        }
    }
    else {
        // recurse on children
        clang_visitChildren(cursor,
            [](CXCursor c, CXCursor parent, CXClientData client_data) {
                auto* scope = static_cast<std::vector<std::string>*>(client_data);
                visitCursor(c, *scope);
                return CXChildVisit_Continue;
            },
            &scope
        );
        return;
	}

    // Namespaces
    if (kind == CXCursor_Namespace) {
        CXString name = clang_getCursorSpelling(cursor);
        scope.push_back(clang_getCString(name));
        clang_disposeString(name);

        clang_visitChildren(cursor,
            [](CXCursor c, CXCursor parent, CXClientData client_data) {
                auto* scope = static_cast<std::vector<std::string>*>(client_data);
                visitCursor(c, *scope);
                return CXChildVisit_Continue;
            },
            &scope
        );

        scope.pop_back();
        return;
    }

    // Classes/structs
    if (kind == CXCursor_ClassDecl || kind == CXCursor_StructDecl) {
        CXString name = clang_getCursorSpelling(cursor);
        scope.push_back(clang_getCString(name));
        clang_disposeString(name);

        clang_visitChildren(cursor,
            [](CXCursor c, CXCursor parent, CXClientData client_data) {
                auto* scope = static_cast<std::vector<std::string>*>(client_data);
                visitCursor(c, *scope);
                return CXChildVisit_Continue;
            },
            &scope
        );

        scope.pop_back();
        return;
    }

    // Functions + methods
    if (kind == CXCursor_FunctionDecl || kind == CXCursor_CXXMethod) {

        CXString mangled = clang_Cursor_getMangling(cursor);
        std::string mangledName = clang_getCString(mangled);

        std::cout << "Mangled: " << mangledName << std::endl;
        CXString name = clang_getCursorSpelling(cursor);
        CXString comment = clang_Cursor_getRawCommentText(cursor);
        std::cout << "Function: " << std::string((char*)name.data) << std::endl;
        if (comment.data != nullptr)
            std::cout << "Comment: " << std::string((char*)comment.data) << std::endl;

        clang_disposeString(mangled);
    }

    // recurse on children
    clang_visitChildren(cursor,
        [](CXCursor c, CXCursor parent, CXClientData client_data) {
            auto* scope = static_cast<std::vector<std::string>*>(client_data);
            visitCursor(c, *scope);
            return CXChildVisit_Continue;
        },
        &scope
    );
}


int main(int argc, char** argv) {
	CLI::App app{ "Amethyst Symbol Generator v0.0.1" };
    fs::path inputDirectory;
	fs::path generatedDirectory;
    std::string filter;

	// Add options
	app.add_option("--input-directory", inputDirectory, "The input directory to look for header files.");
	app.add_option("--generated-directory", generatedDirectory, "The output directory to write generated files to.");
	app.add_option("--filter", filter, "A filter to apply to the headers (e.g., only process headers at this relative path).")->default_val("");

	std::vector<std::string> clangArgs;
	app.allow_extras();
	app.set_help_all_flag("--help-all", "Expand all help");

	// Parse command line arguments
	CLI11_PARSE(app, argc, argv);

	// Collect remaining arguments as clang args
	clangArgs = app.remaining();

	std::cout << "Input Directory: " << inputDirectory << "\n";
	std::cout << "Generated Directory: " << generatedDirectory << "\n";
	std::cout << "Clang args:\n";
	for (auto& arg : clangArgs) {
		std::cout << "  " << arg << "\n";
	}

	InputDirectory = inputDirectory;
    Filter = filter;

	std::unordered_map<std::string, uint64_t> oldHashes;

    fs::path checksumFile = "checksums.json";
	fs::path checksumPath = generatedDirectory / checksumFile;

    // Load existing checksums if present
    if (fs::exists(checksumPath))
    {
        std::ifstream in(checksumPath);
        json j;
        in >> j;
        for (auto& [key, val] : j.items())
            oldHashes[key] = val.get<uint64_t>();
    }

    // Collect headers
    std::vector<fs::path> headers;
    for (auto& p : fs::recursive_directory_iterator(inputDirectory)) {
		fs::path relativeHeaderPath = fs::relative(p.path(), inputDirectory);
		if (!filter.empty() && !startsWith(relativeHeaderPath, filter))
			continue;
        if (relativeHeaderPath.extension() == ".hpp")
            headers.push_back(relativeHeaderPath);
        if (relativeHeaderPath.extension() == ".h")
            headers.push_back(relativeHeaderPath);
    }

    std::vector<std::string> tuIncludes;

    std::unordered_map<std::string, uint64_t> newHashes;
    for (auto& h : headers)
    {
        uint64_t hHash = hashFile(inputDirectory / h);
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
			deleteSymbolFile(oldPath, generatedDirectory / "symbols");
        }
    }

    // Save updated checksums
    json out;
    for (auto& [path, h] : newHashes)
        out[path] = h;

    std::ofstream o(checksumPath);
    o << out.dump(4);
	o.close();

	std::ofstream tuFile(generatedDirectory / "generated.cpp");
	tuFile << "// This file was generated by Amethyst Symbol Generator.\n\n";
    for (auto& inc : tuIncludes)
		tuFile << "#include \"" << inc << "\"\n";
    tuFile.close();
	std::cout << "Generated translation unit: " << (generatedDirectory / "generated.cpp") << "\n";

	// Set up Clang index
	CXIndex index = clang_createIndex(0, 0);
    if (!index) {
        std::cerr << "Failed to create Clang index.\n";
        return 1;
	}

	// Set up Clang translation unit
    std::vector<const char*> clangArgCStrs;
    for (auto& arg : clangArgs)
		clangArgCStrs.push_back(arg.c_str());
    auto start = std::chrono::high_resolution_clock::now();
	CXTranslationUnit tu = clang_parseTranslationUnit(
        index,
        (generatedDirectory / "generated.cpp").string().c_str(),
        clangArgCStrs.data(), static_cast<int>(clangArgCStrs.size()),
        nullptr, 0,
		CXTranslationUnit_None | CXTranslationUnit_DetailedPreprocessingRecord | CXTranslationUnit_SkipFunctionBodies);

	if (!tu) {
        std::cerr << "Failed to parse translation unit.\n";
        clang_disposeIndex(index);
        return 1;
	}

    CXCursor rootCursor = clang_getTranslationUnitCursor(tu);
    std::vector<std::string> scope;
    visitCursor(rootCursor, scope);

	// Dispose of Clang resources
	clang_disposeTranslationUnit(tu);
    clang_disposeIndex(index);

    auto end = std::chrono::high_resolution_clock::now();
    std::chrono::duration<double> diff = end - start;

    std::cout << "Parsing + visiting took " << diff.count() << " seconds\n";

    // Pause before exit
	std::cout << "Press Enter to exit...";
	std::cin.get();
}