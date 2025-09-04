#pragma once
#include "minecraft/src/common/nbt/Tag.hpp"
#include <cstdint>

class Int64Tag : public Tag {
public:
    int64_t data;

    Int64Tag();
    Int64Tag(int64_t);
    virtual void write(IDataOutput&) const;
    virtual void load(IDataInput&);
    virtual Type getId() const;
    virtual std::string toString() const;
    virtual std::unique_ptr<Tag> copy() const;
    virtual bool equals(const Tag&) const;
    virtual size_t hash() const;
};