#pragma once
#include "minecraft/src/common/nbt/Tag.hpp"

class DoubleTag : public Tag {
public:
    double data;

    DoubleTag();
    DoubleTag(double);
    virtual void write(IDataOutput&) const;
    virtual void load(IDataInput&);
    virtual Type getId() const;
    virtual std::string toString() const;
    virtual std::unique_ptr<Tag> copy() const;
    virtual bool equals(const Tag&) const;
    virtual size_t hash() const;
};