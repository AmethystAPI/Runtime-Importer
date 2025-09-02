#pragma once
class BaseActorRenderContext;
class Player;
class TextureTessellator;

enum ItemContextFlags : unsigned char {
    None = 0x0000,
    FirstPersonPass = 0x0001,
    WorldPass = 0x0002,
    UIPass = 0x0004,
    Item2D = 0x0008,
    Item3D = 0x0010,
    InHand = 0x0020,
    Glint = 0x0040,
    MultiColorTint = 0x0080,
};

class ItemInHandRenderer {
public:
    std::byte padding0[216];
    ItemStack mOffhandItem;
    //int32_t mSlot;
    //IClientInstance& mClient;
    //std::unique_ptr<TextureTessellator> mTextureTessellator;
    std::byte padding368[32];
    float mHeight;
    float mOldHeight;
    float mHeightOffHand;
    float mOldHeightOffHand;

    // 1.21.0.3 - 40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ? 48 81 EC ? ? ? ? 0F 29 B4 24 ? ? ? ? 48 8B 05 ? ? ? ? 48 33 C4 48 89 45 ? 44 88 4D
    void renderOffhandItem(BaseActorRenderContext& ctx, Player& player, ItemContextFlags flags);

    // 1.21.0.3 - 40 53 48 83 EC ? 48 8B 42 ? 48 8B DA 48 85 C0 74 ? 48 8B 00
    bool _canTessellateAsBlockItem(const ItemStack& item) const {
        using function = decltype(&ItemInHandRenderer::_canTessellateAsBlockItem);
        static auto func = std::bit_cast<function>(SigScan("40 53 48 83 EC ? 48 8B 42 ? 48 8B DA 48 85 C0 74 ? 48 8B 00"));
        return (this->*func)(item);
    }
};