/// @symgen
#pragma once
#include <gsl/gsl>
#include <minecraft/src-deps/core/utility/NonOwnerPointer.hpp>
#include <minecraft/src-deps/core/utility/AutomaticID.hpp>
#include <minecraft/src/common/gamerefs/WeakRef.hpp>
#include <minecraft/src-deps/core/file/Path.hpp>
#include <minecraft/src/common/world/level/ChunkPos.hpp>
#include <minecraft/src/common/world/level/Tick.hpp>
#include <minecraft/src/common/world/level/storage/StorageVersion.hpp>
#include <minecraft/src/common/world/level/block/LevelEvent.hpp>
#include <minecraft/src/common/gamerefs/OwnerPtr.hpp>
#include <minecraft/src/common/world/phys/Vec3.hpp>
#include <minecraft/src-deps/shared_types/legacy/LevelSoundEvent.hpp>
#include <minecraft/src/common/world/level/GameType.hpp>
#include <minecraft/src/common/world/item/registry/ItemRegistryRef.hpp>
#include <minecraft/src-client/common/client/social/MultiplayerGameinfo.hpp>

// Auto-generated: Forward declarations
namespace PlayerCapabilities { struct ISharedController; }
namespace PositionTrackingDB { class PositionTrackingDBClient; }
namespace cg { class ImageBuffer; }
namespace mce {class UUID; class Color; }
class EntityContext;
struct ActorUniqueID;
class LevelSettings;
class LevelData;
class Experiments;
class ChunkTickRangeManager;
class PortalForcer;
class Player;
class Actor;
class Spawner;
class ProjectileFactory;
class ActorDefinitionGroup;
class BlockDefinitionGroup;
class PropertyGroupManager;
class AutomationBehaviorTreeGroup;
class BehaviorFactory;
class InternalComponentRegistry;
class BlockSource;
class LevelChunk;
class Mob;
class IMinecraftEventing;
class BiomeManager;
class LevelListener;
class ActorDamageSource;
class Explosion;
class BlockPos;
class NavigationComponent;
class PhotoStorage;
class TickingAreasManager;
struct ActorDefinitionIdentifier;
class IConstBlockSource;
class PlayerEventCoordinator;
class ServerPlayerEventCoordinator;
class ClientPlayerEventCoordinator;
class ActorEventCoordinator;
class BlockEventCoordinator;
class ItemEventCoordinator;
class ServerNetworkEventCoordinator;
class ScriptingEventCoordinator;
class ScriptDeferredEventCoordinator;
class LevelEventCoordinator;
class CompoundTag;
class UserEntityIdentifierComponent;
class Block;
struct Bounds;
class ChunkSource;
class NetworkIdentifier;
class _TickPtr;
class Particle;
class HashedString;
class MolangVariableMap;
struct ResolvedItemIconInfo;
class SavedDataStorage;
class MapItemSavedData;
class TaskGroup;
class ActorInfoRegistry;
class EntitySystems;
class TagRegistry;
struct PlayerMovementSettings;
class SerializedSkin;
class PacketSender;
class HitResult;
struct AdventureSettings;
class GameRules;
class Abilities;
class PermissionsHandler;
struct ScreenshotOptions;
class LootTables;
class LayeredAbilities;
class Recipes;
class BlockReducer;
class ItemComponentPacket;
class BlockLegacy;
class Level;
class ChangeDimensionRequest;
class BossEventSubscriptionManager;
class ActorAnimationGroup;
class ActorAnimationControllerGroup;
class Difficulty;
class DimensionConversionData;
class StrictEntityContext;
class ActorRuntimeID;
class StructureManager;
class Path;
class PlayerSleepStatus;
class EducationLevelSettings;
class ActorEvent;
class IUnknownBlockTypeRegistry;
class NetEventCallback;
class LevelSoundManager;
class SoundPlayerInterface;
class BlockTypeRegistry;
class ChunkViewSource;
class WeakEntityRef;
class Dimension;
class TickingAreaList;
enum class ParticleType : int {};
class BlockPalette;

/// @VirtualTable {0xDEADBEEF, this}
class ILevel : public Bedrock::EnableNonOwnerReferences {
public:
    /**@vIndex {0}*/
    __declspec(dllimport) virtual ~ILevel();

    /**@vIndex {1}*/
    __declspec(dllimport) virtual bool initialize(const std::string&, const LevelSettings&, LevelData*, const Experiments&, const std::string*);

    /**@vIndex {2}*/
    __declspec(dllimport) virtual void startLeaveGame();

    /**@vIndex {3}*/
    __declspec(dllimport) virtual bool isLeaveGameDone();

    /**@vIndex {4}*/
    __declspec(dllimport) virtual WeakRef<Dimension> getOrCreateDimension(DimensionType);

    /**@vIndex {5}*/
    __declspec(dllimport) virtual WeakRef<Dimension> getDimension(DimensionType) const;

    /**
    * @vIndex {6}
    * @brief Validates that the DimensionType != Undefined, in that case resets it to the Overworld
    */
    __declspec(dllimport) virtual DimensionType getLastOrDefaultSpawnDimensionId(DimensionType) const;

    /**@vIndex {7}*/
    __declspec(dllimport) virtual void _unknown_7();

    /**@vIndex {8}*/
    __declspec(dllimport) virtual void _unknown_8();

    /**@vIndex {9}*/
    __declspec(dllimport) virtual void _unknown_9();

    /**@vIndex {10}*/
    __declspec(dllimport) virtual void _unknown_10();

    /**@vIndex {11}*/
    __declspec(dllimport) virtual unsigned int getChunkTickRange() const;

    /**@vIndex {12}*/
    __declspec(dllimport) virtual const ChunkTickRangeManager& getChunkTickRangeManager() const;

    /**@vIndex {13}*/
    __declspec(dllimport) virtual PortalForcer& getPortalForcer();

