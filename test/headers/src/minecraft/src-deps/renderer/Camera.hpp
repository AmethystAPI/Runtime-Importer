#pragma once
#include <cstdint>
#include "minecraft/src-deps/renderer/MatrixStack.hpp"

namespace mce {
    class Camera {
    public:
        /* this + 0   */ MatrixStack viewMatrixStack;
        /* this + 64  */ MatrixStack worldMatrixStack;
        /* this + 128 */ MatrixStack projectionMatrixStack;
        /* this + 192 */ glm::tmat4x4<float> mInverseViewMatrix;
        glm::tvec3<float> mRight;
        glm::tvec3<float> mUp;
        glm::tvec3<float> mForward;
        glm::tvec3<float> mPosition;
        /* this +  */ std::byte padding256[208];

        const glm::tvec3<float> getRightVector() const {
            return mRight;
        }

        const glm::tvec3<float> getUpVector() const {
            return mUp;
        }

        const glm::tvec3<float> getForwardVector() const {
            return mForward;
        }

        const glm::tvec3<float> getPosition() const {
            return mPosition;
        }
    };
};

static_assert(sizeof(mce::Camera) == 512);