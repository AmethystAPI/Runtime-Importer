#pragma once
#include "minecraft/src-deps/core/headerIncludes/gsl_includes.hpp"
#include "minecraft/src/common/world/level/block/components/BlockComponentStorage.hpp"
#include <minecraft/src/common/world/level/block/BlockState.hpp>
#include <minecraft/src/common/CommonTypes.hpp>
#include <minecraft/src/common/nbt/CompoundTag.hpp>
#include <minecraft/src-deps/core/utility/optional_ref.hpp>
#include <minecraft/src/common/world/Facing.hpp>
#include <optional>
#include <minecraft/src/common/world/phys/Vec3.hpp>
#include <minecraft/src/common/world/level/block/BlockProperty.hpp>

//is_virtual = True
//hide_vtable = False
//struct_size = 208
//
//# (Type, Name, Size (in bytes), Offset (in bytes))
//struct = [
//("gsl::not_null<BlockLegacy*>", "mLegacyBlock", 8, 0x30),
//]

namespace mce {
class Color;
}
class BlockSource;
class BlockPos;
using DataID = unsigned short;

enum class BlockOcclusionType : int32_t {
    Unk1
};

class CachedComponentData {
    Brightness mEmissiveBrightness;
    bool mIsSolid;
    BlockOcclusionType mOcclusionType;
};

class BlockTransformationComponent;
class BlockCollisionBoxComponent;
class BlockSelectionBoxComponent;
class BlockGeometryComponent;
class BlockBakedMaterialDataComponent;
class AABB;
class IConstBlockSource;
class GetCollisionShapeInterface;
class Material;
class Player;

class BlockComponentDirectData {
public:
    enum class LayerBitMask {
        NONE,
        INIT,
        RENDERING
    };

    const BlockTransformationComponent* mBlockTransformationComponent;
    const BlockCollisionBoxComponent* mBlockCollisionBoxComponent;
    const BlockSelectionBoxComponent* mBlockSelectionBoxComponent;
    const BlockGeometryComponent* mBlockGeometryComponent;
    const BlockBakedMaterialDataComponent* mBlockBakedMaterialDataComponent;
    bool mIsFullBlock;
    bool mUseNewTessellation;
    bool mNeedsLegacyTopRotation;
    bool mIsOpaqueFullBlock;
    Brightness mLightEmission;
    Brightness mLight;
    float mExplosionResistance;
    int mBurnOdds;
    int mFlameOdds;
    float mFriction;
    float mDestroySpeed;
    BlockComponentDirectData::LayerBitMask mFinalized;
};

class Block : public BlockComponentStorage {
public:
    /* this + 40  */ const uint16_t mData;
    /* this + 48  */ gsl::not_null<class BlockLegacy*> mLegacyBlock;
    /* this + 56  */ CachedComponentData mCachedComponentData;
    /* this + 64  */ BlockComponentDirectData mDirectData;
    /* this + 136 */ std::vector<HashedString> mTags;
    /* this + 160 */ CompoundTag mSerializationId;
    /* this + 184 */ uint64_t mSerializationIdHash;
    /* this + 192 */ uint32_t mSerializationIdHashForNetwork;
    /* this + 196 */ BlockRuntimeId mNetworkId;
    /* this + 204 */ bool mHasRuntimeId;


public:
    // Circular dependency between this and BlockLegacy because of permutations vector
    // Have to explictly declare each thing..
    template <typename T>
    T getState(const BlockState& blockState) const;

    // Circular dependency between this and BlockLegacy because of permutations vector
    // Have to explictly declare each thing..
    template <typename T>
    gsl::strict_not_null<const Block*> setState(const BlockState& stateType, T value) const;

    // 1.20.71.1 - Partial reimplementation using `BlockLegacy::getMapColor`
    // Needed because `BlockLegacy::getMapColor` is a protected function and the Block class
    // is its friend, how adorable..
    mce::Color getMapColor(BlockSource& region, const BlockPos& pos) const;

    bool canBeBuiltOver(BlockSource& region, const BlockPos&) const;

    BlockRuntimeId getRuntimeId() const {
        return mNetworkId;
    }

    bool isFenceBlock() const;
                                                             
    bool getCollisionShape(AABB& outAABB, const IConstBlockSource& region, const BlockPos& pos, optional_ref<const GetCollisionShapeInterface> entity) const;

    const Material& getMaterial() const;

    bool use(Player& player, const BlockPos& at, FacingID face, std::optional<Vec3> hit) const;

    bool isInteractiveBlock() const;

    bool hasProperty(BlockProperty property) const;
};

static_assert(sizeof(Block) == 208);
static_assert(offsetof(Block, mLegacyBlock) == 48);