    /**@vIndex {14}*/
    __declspec(dllimport) virtual void requestPlayerChangeDimension(Player&, ChangeDimensionRequest&&);

    /**@vIndex {15}*/
    __declspec(dllimport) virtual void entityChangeDimension(Actor&, DimensionType, std::optional<Vec3>);

    /**@vIndex {16}*/
    __declspec(dllimport) virtual Spawner& getSpawner() const;

    /**@vIndex {17}*/
    __declspec(dllimport) virtual gsl::not_null<Bedrock::NonOwnerPointer<BossEventSubscriptionManager>> getBossEventSubscriptionManager();

    /**@vIndex {18}*/
    __declspec(dllimport) virtual ProjectileFactory& getProjectileFactory() const;

    /**@vIndex {19}*/
    __declspec(dllimport) virtual ActorDefinitionGroup* getEntityDefinitions() const;

    /**@vIndex {20}*/
    __declspec(dllimport) virtual gsl::not_null<Bedrock::NonOwnerPointer<ActorAnimationGroup>> getActorAnimationGroup() const;

    /**@vIndex {21}*/
    __declspec(dllimport) virtual Bedrock::NonOwnerPointer<ActorAnimationControllerGroup> getActorAnimationControllerGroup() const;

    /**@vIndex {22}*/
    __declspec(dllimport) virtual BlockDefinitionGroup* getBlockDefinitions() const;

    /**@vIndex {23}*/
    __declspec(dllimport) virtual void _unknown_23();

    /**@vIndex {24}*/
    __declspec(dllimport) virtual void _unknown_24();

    /**@vIndex {25}*/
    __declspec(dllimport) virtual PropertyGroupManager& getActorPropertyGroup() const;

    /**@vIndex {26}*/
    __declspec(dllimport) virtual void _unknown_26();

    /**@vIndex {27}*/
    __declspec(dllimport) virtual void _unknown_27();

    /**@vIndex {28}*/
    __declspec(dllimport) virtual bool getDisablePlayerInteractions() const;

    /**@vIndex {29}*/
    __declspec(dllimport) virtual void setDisablePlayerInteractions(bool);

    /**@vIndex {30}*/
    __declspec(dllimport) virtual AutomationBehaviorTreeGroup& getAutomationBehaviorTreeGroup() const;

    /**@vIndex {31}*/
    __declspec(dllimport) virtual BehaviorFactory& getBehaviorFactory() const;

    /**@vIndex {32}*/
    __declspec(dllimport) virtual Difficulty getDifficulty() const;

    /**@vIndex {33}*/
    __declspec(dllimport) virtual InternalComponentRegistry& getInternalComponentRegistry() const;

    /**@vIndex {34}*/
    __declspec(dllimport) virtual DimensionConversionData getDimensionConversionData() const;

    /**@vIndex {35}*/
    __declspec(dllimport) virtual float getSpecialMultiplier(DimensionType) const;

    /**@vIndex {36}*/
    __declspec(dllimport) virtual bool hasCommandsEnabled() const;

    /**@vIndex {37}*/
    __declspec(dllimport) virtual bool useMsaGamertagsOnly() const;

    /**@vIndex {38}*/
    __declspec(dllimport) virtual void setMsaGamertagsOnly(bool);

    /**@vIndex {39}*/
    __declspec(dllimport) virtual Actor* addEntity(BlockSource&, OwnerPtr<EntityContext>);

    /**@vIndex {40}*/
    __declspec(dllimport) virtual Actor* addGlobalEntity(BlockSource&, OwnerPtr<EntityContext>);

    /**@vIndex {41}*/
    __declspec(dllimport) virtual Actor* addAutonomousEntity(BlockSource&, OwnerPtr<EntityContext>);

    /**@vIndex {42}*/
    __declspec(dllimport) virtual void addUser(OwnerPtr<EntityContext>);

    /**@vIndex {43}*/
    __declspec(dllimport) virtual Actor* addDisplayEntity(BlockSource&, OwnerPtr<EntityContext>);

    /**@vIndex {44}*/
    __declspec(dllimport) virtual void removeDisplayEntity(WeakEntityRef);

    /**@vIndex {45}*/
    __declspec(dllimport) virtual void suspendPlayer(Player&);

    /**@vIndex {46}*/
    __declspec(dllimport) virtual void resumePlayer(Player&);

    /**@vIndex {47}*/
    __declspec(dllimport) virtual bool isPlayerSuspended(Player&) const;

    /**@vIndex {48}*/
    __declspec(dllimport) virtual OwnerPtr<EntityContext> removeActorAndTakeEntity(WeakEntityRef);

    /**@vIndex {49}*/
    __declspec(dllimport) virtual OwnerPtr<EntityContext> removeActorFromWorldAndTakeEntity(WeakEntityRef);

    /**@vIndex {50}*/
    __declspec(dllimport) virtual OwnerPtr<EntityContext> takeEntity(WeakEntityRef, LevelChunk&);

    /**@vIndex {51}*/
    __declspec(dllimport) virtual StrictEntityContext fetchStrictEntity(ActorUniqueID, bool) const;

    /**@vIndex {52}*/
    __declspec(dllimport) virtual Actor* fetchEntity(ActorUniqueID, bool) const;

    /**@vIndex {53}*/
    __declspec(dllimport) virtual Actor* getRuntimeEntity(ActorRuntimeID, bool) const;

    /**@vIndex {54}*/
    __declspec(dllimport) virtual Mob* getMob(ActorUniqueID) const;

    /**@vIndex {55}*/
    __declspec(dllimport) virtual Player* getPlayer(ActorUniqueID) const;

    /**@vIndex {56}*/
    __declspec(dllimport) virtual Player* getPlayer(const mce::UUID&) const;

    /**@vIndex {57}*/
    __declspec(dllimport) virtual Player* getPlayer(const std::string&) const;

