#pragma once
#include "minecraft/src/common/resources/PackSource.hpp"

class DirectoryPackSource : public PackSource {
public:
    Core::Path mPath;
};