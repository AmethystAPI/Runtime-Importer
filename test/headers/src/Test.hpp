#pragma once
class ClassA {
public:
    virtual void FunctionA1(); // 0 of this
    virtual void FunctionA2(); // 1 of this
    virtual void FunctionA3(); // 2 of this
    virtual void FunctionA4(); // 3 of this
    void NoVirtualA6();
    virtual void FunctionA5(); // 4 of this
};

class ClassB {
public:
    virtual void FunctionB1(); // 0 of this
    virtual void FunctionB2(); // 1 of this
    virtual void FunctionB3(); // 2 of this
    virtual void FunctionB4(); // 3 of this
    virtual void FunctionB5(); // 4 of this
};

class ClassC : public ClassA {
public:
    virtual void FunctionC1(); // 5 of this
    virtual void FunctionA3(); // 2 of this
    virtual void FunctionC2(); // 6 of this
};

class NoVirtual {

};

class ClassD : public ClassC, public NoVirtual, public ClassB {
public:
    virtual void FunctionC2(); // 6 of vtable for ClassC
    virtual void FunctionB4(); // 3 of vtable for ClassB
    virtual void FunctionD1(); // 0 of vtable for this
};