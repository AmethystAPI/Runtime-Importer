#include <iostream>
#include "clang-c/Index.h"
#include "CLI11.hpp"

int main(int argc, char** argv) {
	CLI::App app{ "Amethyst Symbol Generator v0.0.1" };
	std::string inputDirectory;
	std::string generatedDirectory;

	// Add options
	app.add_option("--input-directory", inputDirectory, "The input directory to look for header files.");
	app.add_option("--generated-directory", generatedDirectory, "The output directory to write generated files to.");
	
	std::vector<std::string> clangArgs;
	app.allow_extras();
	app.set_help_all_flag("--help-all", "Expand all help");

	// Parse command line arguments
	CLI11_PARSE(app, argc, argv);

	// Collect remaining arguments as clang args
	clangArgs = app.remaining();

	std::cout << "Inout Directory: " << inputDirectory << "\n";
	std::cout << "Generated Directory: " << generatedDirectory << "\n";
	std::cout << "Clang args:\n";
	for (auto& arg : clangArgs) {
		std::cout << "  " << arg << "\n";
	}
	std::cin.get();
}