#pragma once
#include <cstdint>
#include <memory>
#include <string>

class UIControl;
class ScreenEvent;
class ScreenInputContext;
class VisualTree;
class UIAnimationController;

enum class ComponentReceiveActionType : uint16_t {
    ConsumeEvent = 0x0000,
    FurtherPassEventToControls = 0x0001,
};

class UIComponent {
public:
    UIControl& mOwner;

public:
    UIComponent(UIControl& owner) : mOwner(owner) {}
    virtual ~UIComponent() = default;
    virtual std::unique_ptr<UIComponent> clone(UIControl& owner) = 0;
    virtual ComponentReceiveActionType receive(const ScreenEvent&);
    virtual ComponentReceiveActionType receive(VisualTree&, ScreenInputContext&, UIAnimationController&, const ScreenEvent&);
    virtual void onNotifyChildAdded() {};
    virtual void onNotifyChildRemoved() {};
    virtual void onRemoved() {};
    virtual void onAdded() {};
    virtual void onVisibilityChanged(bool) {};
    virtual void onEnabledChanged(bool) {};
    virtual bool isRenderableComponent();
    virtual bool onLayoutChange() { return false; };
    virtual void reset() = 0;
    virtual void reload(const UIComponent&) {};
    virtual const std::string& getTextToSpeechComponentValue() const;
};