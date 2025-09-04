#include "Mob.hpp"
#include <amethyst/Memory.hpp>

// void Mob::setSprinting(bool isSprinting)
// {
//     using function = decltype(&Mob::setSprinting);
//     auto func = std::bit_cast<function>(this->vtable[153]);
//     return (this->*func)(isSprinting);
// }

// void Mob::swing() {
//     using function = decltype(&Mob::swing);
//     auto func = std::bit_cast<function>(this->vtable[117]);
//     return (this->*func)();
// }

int Mob::getCurrentSwingDuration() const
{
    using function = decltype(&Mob::getCurrentSwingDuration);
    auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 57 48 83 EC ? 48 8B 15 ? ? ? ? 48 8B F9"));
    return (this->*func)();
}
