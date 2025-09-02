#pragma once
#include <amethyst/Memory.hpp>
#include <functional>
#include <minecraft/src/common/world/events/EventResult.hpp>
class Player;
class BlockPos;

class BlockEventCoordinator {
public:
    void sendBlockInteractedWith(Player& player, const BlockPos& pos) {
        // inlined on client
        // "protected: void __cdecl EventCoordinatorPimpl<class ScriptingEventListener>::processEvent(class std::function<enum EventResult __cdecl(class ScriptingEventListener & __ptr64)>) __ptr64"

        using function = void (BlockEventCoordinator::*)(std::function<EventResult(class PlayerEventListener&)>);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 44 24 ? 4C 8B EA 48 8B F1"));
        
        (this->*func)([this, &player, &pos](class PlayerEventListener& listener) -> EventResult {
            // dont ask me what the fuck this does, i just manually reimplemented a lambda call
            // i tried to figure out what it was calling but i dont have the slightest idea

            using innerFunc = EventResult(PlayerEventListener::*)(Player*, const BlockPos&);
            uintptr_t* vtable = *(uintptr_t**)&listener;

            static auto inner = std::bit_cast<innerFunc>(vtable[7]);
            return (listener.*inner)(&player, pos);
        });
    }
};