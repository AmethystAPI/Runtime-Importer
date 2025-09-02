#pragma once
#include <gsl/gsl>
#include <vector>
#include <memory>
#include <string>
#include <optional>

#include "minecraft/src/common/world/entity/EntityContext.hpp"
#include <minecraft/src/common/gamerefs/WeakRef.hpp>
#include <minecraft/src/common/world/actor/Actor.hpp>
#include <minecraft/src/common/world/phys/Vec3.hpp>
#include <minecraft/src-deps/core/math/Color.hpp>
#include <minecraft/src/common/ActorUniqueID.hpp>
#include <minecraft/src/common/world/phys/AABB.hpp>
#include <minecraft/src/common/world/level/material/MaterialType.hpp>
#include <minecraft/src/common/world/level/BlockPos.hpp>
#include <minecraft/src/common/world/level/dimension/Dimension.hpp>
#include <minecraft/src-deps/core/utility/AutomaticID.hpp>
#include <minecraft/src/common/world/item/Item.hpp>
#include <minecraft/src-deps/shared_types/legacy/LevelSoundEvent.hpp>
#include <minecraft/src/common/world/actor/ActorFlags.hpp>
#include <minecraft/src/common/world/actor/ActorType.hpp>
#include <minecraft/src/common/world/actor/player/Abilities.hpp>
#include <minecraft/src-deps/input/InputMode.hpp>

enum class ActorInitializationMethod : __int8 {
    INVALID = 0x0,
    LOADED = 0x1,
    SPAWNED = 0x2,
    BORN = 0x3,
    TRANSFORMED = 0x4,
    UPDATED = 0x5,
    EVENT = 0x6,
    LEGACY = 0x7,
};

class AddActorBasePacket;

// Auto-generated: Unknown complete types
enum NewInteractionModel {};
enum ActorDamageCause {};

namespace Puv {
    namespace Legacy {
        enum EquipmentSlot {};
    } // namespace Legacy
} // namespace Puv

enum ArmorSlot {};
enum HandSlot {};
enum ArmorMaterialType {};

// Auto-generated: Forward declarations
class AABBShapeComponent;
class HashedString;
class VariantParameterList;
class ActorDamageSource;
class Actor;
class IConstBlockSource;
class Block;
class ItemStack;
class CompoundTag;
class DataLoadHelper;
struct PlayerMovementSettings;
class MobEffectInstance;
class AnimationComponent;
class Player;
class ItemStackBase;
class ActorInteraction;
class EntityContext;
class DefaultDataLoadHelper;
class ActorWalkAnimationComponent;
class StateVectorComponent;
class ActorRotationComponent;
class Vec2;
class Vec3;

struct BuiltInActorComponents {
    gsl::not_null<StateVectorComponent*> mStateVectorComponent;
    gsl::not_null<AABBShapeComponent*> mAABBShapeComponent;
    gsl::not_null<ActorRotationComponent*> mActorRotationComponent;
    gsl::not_null<ActorWalkAnimationComponent*> mWalkAnimationComponent;
};

#pragma pack(push, 1)
class Actor {
public:
    /* this + 8   */ EntityContext mEntityContext;
    /* this + 32  */ ActorInitializationMethod mInitMethod;
    /* this + 33  */ std::byte padding33[576 - 33];
    /* this + 576 */ std::weak_ptr<Dimension> mDimension; // moved -16 in 1.21
    /* this + 592 */ ILevel* mLevel;
    /* this + 600 */ std::byte padding592[56];
    /* this + 656 */ BuiltInActorComponents mBuiltInComponents; // 1.21
    /* this + 688 */ std::byte padding688[408];

public:
    Vec3* getPosition() const;
    Vec2* getHeadRot() const;
    void moveTo(const Vec3&, const Vec2&);
    float distanceTo(const Vec3& other) const;

