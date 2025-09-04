#pragma once
#include <unordered_map>
#include <vector>
#include "minecraft/src/common/world/inventory/transaction/InventorySource.hpp"
#include "minecraft/src/common/world/inventory/transaction/InventoryTransactionItemGroup.hpp"
#include "minecraft/src/common/world/inventory/transaction/InventoryAction.hpp"


class InventoryTransaction {
public:
    std::unordered_map<InventorySource, std::vector<InventoryAction>> mActions;
    std::vector<InventoryTransactionItemGroup> mContents;

    void addAction(const InventoryAction& action)
    {
        using function = decltype(&InventoryTransaction::addAction);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 74 24 ? 55 57 41 54 41 56 41 57 48 8D AC 24 ? ? ? ? 48 81 EC ? ? ? ? 4C 8B F2 4C 8B F9"));
        (this->*func)(action);
    }

    const std::vector<InventoryAction>& getActions(const InventorySource& source) const {
        // I would've just assumed it wouldve returned mActions, but the game does some shit with a static variable here
        using function = decltype(&InventoryTransaction::getActions);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 57 48 83 EC ? 65 48 8B 04 25 ? ? ? ? 48 8B D9 B9 ? ? ? ? 48 8B FA 4C 8B 00 42 8B 04 01 39 05 ? ? ? ? 7F ? 8B 17"));
        return (this->*func)(source);
    }
};