#pragma once
#include <unordered_map>
#include <string>
#include "minecraft/src/common/world/containers/ContainerEnumName.hpp"

#pragma pack(push, 1)
class ContainerScreenController /** : ClientInstanceScreenController, MinecraftScreenController, ScreenController, IScreenController, std::enable_shared_from_this<MinecraftScreenController> **/ {
public:
    static std::unordered_map<ContainerEnumName, std::string>& ContainerCollectionNameMap;
};
#pragma pack(pop)