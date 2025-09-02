#pragma once
#include "minecraft/src-deps/core/file/Path.hpp"
#include "minecraft/src-deps/core/resource/ResourceHelper.hpp"
#include "minecraft/src/common/resources/DirectoryPackSource.hpp"

class PackSourceFactory {
public:
    virtual void unknown0();
    virtual void unknown1();
    virtual void unknown2();
    virtual void unknown3();
    virtual void unknown4();
    virtual void unknown5();
    virtual void unknown6();
    virtual void unknown7();

    /// @vIndex { 8 }
    virtual DirectoryPackSource& createDirectoryPackSource(const Core::Path& path, PackType type, PackOrigin origin, bool isDevDirectory);
};