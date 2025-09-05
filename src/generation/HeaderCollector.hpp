#pragma once
#include <unordered_map>
#include <string>

#include "utils/PathUtils.hpp"

enum class ChangeType {
	AddedOrChanged,
	Removed
};

struct ChangeRecord {
	fs::path Path;
	ChangeType Type;

	ChangeRecord(const fs::path& path, ChangeType type)
		: Path(path), Type(type) {}
};

class HeaderCollector {
public:
	fs::path Directory;
	fs::path ChecksumFile;
	std::vector<fs::path> Filters;

	HeaderCollector(const fs::path& directory, const fs::path& checksumFile, const std::vector<fs::path>& filters);
	std::vector<ChangeRecord> CollectChangedHeaders();
	void UpdateChecksums(const std::unordered_map<std::string, uint64_t>& hashes);
};