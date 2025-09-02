#pragma once
#include "minecraft/src/common/nbt/Tag.hpp"
#include <memory>
#include <vector>

class CompoundTag;

class ListTag final : public Tag {
public:
    std::vector<std::unique_ptr<Tag>> mList;
    Tag::Type mType;

    ListTag();

    int size() const;
    Tag* get(int index) const;

    const CompoundTag* getCompound(size_t index) const;

    virtual void write(IDataOutput& output) const override;
    virtual void load(IDataInput& input) override;
    virtual std::string toString() const override;
    virtual Tag::Type getId() const override;
    virtual std::unique_ptr<Tag> copy() const override;
    virtual void print(const std::string& prefix_, PrintStream& out) const override;
    virtual size_t hash() const;
};