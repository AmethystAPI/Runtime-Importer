#pragma once
#include <string>
#include <memory>
#include <vector>
#include "glm/glm.hpp"
#include "minecraft/src/common/world/phys/Vec2.hpp"

class UIPropertyBag;
class UIComponent;

namespace ui {
enum class VisibilityFlag : uint32_t {
    None = 0x0000,
    Visible = 0x0001,
    VisibleInStackPanel = 0x0002,
    All = 0x0003,
};
}

class UIControl {
public:
    /* this + 0   */ uintptr_t** vtable;
    /* this + 8   */ std::byte padding8[16];
    /* this + 24  */ std::string mName; // +8 string size
    /* this + 56  */ glm::tvec2<float> mParentRelativePosition;
    /* this + 64  */ glm::tvec2<float> mSize;
    /* this + 72  */ glm::tvec2<float> mMinSize;
    /* this + 80  */ glm::tvec2<float> mMaxSize;
    /* this + 88  */ float mAlpha;
    /* this + 92  */ uint32_t mZOrder;
    /* this + 96  */ uint32_t mLayer;
    /* this + 100 */ Vec2 mClipOffset;

    bool mHover : 1;
    ui::VisibilityFlag mVisible : 2;

    bool mIsVisibleInTree : 1;
    bool mEnabled : 1;
    bool mAllAncestorsEnabled : 1;
    bool mSelected : 1; 
    bool mClipsChildren : 1;
    bool mAllowClipping : 1;
    bool mIsClipped : 1; 
    bool mEnableClippingScissorTest : 1;

    bool mIsTemplate : 1; 
    bool mPropagateAlpha : 1; 
    bool mDelayCollectionUpdate : 1; 
    bool mTextToSpeechTriggered : 1; 
    bool mCachedPositionDirty : 1; 
    glm::tvec2<float> mCachedPosition;
    /* this + 120 */ uint32_t mClipChangeEventId; 
    /* this + 128 */ std::unique_ptr<UIPropertyBag> mPropertyBag;
    /* this + 144 */ std::weak_ptr<UIControl> mParent;
    /* this + 160 */ std::vector<std::shared_ptr<UIControl>> mChildren; 
    /* this + 184 */ std::byte padding184[8];
    /* this + 192 */ std::vector<std::unique_ptr<UIComponent>> mComponents;

    std::string getPathedName();

    template <typename T>
    T* getComponent();

    template <typename T>
    void setComponent(std::unique_ptr<T> component);

    glm::tvec2<float> getPosition() {
        if (mCachedPositionDirty) _setCachedPosition();
        return mCachedPosition;
    }

    void _setCachedPosition() {
        glm::tvec2<float> pos(0.0f, 0.0f);

        if (!mParent.expired()) {
            pos = mParent.lock()->getPosition();
        }

        pos += mParentRelativePosition;
        mCachedPosition = pos;
        mCachedPositionDirty = false;
    }
};

//static_assert(offsetof(UIControl, mChildren) == 160);
