#pragma once
#include <string> 
#include <vector>
#include <functional>
#include <minecraft/src-deps/input/InputHandler.hpp>
#include <minecraft/src-client/common/client/game/IClientInstance.hpp>

namespace Amethyst {

/*
 * Defines the context in which a keybind is active.
 * E.g. `exampleKeybindContext::Gameplay | KeybindContext::Screen`
 */
enum class KeybindContext {
    None = 0,
    Gameplay = 1 << 0,
    //Screen = 1 << 1,
};

inline KeybindContext operator|(KeybindContext lhs, KeybindContext rhs)
{
    using T = std::underlying_type_t<KeybindContext>;
    return static_cast<KeybindContext>(
        static_cast<T>(lhs) | static_cast<T>(rhs));
}

inline KeybindContext operator&(KeybindContext lhs, KeybindContext rhs)
{
    using T = std::underlying_type_t<KeybindContext>;
    return static_cast<KeybindContext>(
        static_cast<T>(lhs) & static_cast<T>(rhs));
}

class InputActionOptions {
public:
    std::string mActionName;
    std::vector<int> mDefaultKeys;
    bool mAllowRemapping;
    KeybindContext mContexts;

    InputActionOptions(
        const std::string& actionName, 
        std::vector<int> defaultKeys,
        bool allowRemapping = true,
        KeybindContext contexts = KeybindContext::Gameplay
    )
        : mActionName(actionName),
          mDefaultKeys(std::move(defaultKeys)),
          mAllowRemapping(allowRemapping),
          mContexts(contexts) {}
};

class InputAction {
private:
    uint32_t mNameHash;
    std::vector<std::function<void(FocusImpact, IClientInstance&)>> mButtonDownHandlers;
    std::vector<std::function<void(FocusImpact, IClientInstance&)>> mButtonUpHandlers;

public:
    InputAction(const std::string& actionName);
    uint32_t getNameHash() const { return mNameHash; }
    void addButtonDownHandler(std::function<void(FocusImpact, IClientInstance&)> handler);
    void addButtonUpHandler(std::function<void(FocusImpact, IClientInstance&)> handler);

private:
    void _onButtonStateChange(ButtonState state, FocusImpact focus, IClientInstance& client) const;
    friend class InputManager;
};

}; // namespace Amethyst