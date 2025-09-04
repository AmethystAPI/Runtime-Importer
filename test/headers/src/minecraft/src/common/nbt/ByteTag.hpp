#pragma once
#include "minecraft/src/common/nbt/Tag.hpp"

class ByteTag : public Tag {
public:
    uint8_t data;

    ByteTag();
    ByteTag(uint8_t data);

    virtual void write(IDataOutput&) const override;
    virtual void load(IDataInput&) override;
    virtual Type getId() const override;
    virtual std::string toString() const override;
    virtual bool equals(const Tag&) const override;
    virtual std::unique_ptr<Tag> copy() const override;
    virtual size_t hash() const override;
};

