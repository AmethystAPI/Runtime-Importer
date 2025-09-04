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

	static std::string MakeRelative(const fs::path& bbase, const fs::path& bfull) {
		fs::path base = fs::absolute(bbase).lexically_normal();
		fs::path full = fs::absolute(bfull).lexically_normal();
		auto baseStr = base.generic_string();
		auto fullStr = full.generic_string();

		if (fullStr.compare(0, baseStr.size(), baseStr) == 0 &&
			(fullStr.size() == baseStr.size() || fullStr[baseStr.size()] == '/')) {
			return fullStr.substr(baseStr.size() + 1);
		}
		return fullStr;
	}
};