    const Dimension& getDimensionConst() const;
    const BlockSource& getDimensionBlockSourceConst() const;
    BlockSource& getDimensionBlockSource() const;
    bool hasDimension() const;
    void setDimension(WeakRef<Dimension> dimension);

    ILevel* getLevel() const
    {
        return mLevel;
    }

    // Generics
    template <typename T>
    const T* tryGetComponent() const
    {
        const auto& registry = mEntityContext.getRegistry();
        return registry.try_get<T>(mEntityContext.mEntity);
    }

    template <typename T>
    T* tryGetComponent()
    {
        auto& registry = mEntityContext.getRegistry();
        return registry.try_get<T>(mEntityContext.mEntity);
    }

    template <typename T>
    bool hasComponent() const
    {
        const auto& registry = mEntityContext.getRegistry();
        return registry.all_of<T>(mEntityContext.mEntity);
    }

    template <typename T, typename... Args>
    T& addComponent(Args&&... args)
    {
        auto& registry = mEntityContext.getRegistry();
        Assert(!registry.all_of<T>(mEntityContext.mEntity), "Entity already has component");
        return registry.emplace<T>(mEntityContext.mEntity, std::forward<Args>(args)...);
    }

    //int load(const CompoundTag&, DefaultDataLoadHelper&);
    void reload();

    bool isClientSide() const;
    bool isCreative() const;

    /**@vIndex {0}*/
    virtual bool getStatusFlag(ActorFlags unk0) const;

    /**@vIndex {1}*/
    virtual void setStatusFlag(ActorFlags unk0, bool unk1);

    /**@vIndex {2}*/
    virtual bool hasComponent(const HashedString& unk0) const;

    /**@vIndex {3}*/
    virtual void outOfWorld();

    /**@vIndex {4}*/
    virtual void reloadHardcoded(ActorInitializationMethod unk0, const VariantParameterList& unk1);

    /**@vIndex {5}*/
    virtual void reloadHardcodedClient(ActorInitializationMethod unk0, const VariantParameterList& unk1);

    /**@vIndex {6}*/
    virtual void initializeComponents(ActorInitializationMethod unk0, const VariantParameterList& unk1);

    /**@vIndex {7}*/
    virtual void reloadComponents(ActorInitializationMethod unk0, const VariantParameterList& unk1);

    /**@vIndex {8}*/
    virtual void _serverInitItemStackIds();

    /**@vIndex {9}*/
    virtual void _doInitialMove();

    /**@vIndex {10}*/
    virtual ~Actor();

    /**@vIndex {11}*/
    virtual void _unknown_11(); // resetUserPos

    /**@vIndex {12}*/
    virtual ActorType getOwnerEntityType();

    /**@vIndex {13}*/
    virtual void remove();

    /**@vIndex {14}*/
    virtual Vec3 getFiringPos() const;

    /**@vIndex {15}*/
    virtual float getInterpolatedBodyRot(float unk0) const;

    /**@vIndex {16}*/
    virtual float getInterpolatedHeadRot(float unk0) const;

    /**@vIndex {17}*/
    virtual float getInterpolatedBodyYaw(float unk0) const;

    /**@vIndex {18}*/
    virtual float getYawSpeedInDegreesPerSecond() const;

    /**@vIndex {19}*/
    virtual Vec3 getInterpolatedRidingOffset(float unk0, int unk1) const;

    /**@vIndex {20}*/
    virtual bool isFireImmune() const;

    /**@vIndex {21}*/
    virtual void blockedByShield(const ActorDamageSource& unk0, Actor& unk1);

    /**@vIndex {22}*/
    virtual bool canDisableShield();

    /**@vIndex {23}*/
    virtual void teleportTo(const Vec3& unk0, bool unk1, int unk2, int unk3, bool unk4);

    /**@vIndex {24}*/
    virtual void lerpMotion(const Vec3& unk0);

    /**@vIndex {25}*/
    virtual std::unique_ptr<AddActorBasePacket> tryCreateAddActorPacket();

