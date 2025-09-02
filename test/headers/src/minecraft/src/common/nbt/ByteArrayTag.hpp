#pragma once
#include "minecraft/src/common/nbt/Tag.hpp"
#include "minecraft/src/common/nbt/TagMemoryChunk.hpp"

class ByteArrayTag : public Tag {
public:
    TagMemoryChunk data;

    ByteArrayTag();
    ByteArrayTag(TagMemoryChunk);
    virtual Type getId() const;
    virtual std::string toString() const;
    virtual bool equals(const Tag&) const;
    virtual std::unique_ptr<Tag> copy() const;
    virtual void write(IDataOutput&) const;
    virtual void load(IDataInput&);
    virtual size_t hash() const;
};