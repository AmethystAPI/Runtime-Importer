#pragma once
#include <optional>
#include <bitset>
#include <memory>
#include "minecraft/src/common/world/actor/Actor.hpp"
#include "minecraft/src/common/world/level/BlockPos.hpp"
#include "minecraft/src/common/ActorUniqueID.hpp"

class CompassSpriteCalculator {
public:
    int mFrame;
    float mRot;
    float mRotA;
    const std::function<BlockPos(const BlockSource*, Actor*)> mTargetToPointTo;
    const std::function<bool(const BlockSource*, Actor*)> mIsPointingAtTarget;
};

class MobAnimationComponent;
class MobHurtTimeComponent;
class BodyControl;

class BuiltInMobComponents {
public:
    gsl::not_null<MobAnimationComponent*> mMobAnimationComponent;
    gsl::not_null<MobHurtTimeComponent*> mMobHurtTimeComponent;
};

class ClockSpriteCalculator {
public:
    int mFrame;
    float mRot;
    float mRotA;
};

enum MobSpawnMethod : int32_t {
    bleh
};

#pragma pack(push, 8)
class Mob : public Actor {
public:
    int mHurtDuration;
    float mHurtDir;
    float mOTilt;
    float mTilt;
    CompassSpriteCalculator mCompassSpriteCalc;
    CompassSpriteCalculator mRecoveryCompassSpriteCalc;
    ClockSpriteCalculator mClockSpriteCalc;
    float mAttackAnim;
    bool mSwinging;
    int mSwingTime;
    int mDeathTime;
    float mMovementComponentCurrentSpeed;
    bool mSurfaceMob;
    bool mNaturallySpawned;
    bool mWantsToBeJockey;
    bool mSpawnedXP;
    BuiltInMobComponents mBuiltInMobComponents;
    ActorUniqueID mCaravanHead;
    ActorUniqueID mCaravanTail;
    float mOAttackAnim;
    bool mHasBoundOrigin;
    BlockPos mBoundOrigin;
    std::optional<bool> mActuallyDoKnockbackOrNotReallyBadHackDoNotUse;
    MobSpawnMethod mSpawnMethod;
    bool mCreateAiOnReload;
    ActorUniqueID mTargetCaptainId;

public:
    // virtuals
    /*void setSprinting(bool isSprinting);

    void swing(); 

    float getAttackAnim(float a) const {
        float delta = mAttackAnim - mOAttackAnim;
        if (delta < 0.0f) delta += 1.0f;
        return (delta * a) + mOAttackAnim;
    }

    void aiStep();*/
    
    /**@vIndex {2}*/
    virtual bool hasComponent(const HashedString& unk0) const override;

    /**@vIndex {3}*/
    virtual void outOfWorld() override;

    /**@vIndex {4}*/
    virtual void reloadHardcoded(ActorInitializationMethod unk0, const VariantParameterList& unk1) override;

    /**@vIndex {5}*/
    virtual void reloadHardcodedClient(ActorInitializationMethod unk0, const VariantParameterList& unk1) override;

    /**@vIndex {6}*/
    virtual void initializeComponents(ActorInitializationMethod unk0, const VariantParameterList& unk1) override;

    /**@vIndex {9}*/
    virtual void _doInitialMove() override;

    /**@vIndex {15}*/
    virtual float getInterpolatedBodyRot(float unk0) const override;

    /**@vIndex {16}*/
    virtual float getInterpolatedHeadRot(float unk0) const override;

    /**@vIndex {17}*/
    virtual float getInterpolatedBodyYaw(float unk0) const override;

    /**@vIndex {18}*/
    virtual float getYawSpeedInDegreesPerSecond() const override;

    /**@vIndex {21}*/
    virtual void blockedByShield(const ActorDamageSource& unk0, Actor& unk1) override;

    /**@vIndex {23}*/
    virtual void teleportTo(const Vec3& unk0, bool unk1, int unk2, int unk3, bool unk4) override;

    /**@vIndex {26}*/
    virtual void normalTick() override;

    /**@vIndex {27}*/
    virtual void baseTick() override;

    /**@vIndex {29}*/
    virtual bool startRiding(Actor& unk0) override;

    /**@vIndex {30}*/
    virtual void addPassenger(Actor& unk0) override;

    /**@vIndex {43}*/
    virtual bool isImmobile() const override;

    /**@vIndex {45}*/
    virtual bool isSleeping() const override;

    /**@vIndex {46}*/
    virtual void setSleeping(bool unk0) override;

    /**@vIndex {48}*/
    virtual bool isBlocking() const override;

    /**@vIndex {50}*/
    virtual bool isAlive() const override;

    /**@vIndex {52}*/
    virtual bool isSurfaceMob() const override;

    /**@vIndex {55}*/
    virtual void setTarget(Actor* unk0) override;

    /**@vIndex {57}*/
    virtual bool attack(Actor& unk0, const ActorDamageCause& unk1) override;

    /**@vIndex {64}*/
    virtual bool canPowerJump() const override;

    /**@vIndex {70}*/
    virtual ActorDamageCause getBlockDamageCause(const Block& unk0) const override;

    /**@vIndex {71}*/
    virtual bool doFireHurt(int unk0) override;

    /**@vIndex {74}*/
    virtual void handleEntityEvent(ActorEvent unk0, int unk1) override;

    /**@vIndex {78}*/
    virtual ArmorMaterialType getArmorMaterialTypeInSlot(ArmorSlot unk0) const override;

    /**@vIndex {79}*/
    virtual int getArmorTextureIndexInSlot(ArmorSlot unk0) const override;

    /**@vIndex {80}*/
    virtual float getArmorColorInSlot(ArmorSlot unk0, int unk1) const override;

