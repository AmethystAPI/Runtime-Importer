#pragma once
#include <format>
#include "minecraft/src/common/world/item/ItemStackBase.hpp"
#include "minecraft/src/common/world/inventory/network/ItemStackNetIdVariant.hpp"
#include "minecraft/src/common/world/Facing.hpp"


class InteractionResult;
class Actor;
class Vec3;
class BlockPalette;
class NetworkItemStackDescriptor;

class ItemStack : public ItemStackBase {
public:
    static ItemStack& EMPTY_ITEM;
    
public:
    ItemStackNetIdVariant mNetIdVariant;

public:
    ItemStack();
    ItemStack(const ItemStack& other) : ItemStackBase(other)
    {
        mNetIdVariant = other.mNetIdVariant;
    }

    InteractionResult useOn(Actor&, int, int, int, FacingID, const Vec3&);

    static ItemStack fromDescriptor(const NetworkItemStackDescriptor& descriptor, BlockPalette& palette, bool isClientSide );

    int getMaxUseDuration() const;
};

static_assert(sizeof(ItemStack) == 0xA0);