    /**@vIndex {58}*/
    __declspec(dllimport) virtual Player* getPlayerByXuid(const std::string&) const;

    /**@vIndex {59}*/
    __declspec(dllimport) virtual Player* getPlatformPlayer(const std::string&) const;

    /**@vIndex {60}*/
    __declspec(dllimport) virtual Player* getPlayerFromServerId(const std::string&) const;

    /**@vIndex {61}*/
    __declspec(dllimport) virtual Player* getRuntimePlayer(ActorRuntimeID) const;

    /**@vIndex {62}*/
    __declspec(dllimport) virtual int getNumRemotePlayers();

    /**@vIndex {63}*/
    __declspec(dllimport) virtual Player* getPrimaryLocalPlayer() const;

    /**@vIndex {64}*/
    __declspec(dllimport) virtual IMinecraftEventing& getEventing();

    /**@vIndex {65}*/
    __declspec(dllimport) virtual mce::Color getPlayerColor(const Player&) const;

    /**@vIndex {66}*/
    __declspec(dllimport) virtual const Tick& getCurrentTick() const;

    /**@vIndex {67}*/
    __declspec(dllimport) virtual const Tick getCurrentServerTick() const;

    /**@vIndex {68}*/
    __declspec(dllimport) virtual void _unknown_68();

    /**@vIndex {69}*/
    __declspec(dllimport) virtual void _unknown_69();

    /**@vIndex {70}*/
    __declspec(dllimport) virtual BlockPalette& getBlockPalette() const;

    /**@vIndex {71}*/
    __declspec(dllimport) virtual void _unknown_71();

    /**@vIndex {72}*/
    __declspec(dllimport) virtual void _unknown_72();

    /**@vIndex {73}*/
    __declspec(dllimport) virtual void _unknown_73();

    /**@vIndex {74}*/
    __declspec(dllimport) virtual void _unknown_74();

    /**@vIndex {75}*/
    __declspec(dllimport) virtual void _unknown_75();

    /**@vIndex {76}*/
    __declspec(dllimport) virtual void _unknown_76();

    /**@vIndex {77}*/
    __declspec(dllimport) virtual void _unknown_77();

    /**@vIndex {78}*/
    __declspec(dllimport) virtual gsl::not_null<Bedrock::NonOwnerPointer<StructureManager>> getStructureManager();

    /**@vIndex {79}*/
    __declspec(dllimport) virtual const gsl::not_null<Bedrock::NonOwnerPointer<StructureManager>> getStructureManager() const;

    /**@vIndex {80}*/
    __declspec(dllimport) virtual void _unknown_80();

    /**@vIndex {81}*/
    __declspec(dllimport) virtual void _unknown_81();

    /**@vIndex {82}*/
    __declspec(dllimport) virtual void _unknown_82();

    /**@vIndex {83}*/
    __declspec(dllimport) virtual void _unknown_83();

    /**@vIndex {84}*/
    __declspec(dllimport) virtual BiomeManager& getBiomeManager();

    /**@vIndex {85}*/
    __declspec(dllimport) virtual const BiomeManager& getBiomeManager() const;

    /**@vIndex {86}*/
    __declspec(dllimport) virtual void _unknown_86();

    /**@vIndex {87}*/
    __declspec(dllimport) virtual void _unknown_87();

    /**@vIndex {88}*/
    __declspec(dllimport) virtual void _unknown_88();

    /**@vIndex {89}*/
    __declspec(dllimport) virtual void _unknown_89();

    /**@vIndex {90}*/
    __declspec(dllimport) virtual void _unknown_90();

    /**@vIndex {91}*/
    __declspec(dllimport) virtual void _unknown_91();

    /**@vIndex {92}*/
    __declspec(dllimport) virtual void addListener(LevelListener&);

    /**@vIndex {93}*/
    __declspec(dllimport) virtual void removeListener(LevelListener&);

    /**@vIndex {94}*/
    __declspec(dllimport) virtual void tickEntities();

    /**@vIndex {95}*/
    __declspec(dllimport) virtual void tickEntitySystems();

    /**@vIndex {96}*/
    __declspec(dllimport) virtual void _unknown_96();

    /**@vIndex {97}*/
    __declspec(dllimport) virtual void _unknown_97();

    /**@vIndex {98}*/
    __declspec(dllimport) virtual void onPlayerDeath(Player&, const ActorDamageSource&);

    /**@vIndex {99}*/
    __declspec(dllimport) virtual void tick();

    /**@vIndex {100}*/
    __declspec(dllimport) virtual bool explode(Explosion&);

    /**@vIndex {101}*/
    __declspec(dllimport) virtual bool explode(BlockSource&, Actor*, const Vec3&, float, bool, bool, float, bool);

    /**@vIndex {102}*/
    __declspec(dllimport) virtual void spawnParticleEffect(const std::string&, const Vec3&, Dimension*);

    /**@vIndex {103}*/
    __declspec(dllimport) virtual void denyEffect(BlockSource&, const Vec3&);

    /**@vIndex {104}*/
    __declspec(dllimport) virtual void potionSplash(const Vec3&, const mce::Color&, bool);

    /**@vIndex {105}*/
    __declspec(dllimport) virtual bool extinguishFire(BlockSource&, const BlockPos&, unsigned char, Actor*);

    /**@vIndex {106}*/
    __declspec(dllimport) virtual std::unique_ptr<Path> findPath(Actor&, Actor&, NavigationComponent&);

    /**@vIndex {107}*/
    __declspec(dllimport) virtual std::unique_ptr<Path> findPath(Actor&, int, int, int, NavigationComponent&);

    /**@vIndex {108}*/
    __declspec(dllimport) virtual void updateSleepingPlayerList();

    /**@vIndex {109}*/
    __declspec(dllimport) virtual void setSleepStatus(const PlayerSleepStatus&);