    /**@vIndex {81}*/
    virtual void setEquippedSlot(Puv::Legacy::EquipmentSlot unk0, const ItemStack& unk1) override;

    /**@vIndex {91}*/
    virtual bool canFreeze() const override;

    /**@vIndex {94}*/
    virtual bool canChangeDimensionsUsingPortal() const override;

    /**@vIndex {98}*/
    virtual float causeFallDamageToActor(float unk0, float unk1, ActorDamageSource unk2) override;

    /**@vIndex {102}*/
    virtual bool canBePulledIntoVehicle() const override;

    /**@vIndex {103}*/
    virtual bool inCaravan() const override;

    /**@vIndex {108}*/
    virtual void buildDebugInfo(std::string& unk0) const override;

    /**@vIndex {110}*/
    virtual int getDeathTime() const override;

    /**@vIndex {117}*/
    virtual void swing() override;

    /**@vIndex {130}*/
    virtual void kill() override;

    /**@vIndex {131}*/
    virtual void die(const ActorDamageSource& unk0) override;

    /**@vIndex {132}*/
    virtual bool shouldDropDeathLoot() const override;

    /**@vIndex {144}*/
    virtual void updateEntitySpecificMolangVariables(RenderParams& unk0) override;

    /**@vIndex {146}*/
    virtual bool _hurt(const ActorDamageSource& unk0, float unk1, bool unk2, bool unk3) override;

    /**@vIndex {147}*/
    virtual void readAdditionalSaveData(const CompoundTag& unk0, DataLoadHelper& unk1) override;

    /**@vIndex {148}*/
    virtual void addAdditionalSaveData(CompoundTag& unk0) const override;

    /**@vIndex {149}*/
    virtual void _playStepSound(const BlockPos& unk0, const Block& unk1) override;

    /**@vIndex {151}*/
    virtual void knockback(Actor* unk0, int unk1, float unk2, float unk3, float unk4, float unk5, float unk6);

    /**@vIndex {152}*/
    virtual void _unknown_152();

    /**@vIndex {153}*/
    virtual void setSprinting(bool unk0);

    /**@vIndex {154}*/
    virtual Puv::Legacy::LevelSoundEvent getDeathSound();

    /**@vIndex {155}*/
    virtual float getSpeed() const;

    /**@vIndex {156}*/
    virtual void setSpeed(float unk0);

    /**@vIndex {157}*/
    virtual void hurtEffects(const ActorDamageSource& unk0, float unk1, bool unk2, bool unk3);

    /**@vIndex {158}*/
    virtual void aiStep();

    /**@vIndex {159}*/
    virtual void pushActors();

    /**@vIndex {160}*/
    virtual bool checkSpawnRules(bool unk0);

    /**@vIndex {161}*/
    virtual bool checkSpawnObstruction() const;

    /**@vIndex {162}*/
    virtual int getItemUseDuration() const;

    /**@vIndex {163}*/
    virtual float getItemUseStartupProgress() const;

    /**@vIndex {164}*/
    virtual float getItemUseIntervalProgress() const;

    /**@vIndex {165}*/
    virtual float getMaxHeadXRot();

    /**@vIndex {166}*/
    virtual bool isAlliedTo(Mob* unk0);

    /**@vIndex {167}*/
    virtual bool doHurtTarget(Actor* unk0, const ActorDamageCause& unk1);

    /**@vIndex {168}*/
    virtual int getArmorValue() const;

    /**@vIndex {169}*/
    virtual void hurtArmorSlots(const ActorDamageSource& unk0, int unk1, std::bitset<4> unk2);

    /**@vIndex {170}*/
    virtual void setDamagedArmor(ArmorSlot unk0, const ItemStack& unk1);

    /**@vIndex {171}*/
    virtual void _unknown_171();

    /**@vIndex {172}*/
    virtual void sendArmor(std::bitset<4> unk0);

    /**@vIndex {173}*/
    virtual std::vector<const ItemStack*> getAllHand() const;

    /**@vIndex {174}*/
    virtual std::vector<const ItemStack*> getAllEquipment() const;

    /**@vIndex {175}*/
    virtual void dropEquipmentOnDeath();

    /**@vIndex {176}*/
    virtual void dropEquipmentOnDeath(const ActorDamageSource& unk0);

    /**@vIndex {177}*/
    virtual void clearVanishEnchantedItemsOnDeath();

    /**@vIndex {178}*/
    virtual void sendInventory(bool unk0);

    /**@vIndex {179}*/
    virtual float getDamageAfterEnchantReduction(const ActorDamageSource& unk0, float unk1) const;

    /**@vIndex {180}*/
    virtual bool createAIGoals();

    /**@vIndex {181}*/
    virtual void _unknown_181();

    /**@vIndex {182}*/
    virtual bool setItemSlot(Puv::Legacy::EquipmentSlot unk0, const ItemStack& unk1);

    /**@vIndex {183}*/
    virtual void setTransitioningSitting(bool unk0);

    /**@vIndex {184}*/
    virtual float _getWalkTargetValue(const BlockPos& unk0);

    /**@vIndex {185}*/
    virtual bool canExistWhenDisallowMob() const;

    /**@vIndex {186}*/
    virtual std::unique_ptr<BodyControl> initBodyControl();

    /**@vIndex {187}*/
    virtual void newServerAiStep();

    /**@vIndex {188}*/
    virtual void tickDeath();

    /**@vIndex {189}*/
    virtual AABB _getAdjustedAABBForSpawnCheck(const AABB& unk0, const Vec3& unk1) const;

    // non-virtuals
    int getCurrentSwingDuration() const;
};
#pragma pack(pop)

// 1.21.0.3
//static_assert(sizeof(Mob) == 1512);