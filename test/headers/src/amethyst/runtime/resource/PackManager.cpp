#include "amethyst/runtime/resource/PackManager.hpp"
#include "amethyst/runtime/AmethystContext.hpp"
#include "minecraft/src-client/common/client/game/MinecraftGame.hpp"
#include "minecraft/src/common/resources/ResourcePackRepository.hpp"
#include <winrt/Windows.Storage.h>
#include <fstream>

Amethyst::PackManager::PackManager(AmethystContext* amethyst) : 
    mAmethyst(amethyst)
{}

Amethyst::PackManager::~PackManager() {}

void Amethyst::PackManager::RegisterNewPack(const Mod::Metadata& metadata, const std::string& path, PackType type)
{
	std::string key = metadata.GetVersionedName();
    fs::path resourcesPath = GetAmethystFolder() / "mods" / metadata.GetVersionedName() / "resource_packs";

	// Check if the mod is on the list of packs, if not add it
	if (!mPacks.contains(key)) {
        mPacks[key] = {};
	}

	// Check if the pack path is already registered for this mod
    if (mPacks[key].contains(path)) {
		Log::Warning("[AmethystRuntime] Pack '{}' of type {} for '{}' is already registered, skipping.", path, static_cast<int>(type), key);
		return;
    }

    auto manifestPath = resourcesPath / path / "manifest.json";

    // Check if the pack contains a manifest.json
    if (!fs::exists(manifestPath)) {
        Log::Warning("[AmethystRuntime] Pack '{}' for '{}' does not contain a manifest.json, skipping.", path, key);
        return;
    }

    // Try to read the manifest.json to ensure it's valid
    std::ifstream manifestFile(manifestPath, std::ios::binary);
    Assert(manifestFile.is_open(), "[AmethystRuntime] Failed to open manifest.json for pack '{}' of '{}'", path, key);
    std::string manifestContents((std::istreambuf_iterator<char>(manifestFile)), std::istreambuf_iterator<char>());

    // Try to parse the manifest.json
    auto manifestJson = json::parse(manifestContents);
    if (!manifestJson.is_object()) {
        Log::Warning("[AmethystRuntime] manifest.json for pack '{}' of '{}' is not a valid json object, skipping.", path, key);
        return;
    }

    // Ensure it has a header object
    auto& header = manifestJson["header"];
    if (!header.is_object()) {
        Log::Warning("[AmethystRuntime] manifest.json for pack '{}' of '{}' does not contain a valid \"header\" object, skipping.", path, key);
        return;
    }

    // Ensure it has a uuid string in the header
    auto& uuid = header["uuid"];
    if (!uuid.is_string()) {
        Log::Warning("[AmethystRuntime] manifest.json for pack '{}' of '{}' does not contain a valid \"uuid\" string in the \"header\" object, skipping.", path, key);
        return;
    }
    
    // Ensure it has a version array in the header
    auto& version = header["version"];
    if (!version.is_array() || version.size() != 3 || !version[0].is_number_unsigned() || !version[1].is_number_unsigned() || !version[2].is_number_unsigned()) {
        Log::Warning("[AmethystRuntime] manifest.json for pack '{}' of '{}' does not contain a valid \"version\" array in the \"header\" object, skipping.", path, key);
        return;
    }

    mce::UUID packUuid = mce::UUID::fromString(uuid.get<std::string>());
    SemVersion semVersion = {version[0].get<uint16_t>(), version[1].get<uint16_t>(), version[2].get<uint16_t>()};

	// Insert the new pack
    mPacks[key].insert({path, Pack{metadata, path, packUuid, semVersion, type}});
    Log::Info("[AmethystRuntime] Registered pack '{}' for '{}'.", path, key);
}

const std::unordered_map<std::string, std::unordered_map<std::string, Amethyst::Pack>>& Amethyst::PackManager::GetPacks() const
{
	return mPacks;
}

void Amethyst::PackManager::AddResourcePacksToStack(const Bedrock::NonOwnerPointer<ResourcePackRepository>& repository, ResourcePackStack& stack)
{
    // Iterate over all registered packs
    for (auto& [nameVer, packs] : mPacks) {
        for (auto& [path, pack] : packs) {
            if (pack.type != PackType::Resources)
                continue;
            // Add the pack to the stack
            // Workaround for lambda capture
            lambda::Pack lambdaPack{repository, stack};
            lambdaPack.addFromUUID({pack.uuid, pack.version});
        }
    }
}

void Amethyst::PackManager::AddBehaviorPacksToStack(const Bedrock::NonOwnerPointer<ResourcePackRepository>& repository, ResourcePackStack& stack)
{
    // Iterate over all registered packs
    for (auto& [nameVer, packs] : mPacks) {
        for (auto& [path, pack] : packs) {
            if (pack.type != PackType::Behavior)
                continue;
            // Add the pack to the stack
            // Workaround for lambda capture
            lambda::Pack lambdaPack{repository, stack};
            lambdaPack.addFromUUID({pack.uuid, pack.version});
        }
    }
}
