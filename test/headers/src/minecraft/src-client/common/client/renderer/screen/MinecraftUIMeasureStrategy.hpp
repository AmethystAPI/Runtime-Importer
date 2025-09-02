#pragma once
#include <minecraft/src-client/common/client/gui/controls/UIMeasureStrategy.hpp>

class UIProfanityContext;

class MinecraftUIMeasureStrategy : public UIMeasureStrategy {
public:
    const UIProfanityContext& mUIProfanityContext;
};