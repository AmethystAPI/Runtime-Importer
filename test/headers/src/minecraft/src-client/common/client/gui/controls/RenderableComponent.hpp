#pragma once
#include <minecraft/src-client/common/client/gui/controls/UIComponent.hpp>
#include <amethyst/Log.hpp>

class UIMeasureStrategy;

enum class LayoutVariableType : unsigned char {
    X = 0x0000,
    Y = 0x0001,
    Width = 0x0002,
    Height = 0x0003,
    XOffsetDelta = 0x0004,
    YOffsetDelta = 0x0005,
    XGridPosition = 0x0006,
    YGridPosition = 0x0007,
    XGridDimention = 0x0008,
    YGridDimention = 0x0009,
    StackPanelVisibility = 0x000a,
    Count = 0x000b,
    Unused = 0x0064,
};

class RenderableComponent : public UIComponent {
private:
    float mPropagatedAlpha;

public:
    RenderableComponent(UIControl& owner) : UIComponent(owner), mPropagatedAlpha(1.0f) {}
    virtual bool isRenderableComponent() override { return true; }
    virtual void updateUI(const UIMeasureStrategy&) {}
    virtual bool overridesLayoutAxisOffset(const LayoutVariableType) const { return false; }
    virtual float getLayoutAxisOffsetOverride(const LayoutVariableType) const {
        Assert(false, "This shouldn't get called yet"); // this is the actual message in the game not me being lazy.
        return 0.0f;
    }
    void setPropagatedAlpha(float alpha) { mPropagatedAlpha = alpha; }
    float getPropagatedAlpha() const { return mPropagatedAlpha; }
};