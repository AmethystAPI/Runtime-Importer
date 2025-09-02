#pragma once
#include <cstdint>

enum class InputMode : uint32_t {
    Undefined = 0x0000,
    Mouse = 0x0001,
    Touch = 0x0002,
    GamePad = 0x0003,
    MotionController = 0x0004,
    Count = 0x0005,
};