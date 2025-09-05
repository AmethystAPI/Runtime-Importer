#pragma once
#include <filesystem>
namespace fs = std::filesystem;

struct CursorLocation {
	fs::path FilePath;
	unsigned int Line;
	unsigned int Column;
};