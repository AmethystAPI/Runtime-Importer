#include "minecraft/src/common/resources/ResourcePackRepository.hpp"
#include "amethyst/Memory.hpp"

void ResourcePackRepository::requestReloadUserPacks() 
{
    using function = decltype(&ResourcePackRepository::requestReloadUserPacks);
    auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 57 48 83 EC ? 48 8B F9 48 8D 99 ? ? ? ? 48 89 5C 24 ? 48 8B CB FF 15 ? ? ? ? 85 C0 74 ? 8B C8 FF 15 ? ? ? ? 90 48 8B 07 48 8B CF 48 8B 80 ? ? ? ? FF 15 ? ? ? ? 84 C0 75 ? C6 87 ? ? ? ? ? EB ? 48 8B CF E8 ? ? ? ? 90"));
    (this->*func)();
}


void ResourcePackRepository::_reloadUserPacks()
{
    using function = decltype(&ResourcePackRepository::_reloadUserPacks);
    auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 6C 24 ? 48 89 74 24 ? 57 41 56 41 57 48 83 EC ? 48 8B 59 ? 48 8B E9 48 3B 59"));
    (this->*func)();
}
