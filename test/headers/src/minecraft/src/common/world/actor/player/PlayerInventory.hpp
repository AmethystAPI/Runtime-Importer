#pragma once
#include <functional>
#include "minecraft/src/common/world/item/ItemStack.hpp"

class Container;
class Inventory;
class HudContainerManagerModel;

// This is probably wrong in some way or outdated, from china
enum class ContainerID : unsigned char {
  CONTAINER_ID_NONE = 0xFF,
  CONTAINER_ID_INVENTORY = 0x0,
  CONTAINER_ID_FIRST = 0x1,
  CONTAINER_ID_LAST = 0x64,
  CONTAINER_ID_OFFHAND = 0x77,
  CONTAINER_ID_ARMOR = 0x78,
  CONTAINER_ID_SELECTION_SLOTS = 0x7A,
  CONTAINER_ID_PLAYER_ONLY_UI = 0x7C,
};

// yes these two funcs come before the destructors

class ContainerSizeChangeListener {
    virtual void containerSizeChanged(int);
    virtual ~ContainerSizeChangeListener() = default;
};

class ContainerContentChangeListener {
    virtual void containerContentChanged(int); 
    virtual ~ContainerContentChangeListener() = default;
};

class PlayerInventory : public ContainerSizeChangeListener, public ContainerContentChangeListener {
public:
    class SlotData {
    public:
        ContainerID mContainerId;
        int32_t mSlot;
    };

public:
    int32_t mSelected;
    ItemStack mInfiniteItem;
    ContainerID mSelectedContainerId;
    std::unique_ptr<Inventory> mInventory;
    std::vector<ItemStack> mComplexItems;
    std::weak_ptr<HudContainerManagerModel> mHudContainerManager;

    virtual ~PlayerInventory();
    virtual void createTransactionContext(
        std::function<void __cdecl(Container&, int, ItemStack const&, ItemStack const&)> callback,
        std::function<void __cdecl(void)> execute
    );

    const ItemStack& getSelectedItem() const;
    void setSelectedItem(const ItemStack& item);
};

//static_assert(offsetof(PlayerInventory, mInventory) == 224);