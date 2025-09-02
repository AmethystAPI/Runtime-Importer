#pragma once
#include "TextureUVCoordinateSet.hpp"
#include <minecraft/src-deps/core/resource/ResourceHelper.hpp>
#include <minecraft/src-client/common/client/renderer/texture/IsotropicFaceData.hpp>

class TextureAtlasItem {
public:
    float weight;
    float _u0;
    float _v0;
    float _u1;
    float _v1;
    unsigned __int16 _texSizeW;
    unsigned __int16 _texSizeH;
    ResourceLocation sourceFileLocation;
    IsotropicFaceData mIsotropicFaceData;
    __int16 textureSetTranslationIndex;
    unsigned __int16 mPBRTextureDataHandle;
};