#include <sstream>
#include "minecraft/src/common/util/DataIO.hpp"
#include "minecraft/src/common/nbt/IntArrayTag.hpp"
#include "minecraft/src-deps/core/math/Math.hpp"

IntArrayTag::IntArrayTag() : Tag(), data() {}

IntArrayTag::IntArrayTag(TagMemoryChunk data) : Tag(), data(std::move(data)) {}

Tag::Type IntArrayTag::getId() const
{
    return Type::IntArray;
}

std::string IntArrayTag::toString() const
{
    std::stringstream ss;
    ss << "[" << data.size() << " ints]";
    return ss.str();
}

bool IntArrayTag::equals(const Tag& rhs) const
{
    if (Tag::equals(rhs)) {
        auto& o = static_cast<const IntArrayTag&>(rhs);
        return o.data == data;
    }
    else {
        return false;
    }
}

std::unique_ptr<Tag> IntArrayTag::copy() const
{
    return std::make_unique<IntArrayTag>(data.copy());
}

void IntArrayTag::write(IDataOutput& dos) const
{
    dos.writeInt((int)data.size());
    for (int i = 0; i < data.size(); ++i) {
        dos.writeInt(reinterpret_cast<const int*>(data.data())[i]);
    }
}

constexpr size_t MaxIntArray = 4096;

void IntArrayTag::load(IDataInput& dis)
{
    int nSize = dis.readInt();
    if (nSize > 0) {
        size_t size = nSize;
        int* ptr = data.alloc<int>(std::min(MaxIntArray, size));

        for (int i = 0; i < size && dis.numBytesLeft() != 0; ++i) {
            if (i < data.size()) {
                ptr[i] = dis.readInt();
            }
            else {
                data.alloc<int>(std::min(data.size() + MaxIntArray, size));
            }
        }
    }
}

size_t IntArrayTag::hash() const
{
    size_t hash = 0;
    for (size_t i = 0; i < data.size(); ++i) {
        hash = mce::Math::hash_accumulate(hash, data.data()[i]);
    }
    return hash;
}