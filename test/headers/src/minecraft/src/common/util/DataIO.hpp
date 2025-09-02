#pragma once
#include <string>
#include <minecraft/src-deps/core/headerIncludes/gsl_includes.hpp>

class IDataOutput {
public:
    virtual ~IDataOutput() = default;
    virtual void writeString(std::string_view) = 0;
    virtual void writeLongString(std::string_view) = 0;
    virtual void writeFloat(float) = 0;
    virtual void writeDouble(double) = 0;
    virtual void writeByte(char) = 0;
    virtual void writeShort(short) = 0;
    virtual void writeInt(int) = 0;
    virtual void writeLongLong(int64_t) = 0;
    virtual void writeBytes(const void*, size_t) = 0;
};

class IDataInput {
public:
    virtual ~IDataInput() = default;
    virtual std::string readString() = 0;
    virtual std::string readLongString() = 0;
    virtual float readFloat() = 0;
    virtual double readDouble() = 0;
    virtual char readByte() = 0;
    virtual short readShort() = 0;
    virtual int readInt() = 0;
    virtual int64_t readLongLong() = 0;
    virtual bool readBytes(void*, size_t) = 0;
    virtual size_t numBytesLeft() const = 0;
};