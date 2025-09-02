#include "InputAction.hpp"
#include <minecraft/src-deps/input/InputHandler.hpp>

Amethyst::InputAction::InputAction(const std::string& actionName) 
	: mNameHash(StringToNameId("button." + actionName)), mButtonDownHandlers(), mButtonUpHandlers() {}

void Amethyst::InputAction::addButtonDownHandler(std::function<void(FocusImpact, IClientInstance&)> handler)
{
    mButtonDownHandlers.push_back(handler);
}

void Amethyst::InputAction::addButtonUpHandler(std::function<void(FocusImpact, IClientInstance&)> handler)
{
	mButtonUpHandlers.push_back(handler);
}

void Amethyst::InputAction::_onButtonStateChange(ButtonState state, FocusImpact focus, IClientInstance& client) const
{
	if (state == ButtonState::Down) {
		for (const auto& handler : mButtonDownHandlers) {
			handler(focus, client);
		}
	}
	else if (state == ButtonState::Up) {
		for (const auto& handler : mButtonUpHandlers) {
			handler(focus, client);
		}
	}
}