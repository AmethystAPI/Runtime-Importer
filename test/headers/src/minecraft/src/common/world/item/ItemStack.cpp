#include "minecraft/src/common/world/item/ItemStack.hpp"
#include "minecraft/src/common/world/item/Item.hpp"

ItemStack::ItemStack() : ItemStackBase() {
    
} 

InteractionResult ItemStack::useOn(Actor& actor, int x, int y, int z, FacingID face, const Vec3& hit) {
    using function = decltype(&ItemStack::useOn);
    static auto func = std::bit_cast<function>(SigScan("40 53 55 56 57 41 56 48 83 EC ? 48 8B 41 ? 41 8B E9"));
    return (this->*func)(actor, x, y, z, face, hit);
}

ItemStack ItemStack::fromDescriptor(const NetworkItemStackDescriptor& descriptor, BlockPalette& palette, bool isClientSide )
{
    // When using the games ctor, it doesn't seem to initialize the memory of itemstack, so this forces it

    using function = ItemStack* (*)(const NetworkItemStackDescriptor&, ItemStack*, BlockPalette&, bool);
    static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 6C 24 ? 48 89 74 24 ? 57 41 56 41 57 48 83 EC ? 44 8B 71 ? 49 8B E8 48 8B FA 48 8B D9 33 F6 E8 ? ? ? ? 48 8B CB 44 0F B7 F8 E8 ? ? ? ? 48 8B 4B ? 48 85 C9 74 ? 48 8B 11 48 8B 42 ? 48 8D 54 24 ? FF 15 ? ? ? ? 48 8B 30 48 8B CB E8 ? ? ? ? 48 8B 4B ? 48 85 C9 74 ? 48 8B 01 48 8D 54 24 ? 48 8B 40 ? FF 15 ? ? ? ? 0F B7 48 ? EB ? B9 ? ? ? ? 0F B7 53 ? 48 8D 43 ? 48 89 6C 24 ? 44 0F B7 C1 48 89 44 24 ? 4C 8B CE 44 89 74 24 ? 48 8B CF 66 44 89 7C 24 ? E8 ? ? ? ? 4C 8D 5C 24 ? 48 8B C7 49 8B 5B ? 49 8B 6B ? 49 8B 73 ? 49 8B E3 41 5F 41 5E 5F C3 CC CC CC 48 89 5C 24 ? 48 89 6C 24"));

    ItemStack stack;
    func(descriptor, &stack, palette, isClientSide);
    return stack;
}

int ItemStack::getMaxUseDuration() const
{
    return mItem && mItem->getMaxUseDuration(this);
}
