#include "minecraft/src/common/nbt/ByteArrayTag.hpp"
#include <sstream>
#include <algorithm>
#include "minecraft/src-deps/core/math/Math.hpp"
#include "minecraft/src/common/util/DataIO.hpp"

ByteArrayTag::ByteArrayTag() : Tag(), data() {}

ByteArrayTag::ByteArrayTag(TagMemoryChunk data) : Tag(), data(std::move(data)) {}

Tag::Type ByteArrayTag::getId() const
{
    return Type::ByteArray;
}

std::string ByteArrayTag::toString() const
{
    std::stringstream ss;
    ss << "[" << data.size() << " bytes]";
    return ss.str();
}

bool ByteArrayTag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        auto& o = static_cast<const ByteArrayTag&>(rhs);
        return o.data == data;
    }
    else {
        return false;
    }
}

std::unique_ptr<Tag> ByteArrayTag::copy() const
{
    return std::make_unique<ByteArrayTag>(data.copy());
}

void ByteArrayTag::write(IDataOutput& dos) const
{
    dos.writeInt(data.size());
    dos.writeBytes(data.data(), data.size());
}

void ByteArrayTag::load(IDataInput& dis)
{
    int size = dis.readInt();
    if (size > 0) {
        std::byte* ptr = data.alloc<std::byte>(std::min(static_cast<size_t>(size), dis.numBytesLeft()));
        dis.readBytes(ptr, data.size());
    }
}

size_t ByteArrayTag::hash() const
{
    size_t hash = 0;
    for (size_t i = 0; i < data.size(); ++i) {
        hash = mce::Math::hash_accumulate(hash, data.data()[i]);
    }
    return hash;
}