    /**@vIndex {110}*/
    __declspec(dllimport) virtual PlayerSleepStatus getSleepStatus() const;

    /**@vIndex {111}*/
    __declspec(dllimport) virtual int getTime() const;

    /**@vIndex {112}*/
    __declspec(dllimport) virtual void setTime(int);

    /**@vIndex {113}*/
    __declspec(dllimport) virtual unsigned int getSeed();

    /**@vIndex {114}*/
    __declspec(dllimport) virtual const BlockPos& getDefaultSpawn() const;

    /**@vIndex {115}*/
    __declspec(dllimport) virtual void setDefaultSpawn(const BlockPos&);

    /**@vIndex {116}*/
    __declspec(dllimport) virtual void _unknown_116();

    /**@vIndex {117}*/
    __declspec(dllimport) virtual void setDefaultGameType(GameType);

    /**@vIndex {118}*/
    __declspec(dllimport) virtual GameType getDefaultGameType() const;

    /**@vIndex {119}*/
    __declspec(dllimport) virtual void setDifficulty(Difficulty);

    /**@vIndex {120}*/
    __declspec(dllimport) virtual void setMultiplayerGameIntent(bool);

    /**@vIndex {121}*/
    __declspec(dllimport) virtual bool getMultiplayerGameIntent() const;

    /**@vIndex {122}*/
    __declspec(dllimport) virtual void setMultiplayerGame(bool);

    /**@vIndex {123}*/
    __declspec(dllimport) virtual bool isMultiplayerGame() const;

    /**@vIndex {124}*/
    __declspec(dllimport) virtual void setLANBroadcastIntent(bool);

    /**@vIndex {125}*/
    __declspec(dllimport) virtual bool getLANBroadcastIntent() const;

    /**@vIndex {126}*/
    __declspec(dllimport) virtual void setLANBroadcast(bool);

    /**@vIndex {127}*/
    __declspec(dllimport) virtual bool getLANBroadcast() const;

    /**@vIndex {128}*/
    __declspec(dllimport) virtual void setXBLBroadcastIntent(Social::GamePublishSetting);

    /**@vIndex {129}*/
    __declspec(dllimport) virtual Social::GamePublishSetting getXBLBroadcastIntent() const;

    /**@vIndex {130}*/
    __declspec(dllimport) virtual bool hasXBLBroadcastIntent() const;

    /**@vIndex {131}*/
    __declspec(dllimport) virtual void setXBLBroadcastMode(Social::GamePublishSetting);

    /**@vIndex {132}*/
    __declspec(dllimport) virtual Social::GamePublishSetting getXBLBroadcastMode() const;

    /**@vIndex {133}*/
    __declspec(dllimport) virtual bool hasXBLBroadcast() const;

    /**@vIndex {134}*/
    __declspec(dllimport) virtual void setPlatformBroadcastIntent(Social::GamePublishSetting);

    /**@vIndex {135}*/
    __declspec(dllimport) virtual Social::GamePublishSetting getPlatformBroadcastIntent() const;

    /**@vIndex {136}*/
    __declspec(dllimport) virtual bool hasPlatformBroadcastIntent() const;

    /**@vIndex {137}*/
    __declspec(dllimport) virtual void setPlatformBroadcastMode(Social::GamePublishSetting);

    /**@vIndex {138}*/
    __declspec(dllimport) virtual Social::GamePublishSetting getPlatformBroadcastMode() const;

    /**@vIndex {139}*/
    __declspec(dllimport) virtual bool hasPlatformBroadcast() const;

    /**@vIndex {140}*/
    __declspec(dllimport) virtual void setHasLockedBehaviorPack(bool);

    /**@vIndex {141}*/
    __declspec(dllimport) virtual void setHasLockedResourcePack(bool);

    /**@vIndex {142}*/
    __declspec(dllimport) virtual void setCommandsEnabled(bool);

    /**@vIndex {143}*/
    __declspec(dllimport) virtual void setWorldTemplateOptionsUnlocked();

    /**@vIndex {144}*/
    __declspec(dllimport) virtual bool hasLevelStorage() const;

    /**@vIndex {145}*/
    __declspec(dllimport) virtual void _unknown_145();

    /**@vIndex {146}*/
    __declspec(dllimport) virtual void _unknown_146();

    /**@vIndex {147}*/
    __declspec(dllimport) virtual LevelData& getLevelData();

    /**@vIndex {148}*/
    __declspec(dllimport) virtual const LevelData& getLevelData() const;

    /**@vIndex {149}*/
    __declspec(dllimport) virtual PhotoStorage& getPhotoStorage();

    /**@vIndex {150}*/
    __declspec(dllimport) virtual void createPhotoStorage();

    /**@vIndex {151}*/
    __declspec(dllimport) virtual void setEducationLevelSettings(EducationLevelSettings);

    /**@vIndex {152}*/
    __declspec(dllimport) virtual const std::optional<EducationLevelSettings>& getEducationLevelSettings() const;

    /**@vIndex {153}*/
    __declspec(dllimport) virtual void save();

    /**@vIndex {154}*/
    __declspec(dllimport) virtual void saveLevelData();

    /**@vIndex {155}*/
    __declspec(dllimport) virtual void saveGameData();

    /**@vIndex {156}*/
    __declspec(dllimport) virtual std::shared_ptr<void*> requestTimedStorageDeferment();

    /**@vIndex {157}*/
    __declspec(dllimport) virtual TickingAreasManager& getTickingAreasMgr();

    /**@vIndex {158}*/
    __declspec(dllimport) virtual void addTickingAreaList(DimensionType, const std::shared_ptr<TickingAreaList>&);

    /**@vIndex {159}*/
    __declspec(dllimport) virtual void sendServerLegacyParticle(ParticleType, const Vec3&, const Vec3&, int);

