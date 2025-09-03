#pragma once
#include <cstddef>
#include <fstream>
#include "PathUtils.hpp"
#include "xxhash/xxhash.h"

class Utils {
public:
	static uint64_t GetHashForFile(const fs::path& path) {
        std::ifstream file(path, std::ios::binary);
        XXH64_state_t* state = XXH64_createState();
        XXH64_reset(state, 0);

        std::vector<char> buffer(4096);
        while (file.read(buffer.data(), buffer.size()) || file.gcount() > 0)
            XXH64_update(state, buffer.data(), file.gcount());

        uint64_t hash = XXH64_digest(state);
        XXH64_freeState(state);
        return hash;
	}
};