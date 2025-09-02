#include "amethyst/Memory.hpp"
#include "minecraft/src-client/common/client/gui/screens/controllers/ContainerScreenController.hpp"

// 1.21.0.3 - 0x59D9150 - static ContainerScreenController::ContainerEnumNameMap ContainerScreenController::AllContainerEnumNames
std::unordered_map<ContainerEnumName, std::string>& ContainerScreenController::ContainerCollectionNameMap = 
	*reinterpret_cast<std::unordered_map<ContainerEnumName, std::string>*>(SlideAddress(0x59D9150));