    /**@vIndex {160}*/
    __declspec(dllimport) virtual void playSound(DimensionType, Puv::Legacy::LevelSoundEvent, const Vec3&, int, const ActorDefinitionIdentifier&, bool, bool);

    /**@vIndex {161}*/
    __declspec(dllimport) virtual void playSound(const IConstBlockSource&, Puv::Legacy::LevelSoundEvent, const Vec3&, int, const ActorDefinitionIdentifier&, bool, bool);

    /**@vIndex {162}*/
    __declspec(dllimport) virtual void playSound(const std::string&, const Vec3&, float, float);

    /**@vIndex {163}*/
    __declspec(dllimport) virtual void playSound(Puv::Legacy::LevelSoundEvent, const Vec3&, float, float);

    /**@vIndex {164}*/
    __declspec(dllimport) virtual void playSound(Puv::Legacy::LevelSoundEvent, const Vec3&, int, const ActorDefinitionIdentifier&, bool, bool);

    /**@vIndex {165}*/
    __declspec(dllimport) virtual PlayerEventCoordinator& getRemotePlayerEventCoordinator();

    /**@vIndex {166}*/
    __declspec(dllimport) virtual ServerPlayerEventCoordinator& getServerPlayerEventCoordinator();

    /**@vIndex {167}*/
    __declspec(dllimport) virtual ClientPlayerEventCoordinator& getClientPlayerEventCoordinator();

    /**@vIndex {168}*/
    __declspec(dllimport) virtual ActorEventCoordinator& getActorEventCoordinator();

    /**@vIndex {169}*/
    __declspec(dllimport) virtual BlockEventCoordinator& getBlockEventCoordinator();

    /**@vIndex {170}*/
    __declspec(dllimport) virtual ItemEventCoordinator& getItemEventCoordinator();

    /**@vIndex {171}*/
    __declspec(dllimport) virtual ServerNetworkEventCoordinator& getServerNetworkEventCoordinator();

    /**@vIndex {172}*/
    __declspec(dllimport) virtual ScriptingEventCoordinator& getScriptingEventCoordinator();

    /**@vIndex {173}*/
    __declspec(dllimport) virtual ScriptDeferredEventCoordinator& getScriptDeferredEventCoordinator();

    /**@vIndex {174}*/
    __declspec(dllimport) virtual LevelEventCoordinator& getLevelEventCoordinator();

    /**@vIndex {175}*/
    __declspec(dllimport) virtual void handleLevelEvent(LevelEvent, const CompoundTag&);

    /**@vIndex {176}*/
    __declspec(dllimport) virtual void handleLevelEvent(LevelEvent, const Vec3&, int);

    /**@vIndex {177}*/
    __declspec(dllimport) virtual void handleStopSoundEvent(const std::string&);

    /**@vIndex {178}*/
    __declspec(dllimport) virtual void handleStopAllSounds();

    /**@vIndex {179}*/
    __declspec(dllimport) virtual void broadcastLevelEvent(LevelEvent, const CompoundTag&, const UserEntityIdentifierComponent*);

    /**@vIndex {180}*/
    __declspec(dllimport) virtual void broadcastLevelEvent(LevelEvent, const Vec3&, int, const UserEntityIdentifierComponent*);

    /**@vIndex {181}*/
    __declspec(dllimport) virtual void broadcastLocalEvent(BlockSource&, LevelEvent, const Vec3&, const Block&);

    /**@vIndex {182}*/
    __declspec(dllimport) virtual void broadcastLocalEvent(BlockSource&, LevelEvent, const Vec3&, int);

    /**@vIndex {183}*/
    __declspec(dllimport) virtual void broadcastSoundEvent(Dimension&, Puv::Legacy::LevelSoundEvent, const Vec3&, int, const ActorDefinitionIdentifier&, bool, bool);

    /**@vIndex {184}*/
    __declspec(dllimport) virtual void broadcastSoundEvent(BlockSource&, Puv::Legacy::LevelSoundEvent, const Vec3&, int, const ActorDefinitionIdentifier&, bool, bool);

    /**@vIndex {185}*/
    __declspec(dllimport) virtual void broadcastSoundEvent(BlockSource&, Puv::Legacy::LevelSoundEvent, const Vec3&, const Block&, const ActorDefinitionIdentifier&, bool, bool);

    /**@vIndex {186}*/
    __declspec(dllimport) virtual void broadcastActorEvent(Actor&, ActorEvent, int) const;

    /**@vIndex {187}*/
    __declspec(dllimport) virtual void addChunkViewTracker(std::weak_ptr<ChunkViewSource>);

    /**@vIndex {188}*/
    __declspec(dllimport) virtual void onChunkReload(const Bounds&);

    /**@vIndex {189}*/
    __declspec(dllimport) virtual void onChunkReloaded(ChunkSource&, LevelChunk&);

    /**@vIndex {190}*/
    __declspec(dllimport) virtual int getActivePlayerCount() const;

    /**@vIndex {191}*/
    __declspec(dllimport) virtual int getActiveUsersCount() const;

    /**@vIndex {192}*/
    __declspec(dllimport) virtual void forEachPlayer(std::function<bool(const Player&)>) const;

    /**@vIndex {193}*/
    __declspec(dllimport) virtual void forEachPlayer(std::function<bool(Player&)>);

    /**@vIndex {194}*/
    __declspec(dllimport) virtual void forEachUser(std::function<bool(const EntityContext&)>) const;

    /**@vIndex {195}*/
    __declspec(dllimport) virtual void forEachUser(std::function<bool(EntityContext&)>);

    /**@vIndex {196}*/
    __declspec(dllimport) virtual Player* findPlayer(std::function<bool(const WeakEntityRef&)>) const;

    /**@vIndex {197}*/
    __declspec(dllimport) virtual Player* findPlayer(std::function<bool(const Player&)>) const;

