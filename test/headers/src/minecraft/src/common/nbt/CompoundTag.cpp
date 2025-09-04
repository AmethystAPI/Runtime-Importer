#include "minecraft/src/common/nbt/CompoundTag.hpp"
#include "minecraft/src/common/nbt/CompoundTagVariant.hpp"
#include <sstream>
#include "minecraft/src/common/util/DataIO.hpp"
#include "minecraft/src-deps/core/math/Math.hpp"

CompoundTag::CompoundTag() {}

CompoundTag::CompoundTag(CompoundTag&& rhs) noexcept
{
    mTags = std::move(rhs.mTags);
}

CompoundTag& CompoundTag::operator=(CompoundTag&& rhs) noexcept
{
    mTags = std::move(rhs.mTags);
    return *this;
}

const Tag* CompoundTag::get(std::string_view name) const
{
    auto it = mTags.find(name);
    if (it != mTags.end()) {
        return it->second.get();
    }

    return nullptr;
}

template <typename T>
const T* CompoundTag::get(std::string_view name) const
{
    auto it = mTags.find(name);
    if (it != mTags.end()) {
        return it->second.get<T>();
    }

    return nullptr;
}

template <typename T>
T* CompoundTag::get(std::string_view name)
{
    auto it = mTags.find(name);
    if (it != mTags.end()) {
        return it->second.get<T>();
    }

    return nullptr;
}

Tag* CompoundTag::get(std::string_view name)
{
    auto it = mTags.find(name);
    if (it != mTags.end()) {
        return it->second.get();
    }

    return nullptr;
}

bool CompoundTag::contains(std::string_view name) const
{
    return get(name) != nullptr;
}

bool CompoundTag::contains(std::string_view name, Tag::Type type) const
{
    const Tag* tag = get(name);
    if (tag) {
        return tag->getId() == type;
    }

    return false;
}

void CompoundTag::clear()
{
    mTags.clear();
}

void CompoundTag::deepCopy(const CompoundTag& other)
{
    clear();
    for (auto& it : other.mTags) {
        mTags.emplace(it.first, it.second.copy());
    }
}

std::unique_ptr<CompoundTag> CompoundTag::clone() const
{
    auto newCompound = std::make_unique<CompoundTag>();
    newCompound->deepCopy(*this);
    return newCompound;
}

const ListTag* CompoundTag::getList(std::string_view name) const
{
    return get<ListTag>(name);
}

ListTag* CompoundTag::getList(std::string_view name)
{
    return get<ListTag>(name);
}

const ByteTag* CompoundTag::getByteTag(std::string_view name) const
{
    return get<ByteTag>(name);
}

ByteTag* CompoundTag::getByteTag(std::string_view name)
{
    return get<ByteTag>(name);
}

uint8_t CompoundTag::getByte(std::string_view name) const
{
    auto tag = getByteTag(name);
    if (tag) return tag->data;
    return 0;
}

void CompoundTag::putByte(std::string name, unsigned char value)
{
    ByteTag stringTag(value);
    mTags[name].emplace<ByteTag>(stringTag);
}

const StringTag* CompoundTag::getStringTag(std::string_view name) const
{
    return get<StringTag>(name);
}

StringTag* CompoundTag::getStringTag(std::string_view name)
{
    return get<StringTag>(name);
}

const std::string* CompoundTag::getString(std::string_view name) const
{
    auto tag = getStringTag(name);
    if (tag) {
        return &tag->data;
    }
    else {
        return nullptr;
    }
}

void CompoundTag::putString(std::string name, std::string value) {
    StringTag stringTag(value);
    mTags[name].emplace<StringTag>(stringTag);
}

const IntTag* CompoundTag::getIntTag(std::string_view name) const {
    return get<IntTag>(name);
}

IntTag* CompoundTag::getIntTag(std::string_view name) {
    return get<IntTag>(name);
}

int CompoundTag::getInt(std::string_view name) const {
    auto tag = getIntTag(name);
    if (tag) return tag->data;
    return 0;
}

Tag& CompoundTag::put(std::string name, Tag&& tag)
{
    return mTags[name].emplace(std::move(tag));
}

Tag* CompoundTag::put(std::string name, std::unique_ptr<Tag> tag)
{
    if (tag) {
        return &CompoundTag::put(name, std::move(*tag));
    }

    return nullptr;
}

void CompoundTag::write(IDataOutput& dos) const
{
    for (auto& name : mTags) {
        Tag::writeNamedTag(name.first, *name.second, dos);
    }

    dos.writeByte(static_cast<char>(Type::End));
}

void CompoundTag::load(IDataInput& dis)
{
    mTags.clear();
    std::unique_ptr<Tag> tag;
    std::string name;

    while (true) {
        if (dis.numBytesLeft()) {
            tag = Tag::readNamedTag(dis, name);
            if (tag && (tag->getId() == Type::End)) {
                break;
            }
        }

        put(name, std::move(tag));
    }
}

std::string CompoundTag::toString() const
{
    std::stringstream ss;
    ss << mTags.size() << " entries";
    return ss.str();
}

Tag::Type CompoundTag::getId() const
{
    return Type::Compound;
}

std::unique_ptr<Tag> CompoundTag::copy() const
{
    return clone();
}

void CompoundTag::print(const std::string&, PrintStream&) const
{
}

size_t CompoundTag::hash() const
{
    size_t hash = 0;

    for (auto& [name, tag] : mTags) {
        hash = mce::Math::hash_accumulate(hash, name);
        hash = mce::Math::hash_accumulate(hash, tag->hash());
    }

    return hash;
}

bool CompoundTag::equals(const Tag& obj) const
{
    if (Tag::equals(obj)) {
        // not sure if this is right but the get call is non-const
        auto& o = static_cast<CompoundTag&>(const_cast<Tag&>(obj));

        if (mTags.size() == o.mTags.size()) {
            for (auto it = mTags.begin(); it != mTags.end(); ++it) {
                Tag* other = o.get(it->first);
                if (other) {
                    if (other->getId() != it->second->getId()) {
                        return false;
                    }

                    if (!it->second->equals(*other)) {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    return false;
}