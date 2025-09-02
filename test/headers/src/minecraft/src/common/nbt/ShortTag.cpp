#include "minecraft/src/common/nbt/ShortTag.hpp"
#include <sstream>
#include "minecraft/src/common/util/DataIO.hpp"
#include "minecraft/src-deps/core/math/Math.hpp"

ShortTag::ShortTag() : Tag(), data(0) {}

ShortTag::ShortTag(short data) : Tag(), data(data) {}

void ShortTag::write(IDataOutput& dos) const
{
    dos.writeShort(data);
}

void ShortTag::load(IDataInput& dis)
{
    data = dis.readShort();
}

Tag::Type ShortTag::getId() const
{
    return Type::Short;
}

std::string ShortTag::toString() const
{
    std::stringstream ss;
    ss << data;
    return ss.str();
}

std::unique_ptr<Tag> ShortTag::copy() const
{
    return std::make_unique<ShortTag>(data);
}

bool ShortTag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        auto& o = static_cast<const ShortTag&>(rhs);
        return data == o.data;
    }
    else {
        return false;
    }
}

size_t ShortTag::hash() const
{
    return std::hash<short>{}(data);
}