#pragma once
#include <memory>
#include <queue>
#include <minecraft/src-client/common/client/game/IClientInstance.hpp>
#include <minecraft/src-deps/core/resource/ResourceHelper.hpp>

class UIRenderContext;
class UIControl;
class UIScene;
class UIFrameUpdateContext;
class RectangleArea;
class ResourceLocation;
class ScreenEvent;
class MinecratUIFrameUpdate;

enum class UIBatchType : int32_t {
    Default = 0x0000,
    SharedMesh = 0x0001,
};

enum class UIMaterialType : int32_t {
    None = 0x0000,
    textured_and_glcolor_sprite = 0x0001,
    textured_and_glcolor_bilinear_sprite = 0x0002,
    grayscale_bilinear = 0x0003,
    inventory_item_glint = 0x0004,
    item_glint_stencil = 0x0005,
    item_unglint_stencil = 0x0006,
    banner = 0x0007,
    shield = 0x0008,
    dragon_head = 0x0009,
    mob_head = 0x000a,
    conduit = 0x000b,
    item = 0x000c,
    item_multicolor_tint = 0x000d,
    item_model = 0x000e,
    COUNT = 0x000f,
};

class UICustomRenderer {
public:
    float mPropagatedAlpha;
    UICustomRenderer() : mPropagatedAlpha(1.0f) {}

    virtual ~UICustomRenderer() = default;
    virtual void preRenderSetup(UIRenderContext&) {};
    virtual std::shared_ptr<UICustomRenderer> clone() const = 0;
    virtual bool update(IClientInstance&, UIControl&, const UIScene&) { return true; }
    virtual void frameUpdate(UIFrameUpdateContext& frameCtx, UIControl& control) {}
    virtual void render(UIRenderContext& renderCtx, IClientInstance& client, UIControl& control, int32_t unkn, RectangleArea& area) {}
    virtual UIBatchType getBatchType() const { return UIBatchType::Default; }
    virtual int32_t getCustomId() const { return 0; };
    virtual int32_t getNumRenderPasses() const { return 1; };
    virtual ResourceLocation getResourceLocation(int32_t, int32_t) const { return ResourceLocation(); };
    virtual UIMaterialType getUIMaterialType(int32_t) const { return UIMaterialType::None; };
    virtual bool getRequiresPreRenderSetup(int32_t) const { return false; };
    virtual void onVisibilityChanged(bool) {};
    virtual void collectScreenEvents(std::queue<ScreenEvent>&) {};
    virtual uint64_t getItemRenderInfo() const { return 0; };

    void setPropagatedAlpha(float newVal) { mPropagatedAlpha = newVal; };
};