#pragma once
#include "minecraft/src/common/nbt/Tag.hpp"

class EndTag : public Tag {
public:
    EndTag() : Tag() {}

    virtual void load(IDataInput& dis) {}

    virtual void write(IDataOutput& dos) const {}

    virtual Type getId() const
    {
        return Type::End;
    }

    virtual std::string toString() const
    {
        return "END";
    }

    virtual std::unique_ptr<Tag> copy() const
    {
        return std::make_unique<EndTag>();
    }

    virtual bool equals(const Tag& rhs) const
    {
        return Tag::equals(rhs);
    }

    virtual size_t hash() const
    {
        return 0;
    }
};