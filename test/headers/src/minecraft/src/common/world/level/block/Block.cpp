#include "minecraft/src/common/world/level/block/Block.hpp"
#include "minecraft/src/common/world/level/block/BlockLegacy.hpp"
#include "minecraft/src/common/world/phys/AABB.hpp"

template <typename T>
inline T Block::getState(const BlockState& blockState) const
{
    auto& states = mLegacyBlock->mStates;
    auto result = states.find(blockState.mID);

    if (blockState.mID >= result->first) {
        return (0xFFFF >> (16 - result->second.mNumBits)) & (mData >> (result->second.mEndBit - result->second.mNumBits + 1));
    }

    auto alteredState = mLegacyBlock->_tryLookupAlteredStateCollection(blockState.mID, mData);

    if (alteredState.has_value()) return alteredState.value();
    return 0;
}

template int Block::getState<int>(const BlockState& blockState) const;

template <typename T>
gsl::strict_not_null<const Block*> Block::setState(const BlockState& stateType, T value) const {
    auto& states = mLegacyBlock->mStates;
    auto foundState = states.find(stateType.mID);

    if (stateType.mID >= foundState->first) {
        if (value < (int64_t)foundState->second.mVariationCount) {
            uint64_t maskedData = mData & (~foundState->second.mMask);
            uint64_t index = maskedData | (value << (foundState->second.mEndBit - foundState->second.mNumBits + 1));

            if (index < mLegacyBlock->mBlockPermutations.size()) {
                return gsl::make_not_null(mLegacyBlock->mBlockPermutations[index].get());
            }
        }
        else {
            Log::Info("more than found variation count");
        }

        Assert(false, "Not handling this ong!");
    }

    Assert(false, "Unhandled case in Block::setState");
}

template gsl::strict_not_null<const Block*> Block::setState<int>(const BlockState& stateType, int value) const;

// 1.20.71.1 - Partial reimplementation using `BlockLegacy::getMapColor`
mce::Color Block::getMapColor(BlockSource& region, const BlockPos& pos) const
{
    return this->mLegacyBlock->getMapColor(region, pos, *this);
}

bool Block::canBeBuiltOver(BlockSource& region, const BlockPos& pos) const {
    return mLegacyBlock->canBeBuiltOver(region, pos);
}

bool Block::isFenceBlock() const
{
    return mLegacyBlock->isFenceBlock();
}

bool Block::getCollisionShape(AABB& outAABB, const IConstBlockSource& region, const BlockPos& pos, optional_ref<const GetCollisionShapeInterface> entity) const
{
    outAABB = this->mLegacyBlock->getCollisionShape(*this, region, pos, entity);
    return outAABB.min.x < outAABB.max.x && outAABB.min.y < outAABB.max.y && outAABB.min.z < outAABB.max.z;
}

const Material& Block::getMaterial() const
{
    return mLegacyBlock->mMaterial;
}

bool Block::use(Player& player, const BlockPos& at, FacingID face, std::optional<Vec3> hit) const
{
    using function = decltype(&Block::use);
    static auto func = std::bit_cast<function>(SigScan("40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 85 ? ? ? ? 45 0F B6 E9 44 88 4C 24 ? 4D 8B F0"));
    return (this->*func)(player, at, face, hit);
}

bool Block::isInteractiveBlock() const
{
    using function = decltype(&Block::isInteractiveBlock);
    static auto func = std::bit_cast<function>(SigScan("40 53 48 83 EC ? 48 8B D9 48 8B 49 ? 48 85 C9 74 ? F6 81"));
    return (this->*func)();
}

bool Block::hasProperty(BlockProperty property) const
{
    return mLegacyBlock->hasProperty(property);
}