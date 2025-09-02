#include "UIComponent.hpp"
#include <minecraft/src-deps/core/utility/StringUtils.hpp>

ComponentReceiveActionType UIComponent::receive(const ScreenEvent&)
{
    return ComponentReceiveActionType();
}

ComponentReceiveActionType UIComponent::receive(VisualTree&, ScreenInputContext&, UIAnimationController&, const ScreenEvent&)
{
    return ComponentReceiveActionType::ConsumeEvent;
}

const std::string& UIComponent::getTextToSpeechComponentValue() const
{
    return Util::EmptyString;
}

bool UIComponent::isRenderableComponent()
{
    return false;
}