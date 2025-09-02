#include <sstream>
#include "minecraft/src/common/nbt/Int64Tag.hpp"
#include "minecraft/src/common/util/DataIO.hpp"

Int64Tag::Int64Tag() : Tag(), data(0) {}

Int64Tag::Int64Tag(int64_t data) : Tag(), data(data) {}

void Int64Tag::write(IDataOutput& dos) const
{
    dos.writeLongLong(data);
}

void Int64Tag::load(IDataInput& dis)
{
    data = dis.readLongLong();
}

Tag::Type Int64Tag::getId() const
{
    return Type::Int64;
}

std::string Int64Tag::toString() const
{
    std::stringstream ss;
    ss << data;
    return ss.str();
}

std::unique_ptr<Tag> Int64Tag::copy() const
{
    return std::make_unique<Int64Tag>(data);
}

bool Int64Tag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        auto& o = static_cast<const Int64Tag&>(rhs);
        return data == o.data;
    }
    else {
        return false;
    }
}

size_t Int64Tag::hash() const
{
    return std::hash<uint64_t>{}(data);
}