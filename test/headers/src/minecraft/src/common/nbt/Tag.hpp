#pragma once
#include <memory>
#include <cstdint>
#include <string>

class IDataOutput;
class IDataInput;
class PrintStream;

class Tag {
public:
    enum Type : unsigned char {
        End = 0x0000,
        Byte = 0x0001,
        Short = 0x0002,
        Int = 0x0003,
        Int64 = 0x0004,
        Float = 0x0005,
        Double = 0x0006,
        ByteArray = 0x0007,
        String = 0x0008,
        List = 0x0009,
        Compound = 0x000a,
        IntArray = 0x000b,
    };

    static const std::string NullString;
    static const int TAGERR_OUT_OF_BOUNDS;
    static const int TAGERR_BAD_TYPE;

    virtual ~Tag() = default;
    virtual void deleteChildren() {};
    virtual void write(IDataOutput&) const = 0;
    virtual void load(IDataInput&) = 0;
    virtual std::string toString() const = 0;
    virtual Tag::Type getId() const = 0;
    virtual bool equals(const Tag&) const;
    virtual void print(PrintStream&) const;
    virtual void print(const std::string&, PrintStream&) const;
    virtual std::unique_ptr<Tag> copy() const = 0;
    virtual size_t hash() const = 0;

    static std::unique_ptr<Tag> readNamedTag(IDataInput&, std::string&);
    static void writeNamedTag(const std::string&, const Tag&, IDataOutput&);
    static std::unique_ptr<Tag> newTag(Type);
    static std::string getTagName(Type);

protected:
    Tag();
};