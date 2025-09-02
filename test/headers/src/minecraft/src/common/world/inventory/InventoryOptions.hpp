#pragma once

enum class InventoryLayout : int {
    None       = 0,
    Survival   = 1,
    RecipeBook = 2,
    Creative   = 3,
    Count      = 4,
};

enum class InventoryRightTabIndex : int {
    None       = 0,
    FullScreen = 1,
    Crafting   = 2,
    Armor      = 3,
    Count      = 4,
};

enum class InventoryLeftTabIndex : int {
    None               = 0,
    RecipeConstruction = 1,
    RecipeEquipment    = 2,
    RecipeItems        = 3,
    RecipeNature       = 4,
    RecipeSearch       = 5,
    Survival           = 6,
    Count              = 7,
};

class InventoryOptions {
public:
    InventoryLeftTabIndex mLeftInventoryTab;
    InventoryRightTabIndex mRightInventoryTab;
    bool mFiltering;
    InventoryLayout mLayoutInv;
    InventoryLayout mLayoutCraft;
};