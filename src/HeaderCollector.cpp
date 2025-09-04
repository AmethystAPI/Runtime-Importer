#include "HeaderCollector.hpp"
#include <unordered_map>
#include <fstream>
#include <Json.hpp>
#include "Utils.hpp"

using namespace nlohmann;

HeaderCollector::HeaderCollector(const fs::path& directory, const fs::path& checksumFile, const std::vector<fs::path>& filters) :
	Directory(directory),
	ChecksumFile(checksumFile),
	Filters(filters)
{
}

std::vector<ChangeRecord> HeaderCollector::CollectChangedHeaders()
{
	std::vector<ChangeRecord> changes;
    std::unordered_map<std::string, uint64_t> oldHashes;
    if (fs::exists(ChecksumFile))
    {
        std::ifstream in(ChecksumFile);
        json j;
        in >> j;
        for (auto& [key, val] : j.items())
            oldHashes[key] = val.get<uint64_t>();
    }

    std::vector<fs::path> headers;
    if (!fs::exists(Directory))
        return {};
    for (auto& p : fs::recursive_directory_iterator(Directory)) {
        bool isOnFilter = Filters.empty();
        for (auto& filter : Filters) {
            if (PathUtils::CheapIsFrom(fs::relative(p.path(), Directory).generic_string(), filter.generic_string())) {
                isOnFilter = true;
                break;
            }
		}
        if (!isOnFilter)
			continue;
        if (p.path().extension() == ".h" || p.path().extension() == ".hpp")
            headers.push_back(fs::relative(p.path(), Directory).generic_string());
    }

    std::unordered_map<std::string, uint64_t> newHashes;
    for (auto& header : headers)
    {
		auto key = header.generic_string();
        uint64_t headerHash = Utils::GetHashForFile(Directory / header);
        newHashes[key] = headerHash;
        if (!oldHashes.count(key) || oldHashes[key] != headerHash)
        {
			changes.emplace_back(header, ChangeType::AddedOrChanged);
        }
    }

    for (auto& [oldPath, _] : oldHashes) {
        if (std::find(headers.begin(), headers.end(), oldPath) == headers.end()) {
            changes.emplace_back(oldPath, ChangeType::Removed);
        }
    }

	UpdateChecksums(newHashes);
	return changes;
}

void HeaderCollector::UpdateChecksums(const std::unordered_map<std::string, uint64_t>& hashes)
{
    json out;
    for (auto& [path, h] : hashes)
        out[path] = h;

    std::ofstream o(ChecksumFile);
    o << out.dump(4);
}