    /**@vIndex {198}*/
    __declspec(dllimport) virtual int getUserCount() const;

    /**@vIndex {199}*/
    __declspec(dllimport) virtual int countUsersWithMatchingNetworkId(const NetworkIdentifier&) const;

    /**@vIndex {200}*/
    __declspec(dllimport) virtual const std::vector<OwnerPtr<EntityContext>>& getUsers() const;

    /**@vIndex {201}*/
    __declspec(dllimport) virtual const std::vector<OwnerPtr<EntityContext>>& getEntities() const;

    /**@vIndex {202}*/
    __declspec(dllimport) virtual void _unknown_202();

    /**@vIndex {203}*/
    __declspec(dllimport) virtual void onChunkLoaded(ChunkSource&, LevelChunk&);

    /**@vIndex {204}*/
    __declspec(dllimport) virtual void onChunkDiscarded(LevelChunk&);

    /**@vIndex {205}*/
    __declspec(dllimport) virtual void _unknown_205();

    /**@vIndex {206}*/
    __declspec(dllimport) virtual void queueEntityDestruction(OwnerPtr<EntityContext>);

    /**@vIndex {207}*/
    __declspec(dllimport) virtual OwnerPtr<EntityContext> removeEntity(WeakEntityRef);

    /**@vIndex {208}*/
    __declspec(dllimport) virtual OwnerPtr<EntityContext> removeEntity(Actor&);

    /**@vIndex {209}*/
    __declspec(dllimport) virtual void forceRemoveEntity(Actor&);

    /**@vIndex {210}*/
    __declspec(dllimport) virtual void forceRemoveEntityfromWorld(Actor&);

    /**@vIndex {211}*/
    __declspec(dllimport) virtual void forceFlushRemovedPlayers();

    /**@vIndex {212}*/
    __declspec(dllimport) virtual void _unknown_212();

    /**@vIndex {213}*/
    __declspec(dllimport) virtual void levelCleanupQueueEntityRemoval(OwnerPtr<EntityContext>);

    /**@vIndex {214}*/
    __declspec(dllimport) virtual void registerTemporaryPointer(_TickPtr&);

    /**@vIndex {215}*/
    __declspec(dllimport) virtual void unregisterTemporaryPointer(_TickPtr&);

    /**@vIndex {216}*/
    __declspec(dllimport) virtual bool destroyBlock(BlockSource&, const BlockPos&, bool);

    /**@vIndex {217}*/
    __declspec(dllimport) virtual void upgradeStorageVersion(StorageVersion);

    /**@vIndex {218}*/
    __declspec(dllimport) virtual void suspendAndSave();

    /**@vIndex {219}*/
    __declspec(dllimport) virtual Particle* addParticle(ParticleType, const Vec3&, const Vec3&, int, const CompoundTag*, bool);

    /**@vIndex {220}*/
    __declspec(dllimport) virtual void _destroyEffect(const BlockPos&, const Block&, int);

    /**@vIndex {221}*/
    __declspec(dllimport) virtual void addParticleEffect(const HashedString&, const Vec3&, const MolangVariableMap&);

    /**@vIndex {222}*/
    __declspec(dllimport) virtual void addTerrainParticleEffect(const BlockPos&, const Block&, const Vec3&, float, float, float);

    /**@vIndex {223}*/
    __declspec(dllimport) virtual void addTerrainSlideEffect(const BlockPos&, const Block&, const Vec3&, float, float, float);

    /**@vIndex {224}*/
    __declspec(dllimport) virtual void addBreakingItemParticleEffect(const Vec3&, ParticleType, const ResolvedItemIconInfo&);

    /**@vIndex {225}*/
    __declspec(dllimport) virtual ActorUniqueID getNewUniqueID();

    /**@vIndex {226}*/
    __declspec(dllimport) virtual ActorRuntimeID getNextRuntimeID();

    /**@vIndex {227}*/
    __declspec(dllimport) virtual const std::vector<ChunkPos>& getTickingOffsets() const;

    /**@vIndex {228}*/
    __declspec(dllimport) virtual const std::vector<ChunkPos>& getClientTickingOffsets() const;

    /**@vIndex {229}*/
    __declspec(dllimport) virtual std::vector<ChunkPos> getSortedPositionsFromClientOffsets(const std::vector<ChunkPos>&) const;

    /**@vIndex {230}*/
    __declspec(dllimport) virtual bool isExporting() const;

    /**@vIndex {231}*/
    __declspec(dllimport) virtual void setIsExporting(bool);

    /**@vIndex {232}*/
    __declspec(dllimport) virtual SavedDataStorage& getSavedData();

    /**@vIndex {233}*/
    __declspec(dllimport) virtual void _unknown_233();

    /**@vIndex {234}*/
    __declspec(dllimport) virtual void _unknown_234();

    /**@vIndex {235}*/
    __declspec(dllimport) virtual MapItemSavedData* getMapSavedData(ActorUniqueID);

    /**@vIndex {236}*/
    __declspec(dllimport) virtual void requestMapInfo(ActorUniqueID, bool);

    /**@vIndex {237}*/
    __declspec(dllimport) virtual ActorUniqueID expandMapByID(ActorUniqueID, bool);

    /**@vIndex {238}*/
    __declspec(dllimport) virtual bool copyAndLockMap(ActorUniqueID, ActorUniqueID);

    /**@vIndex {239}*/
    __declspec(dllimport) virtual MapItemSavedData& createMapSavedData(const std::vector<ActorUniqueID>&, const BlockPos&, DimensionType, int);

    /**@vIndex {240}*/
    __declspec(dllimport) virtual MapItemSavedData& createMapSavedData(const ActorUniqueID&, const BlockPos&, DimensionType, int);

    /**@vIndex {241}*/
    __declspec(dllimport) virtual Core::PathBuffer<std::string> getScreenshotsFolder() const;

