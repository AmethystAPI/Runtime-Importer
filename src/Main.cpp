#include <iostream>
#include <unordered_set>
#include <stack>
#include "clang-c/Index.h"
#include "CLI11.hpp"
#include "Json.hpp"
#include "Utils.hpp"
#include "xxhash/xxhash.h"
#include "PathUtils.hpp"
#include "parsing/HeaderParser.hpp"
#include <algorithm>
#include "parsing/CommentParser.hpp"
#include "HeaderCollector.hpp"
#include "regex"

using namespace nlohmann;
namespace fs = std::filesystem;

struct SymbolGeneratorContext {
    fs::path InputDirectory;
	fs::path GeneratedDirectory;
	std::vector<fs::path> Filters;
	std::vector<std::string> ClangArgs;
} Context;

void PrintClassInfo(ClassInfo* cls, int indent = 0) {
    if (!cls) return;

    std::string indentation(indent * 2, ' ');
    std::cout << indentation << "Class: " << cls->Name
        << " | Virtual: " << (cls->HasAtLeastOneVirtualFunction() ? "yes" : "no")
        << " | Base: " << (cls->HasNoBases() ? "yes" : "no")
        << " | Multi-Inheritance: " << (cls->DoesMultiInheritance() ? "yes" : "no")
		<< " | LastOwnVirtualIndex: " << cls->NextVirtualIndex
        << "\n";

    if (!cls->Functions.empty()) {
        for (auto* func : cls->Functions) {
            std::cout << indentation << "  - Function: " << func->ShortName
                << " | Virtual: " << (func->IsVirtual ? "yes" : "no")
                << " | VTableIndex: " << func->VirtualIndex
                << " | VTableTarget: " << (func->VirtualTableTarget.has_value() ? *func->VirtualTableTarget : "invalid_vtable");
            if (func->OverrideOfName.has_value())
                std::cout << " | Overrides: " << func->OverrideOfName.value();
            std::cout << "\n";
        }
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
    Context.InputDirectory = Context.InputDirectory.generic_string();

    fs::path checksumPath = Context.GeneratedDirectory / "checksums.json";
    fs::path symbolsPath = Context.GeneratedDirectory / "symbols";

    std::vector<fs::path> includes;
    Utils::Benchmark([&]() {
        HeaderCollector collector(Context.InputDirectory, checksumPath, Context.Filters);
        auto changes = collector.CollectChangedHeaders();
        for (auto& change : changes) {
            if (change.Type == ChangeType::AddedOrChanged) {
                std::ifstream file(Context.InputDirectory / change.Path);
                if (!file)
                    continue;
                std::string content((std::istreambuf_iterator<char>(file)),
                    std::istreambuf_iterator<char>());

                std::regex marker(R"(\/\/\/\s*@symgen)");
                if (!std::regex_search(content, marker))
                    continue;
                includes.push_back(change.Path);
            }
            else if (change.Type == ChangeType::Removed) {
                fs::path symbolPath = symbolsPath / change.Path;
                if (fs::exists(symbolPath))
                    fs::remove(symbolPath);
            }
        }
    }, "Collect headers");

    std::vector<const char*> clangArgCStrs;
    for (auto& arg : Context.ClangArgs)
        clangArgCStrs.push_back(arg.c_str());

    auto mainFile = Context.GeneratedDirectory / "generated.cpp";
    Utils::CreateCPPFileFor(mainFile, includes);
    HeaderParser parser(
        Context.InputDirectory,
        mainFile.string(),
        clangArgCStrs,
        CXTranslationUnit_DetailedPreprocessingRecord |
        CXTranslationUnit_SkipFunctionBodies,
        ParsingData{
            .mInputDirectory = Context.InputDirectory
        },
        Context.Filters
    );

    Utils::Benchmark([&]() {
        parser.DoStuff();
    }, "Parse all headers");

    for (auto& [className, classInfo] : parser.mClasses) {
        if (!classInfo.IsDefinition || !classInfo.Comment)
            continue;
        auto definedInRelative = fs::relative(classInfo.DefinedIn, Context.InputDirectory);
        bool isInIncludes = false;
        for (auto& inc : includes) {
            if (inc == definedInRelative) {
                isInIncludes = true;
                break;
            }
        }
        if (isInIncludes)
            PrintClassInfo(&classInfo);
    }
    fs::create_directories(symbolsPath);

    // Pause before exit
	std::cout << "Press Enter to exit...";
	std::cin.get();
}