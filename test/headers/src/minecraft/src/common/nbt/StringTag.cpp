#include <sstream>
#include "minecraft/src/common/nbt/StringTag.hpp"
#include "minecraft/src/common/util/DataIO.hpp"
#include "minecraft/src-deps/core/math/Math.hpp"

StringTag::StringTag() : Tag(), data() {}

StringTag::StringTag(std::string data) : Tag(), data(data) {}

void StringTag::write(IDataOutput& dos) const
{
    dos.writeString(data);
}

void StringTag::load(IDataInput& dis)
{
    data = dis.readString();
}

Tag::Type StringTag::getId() const
{
    return Type::String;
}

std::string StringTag::toString() const
{
    std::stringstream ss; // this is cursed
    ss << data;
    return ss.str();
}

std::unique_ptr<Tag> StringTag::copy() const
{
    return std::make_unique<StringTag>(data);
}

bool StringTag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        auto& o = static_cast<const StringTag&>(rhs);
        return data == o.data;
    }
    else {
        return false;
    }
}

size_t StringTag::hash() const
{
    return std::hash<std::string>{}(data);
}