    /**@vIndex {26}*/
    virtual void normalTick();

    /**@vIndex {27}*/
    virtual void baseTick();

    /**@vIndex {28}*/
    virtual void passengerTick();

    /**@vIndex {29}*/
    virtual bool startRiding(Actor& unk0);

    /**@vIndex {30}*/
    virtual void addPassenger(Actor& unk0);

    /**@vIndex {31}*/
    virtual std::string getExitTip(const std::string& unk0, InputMode unk1, NewInteractionModel unk2) const;

    /**@vIndex {32}*/
    virtual std::string getEntityLocNameString() const;

    /**@vIndex {33}*/
    virtual bool isInWall() const;

    /**@vIndex {34}*/
    virtual bool isInvisible() const;

    /**@vIndex {35}*/
    virtual bool canShowNameTag() const;

    /**@vIndex {36}*/
    virtual std::string getFormattedNameTag() const;

    /**@vIndex {37}*/
    virtual mce::Color getNameTagTextColor() const;

    /**@vIndex {38}*/
    virtual float getShadowRadius() const;

    /**@vIndex {39}*/
    virtual Vec3 getHeadLookVector(float unk0) const;

    /**@vIndex {40}*/
    virtual void _unknown_40();

    /**@vIndex {41}*/
    virtual float getBrightness(float unk0, const IConstBlockSource& unk1) const;

    /**@vIndex {42}*/
    virtual void playerTouch(Player& unk0);

    /**@vIndex {43}*/
    virtual bool isImmobile() const;

    /**@vIndex {44}*/
    virtual void _unknown_44();

    /**@vIndex {45}*/
    virtual bool isSleeping() const;

    /**@vIndex {46}*/
    virtual void setSleeping(bool unk0);

    /**@vIndex {47}*/
    virtual void setSneaking(bool unk0);

    /**@vIndex {48}*/
    virtual bool isBlocking() const;

    /**@vIndex {49}*/
    virtual void _unknown_49();

    /**@vIndex {50}*/
    virtual bool isAlive() const;

    /**@vIndex {51}*/
    virtual bool isOnFire() const;

    /**@vIndex {52}*/
    virtual bool isSurfaceMob() const;

    /**@vIndex {53}*/
    virtual void _unknown_53();

    /**@vIndex {54}*/
    virtual bool canAttack(Actor* unk0, bool unk1) const;

    /**@vIndex {55}*/
    virtual void setTarget(Actor* unk0);

    /**@vIndex {56}*/
    virtual void _unknown_56();

    /**@vIndex {57}*/
    virtual bool attack(Actor& unk0, const ActorDamageCause& unk1);

    /**@vIndex {58}*/
    virtual void performRangedAttack(Actor& unk0, float unk1);

    /**@vIndex {59}*/
    virtual void setOwner(ActorUniqueID unk0);

    /**@vIndex {60}*/
    virtual void setSitting(bool unk0);

    /**@vIndex {61}*/
    virtual void _unknown_61();

    /**@vIndex {62}*/
    virtual void _unknown_62();

    /**@vIndex {63}*/
    virtual void setStanding(bool unk0);

    /**@vIndex {64}*/
    virtual bool canPowerJump() const;

    /**@vIndex {65}*/
    virtual bool isEnchanted() const;

    /**@vIndex {66}*/
    virtual bool shouldRender() const;

    /**@vIndex {67}*/
    virtual void playAmbientSound();

    /**@vIndex {68}*/
    virtual Puv::Legacy::LevelSoundEvent getAmbientSound() const;

    /**@vIndex {69}*/
    virtual bool isInvulnerableTo(const ActorDamageSource& unk0) const;

    /**@vIndex {70}*/
    virtual ActorDamageCause getBlockDamageCause(const Block& unk0) const;

    /**@vIndex {71}*/
    virtual bool doFireHurt(int unk0);

