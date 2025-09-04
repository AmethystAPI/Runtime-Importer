#include "minecraft/src/common/nbt/FloatTag.hpp"
#include "minecraft/src/common/util/DataIO.hpp"
#include <sstream>

FloatTag::FloatTag() : Tag(), data(0) {}

FloatTag::FloatTag(float data) : Tag(), data(data) {}

void FloatTag::write(IDataOutput& dos) const
{
    dos.writeFloat(data);
}

void FloatTag::load(IDataInput& dis)
{
    data = dis.readFloat();
}

Tag::Type FloatTag::getId() const
{
    return Type::Float;
}

std::string FloatTag::toString() const
{
    std::stringstream ss;
    ss << data;
    return ss.str();
}

std::unique_ptr<Tag> FloatTag::copy() const
{
    return std::make_unique<FloatTag>(data);
}

bool FloatTag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        auto& o = static_cast<const FloatTag&>(rhs);
        return data == o.data;
    }
    else {
        return false;
    }
}

size_t FloatTag::hash() const
{
    return std::hash<float>{}(data);
}