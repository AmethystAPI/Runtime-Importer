#pragma once
#include <minecraft/src/common/CommonTypes.hpp>
class Block;

class BlockPalette {
public:
    enum class PaletteType : int {
        Sequential = 0,
        Hashed     = 1,
    };

public:
    virtual ~BlockPalette();
    virtual BlockPalette::PaletteType getPaletteType();
    virtual void appendBlock(const Block& blockState);
    virtual const Block& getBlock(const BlockRuntimeId& networkId) const;
};