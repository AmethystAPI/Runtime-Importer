#pragma once
#include <memory>
#include <minecraft/src-deps/core/utility/NonOwnerPointer.hpp>

class FontRepository;
class Font;

class FontHandle : public Bedrock::EnableNonOwnerReferences {
public:
    Bedrock::NonOwnerPointer<FontRepository> mFontRepository;
    std::shared_ptr<Font> mDefaultFont;
    uint64_t mFontId;
    bool mIsDummyHandle;

    virtual ~FontHandle();
};