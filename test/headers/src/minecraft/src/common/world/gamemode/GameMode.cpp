#include "minecraft/src/common/world/gamemode/GameMode.hpp"
#include "minecraft/src/common/world/level/BlockSource.hpp"
#include "minecraft/src/common/world/level/block/Block.hpp"
#include "minecraft/src/common/world/actor/player/Player.hpp"
#include "minecraft/src/common/world/item/Item.hpp"

// Complete implementation
// 1.21.0.3 - no address, inlined.
BlockPos GameMode::_calculatePlacePos(ItemStack& heldStack, const BlockPos& pos, FacingID& face) {
    const Item* item = heldStack.getItem();

    if (item) {
        BlockPos result = pos;
        item->calculatePlacePos(heldStack, mPlayer, face, result);
        return result;
    }

    BlockSource& blockSource = mPlayer.getDimensionBlockSource();
    const Block& block = blockSource.getBlock(pos);

    if (block.canBeBuiltOver(blockSource, pos)) {
        face = Facing::Name::UP;
        return pos;
    }
    
    return pos.neighbor(face);
}

void GameMode::_sendPlayerInteractWithBlockAfterEvent(Player& player, const BlockPos& at, FacingID face, const Vec3& hit)
{
    using function = decltype(&GameMode::_sendPlayerInteractWithBlockAfterEvent);
    static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 74 24 ? 55 57 41 56 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 85 ? ? ? ? 41 0F B6 F1 49 8B D8 4C 8B F2 48 8B BD"));
    (this->*func)(player, at, face, hit);
}

bool GameMode::_canUseBlock(const Block& block) const
{
    return mPlayer.canUseOperatorBlocks() || !block.hasProperty(BlockProperty::OperatorBlock);
}

float GameMode::getMaxPickRange() const
{
    if (this->mPlayer.isCreative()) return 12.0f;
    return 7.0f;
}

bool GameMode::baseUseItem(ItemStack& stack)
{
    using function = decltype(&GameMode::baseUseItem);
    static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 74 24 ? 55 57 41 56 48 8D 6C 24 ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 45 ? 48 8B F2 48 8B F9 45 33 F6 4C 89 75 ? 48 8D 4D ? E8 ? ? ? ? 90 48 8B 5F"));
    return (this->*func)(stack);
}