    /**@vIndex {72}*/
    virtual void onLightningHit();

    /**@vIndex {73}*/
    virtual void feed(int unk0);

    /**@vIndex {74}*/
    virtual void handleEntityEvent(ActorEvent unk0, int unk1);

    /**@vIndex {75}*/
    virtual const HashedString& getActorRendererId() const;

    /**@vIndex {76}*/
    virtual void despawn();

    /**@vIndex {77}*/
    virtual void setArmor(ArmorSlot unk0, const ItemStack& unk1);

    /**@vIndex {78}*/
    virtual ArmorMaterialType getArmorMaterialTypeInSlot(ArmorSlot unk0) const;

    /**@vIndex {79}*/
    virtual int getArmorTextureIndexInSlot(ArmorSlot unk0) const;

    /**@vIndex {80}*/
    virtual float getArmorColorInSlot(ArmorSlot unk0, int unk1) const;

    /**@vIndex {81}*/
    virtual void setEquippedSlot(Puv::Legacy::EquipmentSlot unk0, const ItemStack& unk1);

    /**@vIndex {82}*/
    virtual void setCarriedItem(const ItemStack& unk0);

    /**@vIndex {83}*/
    virtual const ItemStack& getCarriedItem() const;

    /**@vIndex {84}*/
    virtual void setOffhandSlot(const ItemStack& unk0);

    /**@vIndex {85}*/
    virtual const ItemStack& getEquippedTotem() const;

    /**@vIndex {86}*/
    virtual bool consumeTotem();

    /**@vIndex {87}*/
    virtual bool save(CompoundTag& unk0) const;

    /**@vIndex {88}*/
    virtual bool load(const CompoundTag& unk0, DataLoadHelper& unk1);

    /**@vIndex {89}*/
    virtual const HashedString& queryEntityRenderer() const;

    /**@vIndex {90}*/
    virtual ActorUniqueID getSourceUniqueID() const;

    /**@vIndex {91}*/
    virtual bool canFreeze() const;

    /**@vIndex {92}*/
    virtual AABB getLiquidAABB(MaterialType unk0) const;

    /**@vIndex {93}*/
    virtual void handleInsidePortal(const BlockPos& unk0);

    /**@vIndex {94}*/
    virtual bool canChangeDimensionsUsingPortal() const;

    /**@vIndex {95}*/
    virtual void _unknown_95();

    /**@vIndex {96}*/
    virtual void changeDimension(AutomaticID<Dimension, int> unk0);

    /**@vIndex {97}*/
    virtual ActorUniqueID getControllingPlayer() const;

    /**@vIndex {98}*/
    virtual float causeFallDamageToActor(float unk0, float unk1, ActorDamageSource unk2);

    /**@vIndex {99}*/
    virtual void onSynchedDataUpdate(int unk0);

    /**@vIndex {100}*/
    virtual bool canAddPassenger(Actor& unk0) const;

    /**@vIndex {101}*/
    virtual void _unknown_101();

    /**@vIndex {102}*/
    virtual bool canBePulledIntoVehicle() const;

    /**@vIndex {103}*/
    virtual bool inCaravan() const;

    /**@vIndex {104}*/
    virtual void sendMotionPacketIfNeeded(const PlayerMovementSettings& unk0);

    /**@vIndex {105}*/
    virtual bool canSynchronizeNewEntity() const;

    /**@vIndex {106}*/
    virtual void startSwimming();

    /**@vIndex {107}*/
    virtual void stopSwimming();

    /**@vIndex {108}*/
    virtual void buildDebugInfo(std::string& unk0) const;

    /**@vIndex {109}*/
    virtual CommandPermissionLevel getCommandPermissionLevel() const;

    /**@vIndex {110}*/
    virtual int getDeathTime() const;

    /**@vIndex {111}*/
    virtual bool canBeAffected(unsigned int unk0) const;

