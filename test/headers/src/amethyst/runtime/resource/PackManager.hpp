#pragma once
#include "minecraft/src-deps/core/utility/NonOwnerPointer.hpp"
#include "minecraft/src-deps/core/resource/ResourceHelper.hpp"
#include "amethyst/runtime/mod/Mod.hpp"
#include "amethyst/Memory.hpp"

class AmethystContext;
class ResourcePackStack;
class ResourcePackRepository;

namespace lambda {
// The lambda capture structure used in initializeResourceStack
struct Pack {
    Bedrock::NonOwnerPointer<ResourcePackRepository> repo;
    ResourcePackStack& stack;

    void addFromUUID(const PackIdVersion& version)
    {
        using function = decltype(&lambda::Pack::addFromUUID);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 55 56 57 48 81 EC ? ? ? ? 48 8B D9 48 8B 09"));
        (this->*func)(version);
    }
};
} // namespace lambda

namespace Amethyst {
struct Pack {
    Mod::Metadata metadata;
    std::string path;
    mce::UUID uuid;
    SemVersion version;
    PackType type;
};

class PackManager {
public:
    PackManager(AmethystContext* amethyst);
    ~PackManager();

    void RegisterNewPack(const Mod::Metadata& metadata, const std::string& path, PackType type);
    const std::unordered_map<std::string, std::unordered_map<std::string, Pack>>& GetPacks() const;
    void AddResourcePacksToStack(const Bedrock::NonOwnerPointer<ResourcePackRepository>& repository, ResourcePackStack& stack);
    void AddBehaviorPacksToStack(const Bedrock::NonOwnerPointer<ResourcePackRepository>& repository, ResourcePackStack& stack);

private:
    AmethystContext* mAmethyst;
    friend class AmethystContext;

private:
    std::unordered_map<std::string, std::unordered_map<std::string, Pack>> mPacks;
};

}