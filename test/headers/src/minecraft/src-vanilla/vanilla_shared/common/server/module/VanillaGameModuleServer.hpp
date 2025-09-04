#pragma once

/* 1.21.0.3 - vftable: 0x4EC7070 */
class VanillaGameModuleServer {
public:
	// 1.21.0.3 - 48 89 5C 24 ? 55 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 85 ? ? ? ? 49 8B F9 4D 8B F0 4C 8B BD
	void initializeBehaviorStack(const Experiments&, const gsl::not_null<Bedrock::NonOwnerPointer<IResourcePackRepository>>&, ResourcePackStack&, const BaseGameVersion&, GameModuleClient::ResourceLoadingPhase);
};