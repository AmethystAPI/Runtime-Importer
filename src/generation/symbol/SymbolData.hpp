#pragma once
#include <string>
#include <optional>

enum class SymbolType {
	VirtualTableSymbol,
	VirtualFunctionSymbol,
	FunctionSymbol
};

class SymbolData {
public:
	SymbolType Type;
	std::string Name;

	std::optional<uintptr_t> Address;
	std::optional<std::string> Signature;
	std::optional<std::string> VirtualTableTarget;
	std::optional<int> VirtualIndex;
};