    /**@vIndex {112}*/
    virtual bool canBeAffectedByArrow(const MobEffectInstance& unk0) const;

    /**@vIndex {113}*/
    virtual void onEffectRemoved(MobEffectInstance& unk0);

    /**@vIndex {114}*/
    virtual bool canObstructSpawningAndBlockPlacement() const;

    /**@vIndex {115}*/
    virtual AnimationComponent& getAnimationComponent();

    /**@vIndex {116}*/
    virtual void openContainerComponent(Player& unk0);

    /**@vIndex {117}*/
    virtual void swing();

    /**@vIndex {118}*/
    virtual void useItem(ItemStackBase& unk0, ItemUseMethod unk1, bool unk2);

    /**@vIndex {119}*/
    virtual void getDebugText(std::vector<std::string>& unk0);

    /**@vIndex {120}*/
    virtual float getMapDecorationRotation() const;

    /**@vIndex {121}*/
    virtual float getPassengerYRotation(const Actor& unk0) const;

    /**@vIndex {122}*/
    virtual bool add(ItemStack& unk0);

    /**@vIndex {123}*/
    virtual bool drop(const ItemStack& unk0, bool unk1);

    /**@vIndex {124}*/
    virtual bool getInteraction(Player& unk0, ActorInteraction& unk1, const Vec3& unk2);

    /**@vIndex {125}*/
    virtual void _unknown_125();

    /**@vIndex {126}*/
    virtual void _unknown_126();

    /**@vIndex {127}*/
    virtual void startSpinAttack();

    /**@vIndex {128}*/
    virtual void _unknown_128();

    /**@vIndex {129}*/
    virtual void _unknown_129();

    /**@vIndex {130}*/
    virtual void kill();

    /**@vIndex {131}*/
    virtual void die(const ActorDamageSource& unk0);

    /**@vIndex {132}*/
    virtual bool shouldDropDeathLoot() const;

    /**@vIndex {133}*/
    virtual void applySnapshot(const EntityContext& unk0, const EntityContext& unk1);

    /**@vIndex {134}*/
    virtual float getNextStep(float unk0);

    /**@vIndex {135}*/
    virtual void _unknown_135();

    /**@vIndex {136}*/
    virtual std::optional<BlockPos> getLastDeathPos() const;

    /**@vIndex {137}*/
    virtual std::optional<AutomaticID<Dimension, int>> getLastDeathDimension() const;

    /**@vIndex {138}*/
    virtual void _unknown_138();

    /**@vIndex {139}*/
    virtual void _unknown_139();

    /**@vIndex {140}*/
    virtual void _unknown_140();

    /**@vIndex {141}*/
    virtual void doWaterSplashEffect();

    /**@vIndex {142}*/
    virtual bool _shouldProvideFeedbackOnHandContainerItemSet(HandSlot unk0, const ItemStack& unk1) const;

    /**@vIndex {143}*/
    virtual bool _shouldProvideFeedbackOnArmorSet(ArmorSlot unk0, const ItemStack& unk1) const;

    /**@vIndex {144}*/
    virtual void updateEntitySpecificMolangVariables(RenderParams& unk0);

    /**@vIndex {145}*/
    virtual bool shouldTryMakeStepSound();

    /**@vIndex {146}*/
    virtual bool _hurt(const ActorDamageSource& unk0, float unk1, bool unk2, bool unk3);

    /**@vIndex {147}*/
    virtual void readAdditionalSaveData(const CompoundTag& unk0, DataLoadHelper& unk1);

    /**@vIndex {148}*/
    virtual void addAdditionalSaveData(CompoundTag& unk0) const;

    /**@vIndex {149}*/
    virtual void _playStepSound(const BlockPos& unk0, const Block& unk1);

    /**@vIndex {150}*/
    virtual void _unknown_150();
};
#pragma pack(pop)   

// 1.21.0.3
//static_assert(sizeof(Actor) == 1096);