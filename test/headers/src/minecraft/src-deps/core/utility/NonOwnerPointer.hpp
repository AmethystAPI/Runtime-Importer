#pragma once
#include <memory>

namespace Bedrock {
class EnableNonOwnerReferences {
public:
    struct ControlBlock {
        EnableNonOwnerReferences* ptr;

        explicit ControlBlock(EnableNonOwnerReferences* ptr) : ptr(ptr) {}
    };

public:
    std::shared_ptr<ControlBlock> mControlBlock;

public:
    EnableNonOwnerReferences() : mControlBlock(std::make_shared<ControlBlock>(this)) {}

    EnableNonOwnerReferences(const EnableNonOwnerReferences& other) = default;

    virtual ~EnableNonOwnerReferences() = default;

    EnableNonOwnerReferences& operator=(const EnableNonOwnerReferences& other) = default;
};

template <typename T = EnableNonOwnerReferences>
class NonOwnerPointer {
public:
    std::shared_ptr<EnableNonOwnerReferences::ControlBlock> mControlBlock;

public:
    NonOwnerPointer() : mControlBlock(nullptr) {}

    explicit NonOwnerPointer(T* ptr) : mControlBlock(ptr->controlBlock) {}

    NonOwnerPointer(const NonOwnerPointer<T>& other) : mControlBlock(other.mControlBlock) {}

    NonOwnerPointer& operator=(const NonOwnerPointer<T>& other)
    {
        this->mControlBlock = other.mControlBlock;
        return *this;
    }

    NonOwnerPointer& operator=(T* ptr)
    {
        this->mControlBlock = ptr->controlBlock;
        return *this;
    }

    T* operator->() const
    {
        return static_cast<T*>(this->mControlBlock.get()->ptr);
    }

    T& operator*() const
    {
        return *this->operator->();
    }

    bool operator==(void*) const
    {
        return this->mControlBlock == nullptr;
    }

    bool operator!=(void*) const
    {
        return this->mControlBlock != nullptr;
    }

    T* get() const
    {
        // Ensure T is derived from EnableNonOwnerReferences
        static_assert(std::is_base_of<EnableNonOwnerReferences, T>::value, "T must derive from EnableNonOwnerReferences");
        return static_cast<T*>(this->mControlBlock ? this->mControlBlock->ptr : nullptr);
    }
};

} // namespace Bedrock