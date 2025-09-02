#pragma once
#include <memory>
#include <chrono>
#include "minecraft/src/common/world/level/BlockPos.hpp"
#include "minecraft/src/common/world/phys/Vec3.hpp"
#include "minecraft/src/common/world/Facing.hpp"
#include "minecraft/src/common/world/item/Item.hpp"

class Player;
class IGameModeTimer;
class IGameModeMessenger;
class ItemStack;

class GameMode {
public:
    class BuildContext {
    public:
        bool mHasBuildDirection;
        bool mHasLastBuiltPosition;
        bool mLastBuildBlockWasInteractive;
        bool mLastBuildBlockWasSnappable;
        BlockPos mLastBuiltBlockPosition;
        BlockPos mBuildDirection;
        BlockPos mNextBuildPos;
        unsigned char mContinueFacing;
    };

    Player& mPlayer;
    BlockPos mDestroyBlockPos;
    uint8_t mDestroyBlockFace;
    float mOldDestroyProgress;
    float mDestroyProgress;
    long double mLastDestroyTime;
    float mDistanceTravelled;
    Vec3 mPlayerLastPosition;
    GameMode::BuildContext mBuildContext;
    float mMinPlayerSpeed;
    int mContinueBreakBlockCount;

    std::chrono::steady_clock::time_point mLastBuildTime;
    std::chrono::steady_clock::time_point mNoDestroyUntil;
    std::chrono::steady_clock::time_point mNoDestroySoundUntil;

    std::chrono::milliseconds creativeDestructionTickDelay;
    std::chrono::milliseconds buildingTickDelay;
    std::chrono::milliseconds destroySoundDelay;

    std::unique_ptr<IGameModeTimer> mTimer;
    std::unique_ptr<IGameModeMessenger> mMessenger;

    BlockPos _calculatePlacePos(ItemStack& heldStack, const BlockPos& pos, FacingID& face);

    virtual ~GameMode() = default;
    virtual void ukn1();
    virtual void ukn2();
    virtual void ukn3();
    virtual void ukn4();
    virtual void ukn5();
    virtual void ukn6();
    virtual void ukn7();
    virtual void ukn8();
    virtual void ukn9();
    virtual void ukn10();
    virtual void ukn11();
    virtual InteractionResult useItemOn(ItemStack& item, const BlockPos& pos, FacingID face, const Vec3& clickPos, const Block* block);

    __declspec(noinline) InteractionResult _sendUseItemOnEvents(ItemStack& item, const BlockPos& pos, FacingID face, const Vec3& clickPos) const
    {
        using function = decltype(&GameMode::_sendUseItemOnEvents);
        static auto func = std::bit_cast<function>(SigScan("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 85 ? ? ? ? 4D 8B F9 4D 8B E0 48 8B F2 4C 8B F1 4C 8B AD"));
        return (this->*func)(item, pos, face, clickPos);
    }

    void _sendPlayerInteractWithBlockAfterEvent(Player& player, const BlockPos& pos, FacingID face, const Vec3& at);

    bool _canUseBlock(const Block& block) const;

    float getMaxPickRange() const;

    bool baseUseItem(ItemStack& stack);

    bool isLastBuildBlockInteractive() const {
        return mBuildContext.mLastBuildBlockWasInteractive;
    }
};