    /**@vIndex {242}*/
    __declspec(dllimport) virtual std::string getLevelId() const;

    /**@vIndex {243}*/
    __declspec(dllimport) virtual void setLevelId(std::string);

    /**@vIndex {244}*/
    __declspec(dllimport) virtual TaskGroup& getSyncTasksGroup();

    /**@vIndex {245}*/
    __declspec(dllimport) virtual TaskGroup& getIOTasksGroup();

    /**@vIndex {246}*/
    __declspec(dllimport) virtual void _unknown_246();

    /**@vIndex {247}*/
    __declspec(dllimport) virtual void _unknown_247();

    /**@vIndex {248}*/
    __declspec(dllimport) virtual void _unknown_248();

    /**@vIndex {249}*/
    __declspec(dllimport) virtual void _unknown_249();

    /**@vIndex {250}*/
    __declspec(dllimport) virtual void _unknown_250();

    /**@vIndex {251}*/
    __declspec(dllimport) virtual bool isEdu() const;

    /**@vIndex {252}*/
    __declspec(dllimport) virtual void _unknown_252();

    /**@vIndex {253}*/
    __declspec(dllimport) virtual void _unknown_253();

    /**@vIndex {254}*/
    __declspec(dllimport) virtual ActorInfoRegistry* getActorInfoRegistry();

    /**@vIndex {255}*/
    //__declspec(dllimport) virtual StackRefResult<const EntityRegistry> getEntityRegistry() const;
    __declspec(dllimport) virtual void _unknown_255();

    /**@vIndex {256}*/
    //__declspec(dllimport) virtual StackRefResult<EntityRegistry> getEntityRegistry();
    __declspec(dllimport) virtual void _unknown_256();

    /**@vIndex {257}*/
    __declspec(dllimport) virtual EntitySystems& getEntitySystems();

    /**@vIndex {258}*/
    __declspec(dllimport) virtual WeakRef<EntityContext> getLevelEntity();

    /**@vIndex {259}*/
    __declspec(dllimport) virtual void _unknown_259();

    /**@vIndex {260}*/
    __declspec(dllimport) virtual void _unknown_260();

    /**@vIndex {261}*/
    __declspec(dllimport) virtual const PlayerCapabilities::ISharedController& getCapabilities() const;

    /**@vIndex {262}*/
    //__declspec(dllimport) virtual TagRegistry<IDType<LevelTagIDType>, IDType<LevelTagSetIDType>>& getTagRegistry();
    __declspec(dllimport) virtual void _unknown_262();

    /**@vIndex {263}*/
    __declspec(dllimport) virtual const PlayerMovementSettings& getPlayerMovementSettings() const;

    /**@vIndex {264}*/
    __declspec(dllimport) virtual void setPlayerMovementSettings(const PlayerMovementSettings&);

    /**@vIndex {265}*/
    __declspec(dllimport) virtual bool canUseSkin(const SerializedSkin&, const NetworkIdentifier&, const mce::UUID&, const ActorUniqueID&) const;

    /**@vIndex {266}*/
    __declspec(dllimport) virtual PositionTrackingDB::PositionTrackingDBClient* getPositionTrackerDBClient() const;

    /**@vIndex {267}*/
    __declspec(dllimport) virtual void _unknown_267();

    /**@vIndex {268}*/
    __declspec(dllimport) virtual void flushRunTimeLighting();

    /**@vIndex {269}*/
    __declspec(dllimport) virtual void loadBlockDefinitionGroup(const Experiments&);

    /**@vIndex {270}*/
    __declspec(dllimport) virtual void initializeBlockDefinitionGroup();

    /**@vIndex {271}*/
    __declspec(dllimport) virtual Bedrock::NonOwnerPointer<IUnknownBlockTypeRegistry> getUnknownBlockTypeRegistry();

    /**@vIndex {272}*/
    __declspec(dllimport) virtual bool isClientSide() const;

    /**@vIndex {273}*/
    __declspec(dllimport) virtual void _unknown_273();

    /**@vIndex {274}*/
    __declspec(dllimport) virtual void _unknown_274();

    /**@vIndex {275}*/
    __declspec(dllimport) virtual const std::string& getPlayerXUID(const mce::UUID&) const;

    /**@vIndex {276}*/
    __declspec(dllimport) virtual const std::string& getPlayerPlatformOnlineId(const mce::UUID&) const;

    /**@vIndex {277}*/
    __declspec(dllimport) virtual const std::vector<WeakEntityRef>& getActiveUsers() const;

    /**@vIndex {278}*/
    __declspec(dllimport) virtual std::vector<Actor*> getRuntimeActorList() const;

    /**@vIndex {279}*/
    __declspec(dllimport) virtual void _unknown_279();

    /**@vIndex {280}*/
    __declspec(dllimport) virtual void _unknown_280();

    /**@vIndex {281}*/
    __declspec(dllimport) virtual PacketSender* getPacketSender() const;

    /**@vIndex {282}*/
    __declspec(dllimport) virtual void setPacketSender(PacketSender*);

    /**@vIndex {283}*/
    __declspec(dllimport) virtual Bedrock::NonOwnerPointer<NetEventCallback> getNetEventCallback() const;

    /**@vIndex {284}*/
    __declspec(dllimport) virtual void setNetEventCallback(Bedrock::NonOwnerPointer<NetEventCallback>);

    /**@vIndex {285}*/
    __declspec(dllimport) virtual void _unknown_285();

    /**@vIndex {286}*/
    __declspec(dllimport) virtual void _unknown_286();

    /**@vIndex {287}*/
    __declspec(dllimport) virtual void _unknown_287();

    /**@vIndex {288}*/
    __declspec(dllimport) virtual HitResult& getHitResult();

    /**@vIndex {289}*/
    __declspec(dllimport) virtual HitResult& getLiquidHitResult();

