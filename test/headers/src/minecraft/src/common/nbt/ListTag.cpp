#include "minecraft/src/common/nbt/ListTag.hpp"
#include <sstream>
#include "minecraft/src/common/util/DataIO.hpp"
#include "amethyst/Log.hpp"
#include "minecraft/src-deps/core/math/Math.hpp"

int ListTag::size() const {
    return (int)mList.size();
}

Tag* ListTag::get(int index) const
{
    if (index >= size()) {
        return nullptr;
    }
    else {
        return mList[index].get();
    }
}

const CompoundTag* ListTag::getCompound(size_t index) const
{
    if (index < mList.size()) {
        auto& tag = mList[index];
        if (tag->getId() == Type::Compound) {
            return reinterpret_cast<const CompoundTag*>(tag.get());
        }
    }

    return nullptr;
}

ListTag::ListTag() : Tag()
{
    mType = Type::End;
}

void ListTag::write(IDataOutput& dos) const
{
    dos.writeByte((char)((!mList.empty()) ? mList.front()->getId() : Type::End));
    dos.writeInt((int)mList.size());
    for (auto& a : mList) {
        a->write(dos);
    }
}

void ListTag::load(IDataInput& dis)
{
    mType = static_cast<Type>(dis.readByte());
    int size = dis.readInt();

    mList.clear();
    if (size > 0 && mType != Type::End) {
        for (int i = 0; i < size && dis.numBytesLeft() != 0; ++i) {
            std::unique_ptr<Tag> tag = newTag(mType);
            if (tag) {
                tag->load(dis);
                mList.emplace_back(std::move(tag));
            }
            else {
                break;
            }
        }
    }
}

Tag::Type ListTag::getId() const
{
    return Type::List;
}

std::unique_ptr<Tag> ListTag::copy() const
{
    std::unique_ptr<ListTag> res = std::make_unique<ListTag>();
    res->mType = mType;

    for (auto it = mList.begin(); it != mList.end(); ++it) {
        res->mList.emplace_back((*it)->copy());
    }

    return res;
}

std::string ListTag::toString() const
{
    std::stringstream ss;
    ss << mList.size() << " entries of type " << Tag::getTagName(mType);
    return ss.str();
}

void ListTag::print(const std::string& prefix_, PrintStream& out) const
{
    Log::Info("ListTag::print not implemented");
   /* Tag::print(prefix_, out);
    std::string prefix = prefix_;

    out.print(prefix);
    out.println("{");
    prefix += "   ";
    for (auto it = mList.begin(); it != mList.end(); ++it) {
        (*it)->print(prefix, out);
    }
    out.print(prefix_);
    out.println("}");*/
}


size_t ListTag::hash() const
{
    size_t hash = 0;

    for (auto it = mList.begin(); it != mList.end(); ++it) {
        hash = mce::Math::hash_accumulate(hash, (*it)->hash());
    }

    return hash;
}