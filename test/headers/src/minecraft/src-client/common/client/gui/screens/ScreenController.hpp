#pragma once
#include "minecraft/src-deps/core/string/StringHash.hpp"

class ScreenController {
public:
    // 1.21.0.3 - 4C 8B DC 49 89 5B ? 49 89 73 ? 57 48 81 EC ? ? ? ? 49 8B D8 48 8B FA 48 8D B1 ? ? ? ? 48 8D 44 24 ? 49 89 43 ? 49 C7 43 ? ? ? ? ? 49 8B 49 ? 48 85 C9 74 ? 48 8B 01 48 8D 54 24 ? 48 8B 00 FF 15 ? ? ? ? 48 89 44 24 ? 48 8D 44 24 ? 48 89 84 24 ? ? ? ? 48 C7 84 24 ? ? ? ? ? ? ? ? 48 8B 4B ? 48 85 C9 74 ? 48 8B 01 48 8D 54 24 ? 48 8B 00 FF 15 ? ? ? ? 48 89 84 24 ? ? ? ? 8B 07 89 84 24 ? ? ? ? 4C 8D 4C 24 ? 4C 8D 84 24 ? ? ? ? 48 8D 94 24 ? ? ? ? 48 8B CE E8 ? ? ? ? 90 48 8B 8C 24 ? ? ? ? 48 85 C9 74 ? 48 8B 01 48 8D 54 24 ? 48 3B CA 0F 95 C2 48 8B 40 ? FF 15 ? ? ? ? 48 C7 84 24 ? ? ? ? ? ? ? ? 48 8B 4C 24 ? 48 85 C9 74 ? 48 8B 01 48 8D 54 24 ? 48 3B CA 0F 95 C2 48 8B 40 ? FF 15 ? ? ? ? 4C 8D 9C 24 ? ? ? ? 49 8B 5B ? 49 8B 73 ? 49 8B E3 5F C3 CC CC CC CC CC CC 48 89 5C 24 ? 48 89 74 24 ? 57 48 83 EC ? 49 8B F0 48 8B DA 48 8B F9 48 8B 49 ? 48 85 C9 75
    void bindString(const StringHash& bindingName, const std::function<std::string()>& callback, const std::function<bool()>& condition);
};