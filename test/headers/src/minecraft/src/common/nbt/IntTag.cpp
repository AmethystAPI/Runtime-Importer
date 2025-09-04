#include <sstream>
#include "minecraft/src/common/nbt/IntTag.hpp"
#include "minecraft/src/common/util/DataIO.hpp"

IntTag::IntTag() : Tag(), data(0) {}

IntTag::IntTag(int data) : Tag(), data(data) {}

void IntTag::write(IDataOutput& dos) const
{
    dos.writeInt(data);
}

void IntTag::load(IDataInput& dis)
{
    data = dis.readInt();
}

Tag::Type IntTag::getId() const
{
    return Type::Int;
}

std::string IntTag::toString() const
{
    std::stringstream ss;
    ss << data;
    return ss.str();
}

std::unique_ptr<Tag> IntTag::copy() const
{
    return std::make_unique<IntTag>(data);
}

bool IntTag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        return data == static_cast<const IntTag&>(rhs).data;
    }
    else {
        return false;
    }
}

size_t IntTag::hash() const
{
    return std::hash<int>{}(data);
}