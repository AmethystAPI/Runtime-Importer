#pragma once
#include <minecraft/src-deps/core/string/StringHash.hpp>
#include <minecraft/src/common/AppPlatformListener.hpp>
#include <minecraft/src-deps/renderer/RenderMaterialGroupBase.hpp>

namespace mce {
    class RenderMaterialInfo;
    class RenderMaterialGroup : public AppPlatformListener, public mce::RenderMaterialGroupBase {
    public:
        static RenderMaterialGroup* common;
    };
};