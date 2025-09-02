#pragma once
#include <vector>
#include <map>
#include "minecraft/src-client/common/client/options/Options.hpp"
#include <minecraft/src-client/common/client/input/ClientInputHandler.hpp>
#include "amethyst/runtime/input/InputAction.hpp"
#include <minecraft/src-deps/input/InputHandler.hpp>

class VanillaClientInputMappingFactory;
class KeyboardInputMapping;
class MouseInputMapping;
class AmethystContext;

namespace Amethyst {
class InputManager {
public:
    InputManager(AmethystContext* amethyst);
    ~InputManager();

    InputAction& RegisterNewInput(const std::string& actionName, std::vector<int> defaultKeys, bool allowRemapping = true, KeybindContext context = KeybindContext::Gameplay);
    InputAction& GetVanillaInput(const std::string& actionName);

private:
    std::unordered_map<uint32_t, std::unique_ptr<InputAction>> mActions;
    std::vector<InputActionOptions> mCustomInputs;

private:
    AmethystContext* mAmethyst;
    friend class AmethystRuntime;

public:
    void _handleButtonEvent(InputHandler* handler, const ButtonEventData& button, FocusImpact focus, IClientInstance& client, int controllerId) const;
    void _registerKeyboardInputs(VanillaClientInputMappingFactory* inputs, KeyboardInputMapping* keyboard, MouseInputMapping* mouse, Amethyst::KeybindContext context);
};

}