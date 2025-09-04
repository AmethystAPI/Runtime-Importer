#pragma once
#include "minecraft/src/common/network/NetworkBlockPosition.hpp"
#include "minecraft/src/common/world/Facing.hpp"
#include "minecraft/src/common/world/inventory/transaction/ComplexInventoryTransaction.hpp"
#include "minecraft/src/common/world/level/block/Block.hpp"
#include "minecraft/src/common/world/phys/Vec3.hpp"

class Player;

class ItemUseInventoryTransaction : public ComplexInventoryTransaction {
public:
    enum class ActionType {
        Place,
        Use,
        Destroy
    };

    ActionType mActionType;
    NetworkBlockPosition mPos;
    BlockRuntimeId mTargetBlockId;
    FacingID mFace;
    int32_t mSlot;
    NetworkItemStackDescriptor mItem;
    Vec3 mFromPos;
    Vec3 mClickPos;

public:
    ItemUseInventoryTransaction();

    void setTargetBlock(const Block& block)
    {
        this->mTargetBlockId = block.getRuntimeId();
    }

    /*void setSelectedItem(const ItemStack& stack)
    {
        NetworkItemStackDescriptor networkDescriptor(stack);
    }*/

    void resendPlayerState(Player& player) const {
        using function = decltype(&ItemUseInventoryTransaction::resendPlayerState);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 74 24 ? 48 89 7C 24 ? 55 48 8D 6C 24 ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 45 ? 48 8B DA 48 8B 8A"));
        (this->*func)(player);
    }

    void resendBlocksAroundArea(Player& player, const BlockPos& pos, FacingID facing) const {
        using function = decltype(&ItemUseInventoryTransaction::resendBlocksAroundArea);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 7C 24 ? 55 48 8B EC 48 83 EC ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 45 ? 48 8B CA"));
        (this->*func)(player, pos, facing);
    }
};

static_assert(sizeof(ItemUseInventoryTransaction) == 0x100);