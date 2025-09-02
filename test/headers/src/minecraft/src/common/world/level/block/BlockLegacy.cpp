#include "minecraft/src/common/world/level/block/BlockLegacy.hpp"


BlockLegacy::~BlockLegacy() {

}

// 1.21.0.3 - Re-Implemented
bool BlockLegacy::isContainerBlock() const
{
    return false;
}

// 1.21.0.3 - Re-Implemented
bool BlockLegacy::isSignalSource() const
{
    return false;
}

// 1.21.0.3 - Re-Implemented
bool BlockLegacy::detachesOnPistonMove(BlockSource&, BlockPos const&) const
{
    return false;
}

// 1.21.0.3 - Re-Implemented
bool BlockLegacy::hasComparatorSignal() const
{
    return false;
}

// 1.21.0.3 - Re-Implemented
bool BlockLegacy::isInteractive() const
{
    return false;
}

// 1.20.71.1 - Re-Implemented
short BlockLegacy::getBlockItemId()
{
    if (mID <= 0xFF) return mID;
    return 255 - mID;
}

void BlockLegacy::setDestroyTime(float destroyTime, float explosionResistance)
{
    mDestroySpeed = destroyTime;
    mExplosionResistance = explosionResistance;
}

void BlockLegacy::addState(const BlockState& blockState)
{
    using function = decltype(&BlockLegacy::addState);
    static auto func = std::bit_cast<function>(SigScan("4C 8B 42 ? E9")); // why is this signature so small lol
    (this->*func)(blockState);
}

bool BlockLegacy::isAir() const
{
    return mNameInfo.mFullName == HashedString("minecraft:air");
}

bool BlockLegacy::hasProperty(BlockProperty prop) const
{
    return (mProperties & (uint64_t)prop) != 0;
}

bool BlockLegacy::isSnappableBlock() const
{
    if (hasProperty(BlockProperty::CubeShaped)) return true;
    if (hasProperty(BlockProperty::Stair)) return true;
    if (hasProperty(BlockProperty::HalfSlab)) return true;
    if (isFenceBlock()) return true;
    if (isFenceGateBlock()) return true;
    if (isWallBlock()) return true;

    return false;
}


std::optional<int> BlockLegacy::_tryLookupAlteredStateCollection(uint64_t stateId, uint16_t blockData)
{
    // Re-implemented 1.20.71.1
    for (auto& item : mAlteredStateCollections) {
        if (item->mBlockState.get().mID != stateId) continue;
        return item->getState(*this, blockData);
    }

    return std::nullopt;
}