    /**@vIndex {290}*/
    __declspec(dllimport) virtual const std::string& getImmersiveReaderString() const;

    /**@vIndex {291}*/
    __declspec(dllimport) virtual void setImmersiveReaderString(std::string);

    /**@vIndex {292}*/
    __declspec(dllimport) virtual const AdventureSettings& getAdventureSettings() const;

    /**@vIndex {293}*/
    __declspec(dllimport) virtual AdventureSettings& getAdventureSettings();

    /**@vIndex {294}*/
    __declspec(dllimport) virtual GameRules& getGameRules();

    /**@vIndex {295}*/
    __declspec(dllimport) virtual const GameRules& getGameRules() const;

    /**@vIndex {296}*/
    __declspec(dllimport) virtual bool hasStartWithMapEnabled() const;

    /**@vIndex {297}*/
    __declspec(dllimport) virtual bool isEditorWorld() const;

    /**@vIndex {298}*/
    __declspec(dllimport) virtual Abilities& getDefaultAbilities();

    /**@vIndex {299}*/
    __declspec(dllimport) virtual const PermissionsHandler& getDefaultPermissions() const;

    /**@vIndex {300}*/
    __declspec(dllimport) virtual PermissionsHandler& getDefaultPermissions();

    /**@vIndex {301}*/
    __declspec(dllimport) virtual bool getTearingDown() const;

    /**@vIndex {302}*/
    __declspec(dllimport) virtual void takePicture(cg::ImageBuffer&, Actor*, Actor*, ScreenshotOptions&);

    /**@vIndex {303}*/
    __declspec(dllimport) virtual gsl::not_null<Bedrock::NonOwnerPointer<LevelSoundManager>> getLevelSoundManager();

    /**@vIndex {304}*/
    __declspec(dllimport) virtual gsl::not_null<Bedrock::NonOwnerPointer<SoundPlayerInterface>> getSoundPlayer() const;

    /**@vIndex {305}*/
    __declspec(dllimport) virtual void setSimPaused(bool);

    /**@vIndex {306}*/
    __declspec(dllimport) virtual bool getSimPaused();

    /**@vIndex {307}*/
    __declspec(dllimport) virtual void setFinishedInitializing();

    /**@vIndex {308}*/
    __declspec(dllimport) virtual LootTables& getLootTables();

    /**@vIndex {309}*/
    __declspec(dllimport) virtual void updateWeather(float, int, float, int);

    /**@vIndex {310}*/
    __declspec(dllimport) virtual int getNetherScale() const;

    /**@vIndex {311}*/
    __declspec(dllimport) virtual void _unknown_311();

    /**@vIndex {312}*/
    __declspec(dllimport) virtual void _unknown_312();

    /**@vIndex {313}*/
    __declspec(dllimport) virtual void _unknown_313();

    /**@vIndex {314}*/
    __declspec(dllimport) virtual LayeredAbilities* getPlayerAbilities(const ActorUniqueID&);

    /**@vIndex {315}*/
    __declspec(dllimport) virtual void setPlayerAbilities(const ActorUniqueID&, const LayeredAbilities&);

    /**@vIndex {316}*/
    __declspec(dllimport) virtual void sendAllPlayerAbilities(const Player&);

    /**@vIndex {317}*/
    __declspec(dllimport) virtual Recipes& getRecipes() const;

    /**@vIndex {318}*/
    __declspec(dllimport) virtual BlockReducer* getBlockReducer() const;

    /**@vIndex {319}*/
    __declspec(dllimport) virtual void _unknown_319();

    /**@vIndex {320}*/
    __declspec(dllimport) virtual void _unknown_320();

    /**@vIndex {321}*/
    __declspec(dllimport) virtual void _unknown_321();

    /**@vIndex {322}*/
    __declspec(dllimport) virtual void _unknown_322();

    /**@vIndex {323}*/
    __declspec(dllimport) virtual void digestServerItemComponents(const ItemComponentPacket&);

    /**@vIndex {324}*/
    __declspec(dllimport) virtual const BlockLegacy& getRegisteredBorderBlock() const;

    /**@vIndex {325}*/
    __declspec(dllimport) virtual bool use3DBiomeMaps() const;

    /**@vIndex {326}*/
    __declspec(dllimport) virtual void addBlockSourceForValidityTracking(BlockSource*);

    /**@vIndex {327}*/
    __declspec(dllimport) virtual void removeBlockSourceFromValidityTracking(BlockSource*);

    /**@vIndex {328}*/
    __declspec(dllimport) virtual Level* asLevel();

    /**@vIndex {329}*/
    __declspec(dllimport) virtual void _unknown_329();

    /**@vIndex {330}*/
    __declspec(dllimport) virtual bool isClientSideGenerationEnabled();

    /**@vIndex {331}*/
    __declspec(dllimport) virtual bool blockNetworkIdsAreHashes();

    /**@vIndex {332}*/
    __declspec(dllimport) virtual ItemRegistryRef getItemRegistry() const;

    /**@vIndex {333}*/
    __declspec(dllimport) virtual std::weak_ptr<BlockTypeRegistry> getBlockRegistry() const;

    /**@vIndex {334}*/
    __declspec(dllimport) virtual void pauseAndFlushTaskGroups();

    /**@vIndex {335}*/
    __declspec(dllimport) virtual void _unknown_335();

    /**@vIndex {336}*/
    __declspec(dllimport) virtual void _unknown_336();

    /**@vIndex {337}*/
    __declspec(dllimport) virtual void _unknown_337();

    /**@vIndex {338}*/
    __declspec(dllimport) virtual void _unknown_338();

    /**@vIndex {339}*/
    __declspec(dllimport) virtual void _unknown_339();

protected:
    /**@vIndex {340}*/
    __declspec(dllimport) virtual void _subTick();

    /// @VirtualIndex {0, this}
    __declspec(dllimport) virtual void _initializeMapDataManager();
};