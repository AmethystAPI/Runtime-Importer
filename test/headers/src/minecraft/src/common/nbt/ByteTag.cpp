#include "minecraft/src/common/nbt/ByteTag.hpp"
#include "minecraft/src/common/util/DataIO.hpp"

ByteTag::ByteTag() : Tag(), data(0) {}
ByteTag::ByteTag(uint8_t data) : Tag(), data(data) {}

void ByteTag::write(IDataOutput& output) const
{
    output.writeByte(data);
}

void ByteTag::load(IDataInput& input)
{
    data = input.readByte();
}

Tag::Type ByteTag::getId() const
{
    return Type::Byte;
}

std::string ByteTag::toString() const
{
    return std::string(1, data);
}

bool ByteTag::equals(const Tag& rhs) const 
{
    if (Tag::equals(rhs)) {
        return data == static_cast<const ByteTag&>(rhs).data;
    }
    else {
        return false;
    }
}

std::unique_ptr<Tag> ByteTag::copy() const
{
    return std::make_unique<ByteTag>(data);
}

size_t ByteTag::hash() const
{
    return std::hash<uint8_t>{}(data);
}