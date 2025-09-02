#pragma once
#include <memory>

struct TagMemoryChunk {
protected:
    size_t mElements;
    size_t mSize;
    std::unique_ptr<std::byte[]> mBuffer;

public:
    template<typename T>
    T* alloc(size_t count) {
        mElements = count;
        mSize = sizeof(T) * mElements;

        if (mSize) {
            mBuffer = std::make_unique<std::byte[]>(count);
        }

        return reinterpret_cast<T*>(const_cast<std::byte*>(data()));
    }

    TagMemoryChunk() {
        mElements = 0;
        mSize = 0;
    }

    TagMemoryChunk(const TagMemoryChunk&) = default;
    TagMemoryChunk(TagMemoryChunk&&) = default;
    TagMemoryChunk& operator=(const TagMemoryChunk&) = default;
    TagMemoryChunk& operator=(TagMemoryChunk&&) = default;

    bool operator==(const TagMemoryChunk& rhs) const {
        if (mSize == rhs.mSize) {
            if (mSize == 0) {
                return true;
            }
            else {
                return memcmp(mBuffer.get(), rhs.mBuffer.get(), mSize) == 0;
            }
        }
        else {
            return false;
        }
    }

    bool operator!=(const TagMemoryChunk& rhs) const {
        return !(*this == rhs);
    }

    size_t size() const {
        return mElements;
    }
    
    const std::byte* data() const {
        return mBuffer.get();
    }

    TagMemoryChunk copy() const {
        TagMemoryChunk copy;
        copy.mElements = mElements;
        copy.mSize = mSize;
        
        if (mSize) {
            copy.mBuffer = std::make_unique<std::byte[]>(copy.mElements);
            memcpy(copy.mBuffer.get(), mBuffer.get(), mSize);
        }

        return copy;
    }
};