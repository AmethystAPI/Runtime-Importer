#pragma once
#include <functional>
#include <string>
#include <map>
#include <unordered_map>
#include <unordered_set>
#include <vector>
#include <set>
#include <cstdint>
#include <minecraft/src-deps/core/utility/StringUtils.hpp>
#include <minecraft/src-deps/input/InputMode.hpp>

enum class FocusImpact : char {
    Neutral = 0x0,
    ActivateFocus = 0x1,
    DeactivateFocus = 0x2,
};

enum class InputBindingMode : uint32_t {
    Undefined = 0x0000,
    MouseAndKeyboard = 0x0001,
    Gamepad = 0x0002,
    MotionController = 0x0003,
};

enum class ButtonState {
    Up,
    Down,
    None
};

struct ButtonEventData {
    uint32_t id;
    ButtonState state;
    bool exlcusive;
    int unknown12;
};

class ClientInstance;
using IClientInstance = ClientInstance;
class BindingFactoryMap;
class InputMappingFactoryMap;

int StringToNameId(const std::string& str);

class InputHandler {
public:
    struct InputHandlerState {
        InputMode mCurrentInputMode;
        InputBindingMode mCurrentBindingMode;
        std::vector<std::string> mInputMappingStack;
        std::unordered_set<unsigned int> mButtonsDown;
        std::unordered_set<int> mTriggeredChordsSources;
        bool mSuspendInput;
        bool mDisableInput;
    };

    /* this + 0   */ std::vector<std::function<void(enum FocusImpact, IClientInstance&)>> mInputModeHandlers;
    /* this + 24  */ std::multimap<unsigned int, std::pair<bool, std::function<void(enum FocusImpact, IClientInstance &)>>> mButtonDownHandlerMap;
    /* this + 40  */ std::multimap<unsigned int, std::pair<bool, std::function<void(enum FocusImpact, IClientInstance&)>>> mButtonUpHandlerMap;
    /* this + 56  */ std::byte padding64[680 - 56];
    /* this + 680 */ BindingFactoryMap& mBindingFactoryMap;
    /* this + 688 */ InputMappingFactoryMap& mInputMappingFactoryMap;
    /* this + 696 */ std::unordered_map<int, InputHandler::InputHandlerState> mPerIdState;

public:
    void RegisterButtonDownHandler(std::string buttonName, std::function<void(FocusImpact, IClientInstance&)> handler, bool suspendable);
    void RegisterButtonUpHandler(std::string buttonName, std::function<void(FocusImpact, IClientInstance&)> handler, bool suspendable);

    /*
     * Returns the InputHandlerState for the given controller ID 
     *  - This is not state of a pressed button for example.
     *  - This is essentially the context of where the current player is in, i.e. are they in the title screen and need the title screen specific actions
     */
    const InputHandlerState* getState(int id) const {
        auto it = mPerIdState.find(id);
        return it != mPerIdState.end() ? &it->second : nullptr;
    }

    const std::string& getCurrentInputMapping(int id) const {
        const InputHandlerState* state = getState(id);
        if (!state || state->mInputMappingStack.empty()) return Util::EmptyString;
        return state->mInputMappingStack.back();
    }
};