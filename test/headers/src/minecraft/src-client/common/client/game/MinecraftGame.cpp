#include "MinecraftGame.hpp"
#include "amethyst/Memory.hpp"

void MinecraftGame::onActiveResourcePacksChanged(ResourcePackManager& resourcePackManager)
{
    using function = decltype(&MinecraftGame::onActiveResourcePacksChanged);
    auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 7C 24 ? 55 48 8D 6C 24 ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 45 ? 48 8B FA 48 8B D9 0F 57 C0 0F 11 45"));
    (this->*func)(resourcePackManager);
}
