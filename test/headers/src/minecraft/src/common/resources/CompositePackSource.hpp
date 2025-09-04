#pragma once
#include <vector>
#include "minecraft/src/common/resources/PackSource.hpp"

class CompositePackSource : public PackSource {
public:
    std::vector<PackSource*> mPackSources;
};