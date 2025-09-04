#include "minecraft/src/common/nbt/Tag.hpp"
#include "amethyst/Log.hpp"
#include "minecraft/src/common/util/DataIO.hpp"
#include "minecraft/src/common/nbt/EndTag.hpp"
#include "minecraft/src/common/nbt/CompoundTag.hpp"
#include "minecraft/src/common/nbt/CompoundTagVariant.hpp"
#include "minecraft/src/common/nbt/ShortTag.hpp"
#include "minecraft/src/common/nbt/Int64Tag.hpp"
#include "minecraft/src/common/nbt/FloatTag.hpp"
#include "minecraft/src/common/nbt/DoubleTag.hpp"
#include "minecraft/src/common/nbt/ByteArrayTag.hpp"
#include "minecraft/src/common/nbt/IntArrayTag.hpp"
#include "minecraft/src/common/nbt/ByteTag.hpp"
#include "minecraft/src/common/nbt/IntTag.hpp"
#include "minecraft/src/common/nbt/StringTag.hpp"
#include "minecraft/src/common/nbt/ListTag.hpp"

void Tag::writeNamedTag(const std::string& name, const Tag& tag, IDataOutput& dos)
{
    dos.writeByte((char)tag.getId());

    if (tag.getId() != Type::End) {
        dos.writeString(name);
        tag.write(dos);
    }
}

std::unique_ptr<Tag> Tag::newTag(Type type)
{
    switch (type) {
    case Type::End:
        return std::make_unique<EndTag>();

    case Type::Byte:
        return std::make_unique<ByteTag>();

    case Type::Short:
        return std::make_unique<ShortTag>();

    case Type::Int:
        return std::make_unique<IntTag>();

    case Type::Int64:
        return std::make_unique<Int64Tag>();

    case Type::Float:
        return std::make_unique<FloatTag>();

    case Type::Double:
        return std::make_unique<DoubleTag>();

    case Type::ByteArray:
        return std::make_unique<ByteArrayTag>();

    case Type::String:
        return std::make_unique<StringTag>();

    case Type::List:
        return std::make_unique<ListTag>();

    case Type::Compound:
        return std::make_unique<CompoundTag>();

    case Type::IntArray:
        return std::make_unique<IntArrayTag>();

    default:
        return nullptr;
    }
}

Tag::Tag() {}

bool Tag::equals(const Tag& rhs) const {
	return getId() == rhs.getId();
}

void Tag::print(PrintStream&) const
{
    Log::Info("Tag::print not implemented");
}

void Tag::print(const std::string&, PrintStream&) const
{
	Log::Info("Tag::print not implemented");
}

std::string Tag::getTagName(Type type)
{
    switch (type) {
    case Type::End:
        return "TAG_End";

    case Type::Byte:
        return "TAG_Byte";

    case Type::Short:
        return "TAG_Short";

    case Type::Int:
        return "TAG_Int";

    case Type::Int64:
        return "TAG_Long";

    case Type::Float:
        return "TAG_Float";

    case Type::Double:
        return "TAG_Double";

    case Type::ByteArray:
        return "TAG_Byte_Array";

    case Type::String:
        return "TAG_String";

    case Type::List:
        return "TAG_List";

    case Type::Compound:
        return "TAG_Compound";

    case Type::IntArray:
        return "TAG_Int_Array";

    default:
        return "UNKNOWN";
    }
}

std::unique_ptr<Tag> Tag::readNamedTag(IDataInput& dis, std::string& name)
{
    Type type = static_cast<Type>(dis.readByte());
    if (type == Type::End) {
        return std::make_unique<EndTag>();
    }
    else {
        name = dis.readString();

        std::unique_ptr<Tag> tag = newTag(type);
        if (tag) {
            tag->load(dis);
            return tag;
        }
        else {
            return nullptr;
        }
    }
}