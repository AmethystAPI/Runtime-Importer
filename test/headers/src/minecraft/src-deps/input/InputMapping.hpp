#pragma once
#include "minecraft/src-deps/input/InputMapping.hpp"
#include <string>
#include <vector>
#include <unordered_map>
#include <unordered_set>

struct KeyboardKeyBinding {
public:
    std::string buttonName;
    int32_t keyNum;
    FocusImpact focusImpact;

    KeyboardKeyBinding(const std::string* buttonName, int32_t key, FocusImpact focus)
    {
        this->buttonName = *buttonName;
        this->keyNum = key;
        this->focusImpact = focus;
    }
};

class KeyboardInputMapping {
public:
    std::vector<KeyboardKeyBinding> keyBindings;
};

class MouseButtonBinding {
    std::string buttonName;
    int buttonNum;

public:
    MouseButtonBinding(const std::string* name, int key)
    {
        buttonName = *name;
        buttonNum = key;
    }
};

class MouseInputMapping {
public:
    std::vector<MouseButtonBinding> buttonBindings;
};

struct InputMapping {
public:
    KeyboardInputMapping keyboardMapping;
    std::unordered_map<std::string, std::unordered_set<int>> unknown24;
    MouseInputMapping mouseMapping;
    // there r a bunch more
};