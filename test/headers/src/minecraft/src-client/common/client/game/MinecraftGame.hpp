#pragma once
#include <memory>
#include <minecraft/src-client/common/client/renderer/TextureGroup.hpp>
#include <minecraft/src-client/common/client/gui/gui/FontHandle.hpp>

// class MinecraftGame : public BedrockEngine::AppIsland, App, IMinecraftGame, LevelListener,
//                             ResourcePackListener, OptionsObserver, ActiveDirectoryIdentityObserver

class MinecraftInputHandler;
class FontRepository;
class ResourcePackRepository;
class ResourcePackManager;

class MinecraftGame {
public:
    /* this + 0    */ uintptr_t** vtable;
    /* this + 8    */ std::byte padding8[352 - 8];
    /* this + 352  */ std::shared_ptr<FontRepository> mFontRepository; 
    /* this + 368  */ std::byte padding368[1920 - 1560];
    /* this + 728  */ ResourcePackManager* mResourcePackManager;
    /* this + 736  */ std::byte padding736[1184];
    /* this + 1920 */ std::shared_ptr<mce::TextureGroup> mTextures;
    /* this + 1936 */ std::byte padding1936[3328 - 2992];
    /* this + 2272 */ std::unique_ptr<ResourcePackRepository> mResourcePackRepository;
    /* this + 2280 */ std::byte padding2296[3328 - 2280];
    /* this + 3328 */ std::unique_ptr<MinecraftInputHandler> mInput;
    /* this + 3336 */ std::byte padding3336[3912 - 3336 - 312];
    /* this + 3608 */ std::byte padding3608[3912 - 3600];
    /* this + 3912 */ FontHandle mFontHandle;
    /* this + 3984 */ FontHandle mRuneFontHandle;
    /* this + 4056 */ FontHandle mUnicodeFontHandle;
    /* this + 4128 */ FontHandle mSmoothFontLatinHandle;
    /* this + 4200 */ FontHandle mUIFontHandle;

    /// @signature { "48 89 5C 24 ? 48 89 7C 24 ? 55 48 8D 6C 24 ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 45 ? 48 8B FA 48 8B D9 0F 57 C0 0F 11 45" }
    void onActiveResourcePacksChanged(ResourcePackManager& resourcePackManager);
};

// 1.21.0.3
static_assert(offsetof(MinecraftGame, mTextures) == 1920);