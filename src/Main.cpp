#include <iostream>
#include <unordered_set>
#include <stack>
#include <algorithm>
#include <regex>

#include "Json.hpp"
#include "CLI11.hpp"

#include "utils/PathUtils.hpp"
#include "utils/Utils.hpp"
#include "utils/ClassInfoPrinter.hpp"

#include "generation/HeaderCollector.hpp"
#include "generation/symbol/SymbolGenerator.hpp"

#include "parsing/HeaderParser.hpp"
#include "parsing/annotations/CommentProcessor.hpp"
#include "parsing/annotations/AnnotationProcessor.hpp"


using namespace nlohmann;
namespace fs = std::filesystem;

struct SymbolGeneratorContext {
    fs::path InputDirectory;
    fs::path GeneratedDirectory;
	std::vector<fs::path> Filters;
	std::vector<std::string> ClangArgs;
};

int main(int argc, char** argv) {
    SymbolGeneratorContext ctx;
	CLI::App app{ "Amethyst Symbol Generator v0.0.1" };

	// Add options
	app.add_option("--input-directory", ctx.InputDirectory, "The input directory to look for header files.");
	app.add_option("--generated-directory", ctx.GeneratedDirectory, "The output directory to write generated files to.");
    app.add_option("--filters", ctx.Filters, "Only process headers that have the relative path starting with those filters.")->expected(1, -1);
	app.allow_extras();
	app.set_help_all_flag("--help-all", "Expand all help");

	// Parse command line arguments
	CLI11_PARSE(app, argc, argv);

	// Collect remaining arguments as clang args
    ctx.ClangArgs = app.remaining();

    ctx.InputDirectory = ctx.InputDirectory.generic_string();
	ctx.GeneratedDirectory = ctx.GeneratedDirectory.generic_string();
    for (auto& filter : ctx.Filters)
		filter = (ctx.InputDirectory / filter).generic_string();

    fs::path checksumPath = ctx.GeneratedDirectory / "checksums.json";
    fs::path symbolsPath = ctx.GeneratedDirectory / "symbols";

	// Collect all headers that have changed
    std::vector<fs::path> includes;
    Utils::Benchmark([&]() {
        HeaderCollector collector(ctx.InputDirectory, checksumPath, ctx.Filters);
        auto changes = collector.CollectChangedHeaders();
        for (auto& change : changes) {
            if (change.Type == ChangeType::AddedOrChanged) {
                if (!Utils::HasSymbolGenerationMarker(ctx.InputDirectory / change.Path))
					continue;
                includes.push_back(change.Path);
            }
            else if (change.Type == ChangeType::Removed) {
                fs::path symbolPath = symbolsPath / (change.Path.generic_string() + ".symbols.json");
                if (fs::exists(symbolPath))
                    fs::remove(symbolPath);
            }
        }
    }, "Collect headers");

    std::vector<const char*> clangArgCStrs;
    for (auto& arg : ctx.ClangArgs)
        clangArgCStrs.push_back(arg.c_str());

	// Create translation unit with all headers
    auto mainFile = ctx.GeneratedDirectory / "generated.cpp";
    Utils::CreateCPPFileFor(mainFile, includes);
    HeaderParser parser(
        mainFile.string(),
        clangArgCStrs,
        CXTranslationUnit_DetailedPreprocessingRecord |
        CXTranslationUnit_SkipFunctionBodies |
        CXTranslationUnit_KeepGoing,
        HeaderParser::ParserContext {
            .InputDirectory = ctx.InputDirectory
        },
        ctx.Filters
    );

	// Parse all headers
    Utils::Benchmark([&]() {
        parser.DoStuff();
    }, "Parse all headers");

    parser.PrintTranslationUnitErrors();
    if (parser.HasAnyErrors()) {
        return -1;
    }

    for (auto& [usr, classInfo] : parser.Classes) {
        if (!classInfo.Location || 
            !Utils::IsFileInIncludes(classInfo.Location->FilePath, ctx.InputDirectory, includes) ||
            !classInfo.RawComment.has_value())
            continue;

        auto annotations = CommentProcessor::ProcessComment(*classInfo.RawComment);
        for (auto& annotation : annotations) {
			AnnotationProcessor::Apply(&classInfo, annotation);
		}
    }

    for (auto& [usr, methodInfo] : parser.Methods) {
		if (!methodInfo.Location ||
            !Utils::IsFileInIncludes(methodInfo.Location->FilePath, ctx.InputDirectory, includes) ||
            !methodInfo.RawComment.has_value())
            continue;

        auto annotations = CommentProcessor::ProcessComment(*methodInfo.RawComment);
        for (auto& annotation : annotations) {
            AnnotationProcessor::Apply(&methodInfo, annotation);
        }
    }

	std::vector<ClassInfo*> classes;
	std::vector<MethodInfo*> methods;

	for (auto& [usr, classInfo] : parser.Classes) {
		classes.push_back(&classInfo);
	}

	for (auto& [usr, methodInfo] : parser.Methods) {
		methods.push_back(&methodInfo);
	}

	auto symbolMap = SymbolGenerator::GenerateSymbolData(classes, methods);
    for (auto& [file, symbols] : symbolMap) {
        fs::path relativePath = fs::relative(file, ctx.InputDirectory);
		fs::path symbolFile = symbolsPath / relativePath.parent_path() / (relativePath.stem().string() + ".symbols.json");
	
		json j;
		j["format_version"] = 1;
		j["vtables"] = json::object();
		j["functions"] = json::object();
        for (auto& symbol : symbols) {
            if (symbol.Type == SymbolType::VirtualTableSymbol) {
                j["vtables"][symbol.Name] = {
                    { "address", std::format("0x{:X}", symbol.Address.has_value() ? *symbol.Address : 0x0) }
				};
            }
            else if (symbol.Type == SymbolType::VirtualFunctionSymbol) {
                j["functions"][symbol.Name] = {
                    { "vtable", *symbol.VirtualTableTarget },
                    { "index", *symbol.VirtualIndex }
                };
            }
            else if (symbol.Type == SymbolType::FunctionSymbol) {
                if (symbol.Address.has_value()) {
                    j["functions"][symbol.Name] = {
                        { "address", std::format("0x{:X}", *symbol.Address) }
                    };
                }
                else if (symbol.Signature.has_value()) {
                    j["functions"][symbol.Name] = {
                        { "signature", *symbol.Signature }
                    };
                }
			}
        }
		std::ofstream outFile(symbolFile);
		outFile << j.dump(4) << std::endl;
    }

    return 0;
}