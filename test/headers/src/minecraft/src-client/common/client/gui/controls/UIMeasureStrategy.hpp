#pragma once
#include <string>
#include <minecraft/src-deps/core/headerIncludes/gsl_includes.hpp>
#include <minecraft/src-deps/core/utility/NonOwnerPointer.hpp>
#include <vector>

class TextMeasureData;
class CaretMeasureData;
class FontHandle;

struct MeasureResult {
    std::string mFormattedText;
    glm::tvec2<float> mSize;
    std::vector<int> mCaretOffsets;
    float mCaretHeightOffset;
    float mLineHeight;
    bool mUsingEllipses;
};

class UIMeasureStrategy {
public:
    virtual MeasureResult measureText(const gsl::not_null<Bedrock::NonOwnerPointer<const FontHandle>>& font, const std::string& text, int32_t maxWidth, int32_t maxHeight, const TextMeasureData& textData, const CaretMeasureData& caretData) const;
    virtual MeasureResult measureTextHeight(const gsl::not_null<Bedrock::NonOwnerPointer<const FontHandle>>&, const std::string&, int32_t, const TextMeasureData&, const CaretMeasureData&) const;
    virtual MeasureResult measureTextWidth(const gsl::not_null<Bedrock::NonOwnerPointer<const FontHandle>>&, const std::string&, const TextMeasureData&, const CaretMeasureData&) const;
    virtual std::string filterProfanityFromText(const std::string&, const bool) const;
};