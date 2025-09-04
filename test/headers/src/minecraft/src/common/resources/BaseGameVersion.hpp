#pragma once
#include "minecraft/src-deps/core/semVer/SemVersion.hpp"

class __declspec(align(8)) BaseGameVersion {
public:
    SemVersion mSemVersion;
    bool mNeverCompatible;
};