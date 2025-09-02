#include "minecraft/src-client/common/client/gui/controls/CustomRenderComponent.hpp"
#include "minecraft/src-deps/core/resource/ResourceHelper.hpp"
#include "CustomRenderComponent.hpp"

UIBatchType CustomRenderComponent::getBatchType() const
{
    if (mRenderer)
        return mRenderer->getBatchType();

    return UIBatchType::Default;
}

int32_t CustomRenderComponent::getCustomId() const
{
    if (mRenderer) {
        return mRenderer->getCustomId();
    }

    return 0xFFFFFFFF;
}

int32_t CustomRenderComponent::getNumRenderPasses() const
{
    if (mRenderer) {
        return mRenderer->getNumRenderPasses();
    }

    return 1;
}

UIMaterialType CustomRenderComponent::getUIMaterialType(int32_t arg) const
{
    if (mRenderer) {
        return mRenderer->getUIMaterialType(arg);
    }

    return UIMaterialType::None;
}

ResourceLocation CustomRenderComponent::getResourceLocation(int32_t arg1, int32_t arg2) const
{
    if (mRenderer) {
        return mRenderer->getResourceLocation(arg1, arg2);
    }

    return ResourceLocation();
}

bool CustomRenderComponent::getRequiresPreRenderSetup(int32_t arg1) const
{
    if (mRenderer) {
        return mRenderer->getRequiresPreRenderSetup(arg1);
    }

    return false;
}

void CustomRenderComponent::collectScreenEvents(std::queue<ScreenEvent>& events)
{
    if (mRenderer) {
        mRenderer->collectScreenEvents(events);
    }
}
