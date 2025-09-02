#pragma once

template <class T>
class optional_ref {
protected:
    T* ptr;

public:
    optional_ref(T* p) : ptr(p) {}
};