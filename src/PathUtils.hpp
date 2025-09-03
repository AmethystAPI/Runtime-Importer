#pragma once
#include <filesystem>

namespace fs = std::filesystem;
class PathUtils {
public:
	static bool StartsWith(const fs::path& path, const fs::path& prefix) {
		auto rel = path.lexically_relative(prefix);
		return !rel.empty() && rel.native().substr(0, 2) != L"..";
	}

	static bool CheapIsFrom(const fs::path& path, const fs::path& directory) {
		return path.string().rfind(directory.string(), 0) == 0;
	}
};