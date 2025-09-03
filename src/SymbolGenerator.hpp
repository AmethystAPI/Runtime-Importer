#pragma once
#include "PathUtils.hpp"
#include <regex>

enum class Type {
	Function,
	VirtualFunction
};

struct SymbolDescriptor {
	std::string header;
	Type type = Type::Function;
	std::string name;
	std::string comment;
	int vtableIndex = -1;
};

struct ConcreteSymbol {
	std::string header;
	Type type = Type::Function;
	std::string name = "";
	std::string comment = "";
	bool usesSignature = false;
	std::string signature = "";
	uintptr_t address = 0;
	int vtableIndex = -1;
};

struct HeaderSymbols {
	std::string header;
	std::vector<ConcreteSymbol> symbols;
};

class SymbolGenerator {
public:
	static std::vector<HeaderSymbols> ParseAllSymbolDescriptors(const std::vector<SymbolDescriptor>& descriptors) {
		std::unordered_map<std::string, HeaderSymbols> headerMap;
		for (const auto& descriptor : descriptors) {
			auto symbolOpt = ParseSymbolDescriptor(descriptor);
			if (symbolOpt.has_value()) {
				if (headerMap.find(descriptor.header) == headerMap.end()) {
					headerMap[descriptor.header] = HeaderSymbols{ descriptor.header, {} };
				}
				headerMap[descriptor.header].symbols.push_back(*symbolOpt);
			}
		}
		
		std::vector<HeaderSymbols> result;
		result.reserve(headerMap.size());
		for (auto& [header, headerSymbols] : headerMap) {
			result.push_back(std::move(headerSymbols));
		}

		return result;
	}

	static std::optional<ConcreteSymbol> ParseSymbolDescriptor(const SymbolDescriptor& descriptor) {
		switch (descriptor.type) {
		case Type::Function: {
			std::istringstream stream(descriptor.comment);
			std::string line;
			std::vector<std::string> lines;

			while (std::getline(stream, line)) {
				lines.push_back(line);
			}

			if (lines.empty())
				return std::nullopt;

			// Parse only the last line since there is no point in parsing multiple in a function comment
			std::string lastLine = lines.back();
			auto addressOpt = ParseAddressComment(lastLine);
			auto signatureOpt = ParseSignatureComment(lastLine);
			if (addressOpt.has_value()) {
				ConcreteSymbol symbol;
				symbol.header = descriptor.header;
				symbol.type = descriptor.type;
				symbol.name = descriptor.name;
				symbol.comment = descriptor.comment;
				symbol.address = *addressOpt;
				return symbol;
			} else if (signatureOpt.has_value()) {
				ConcreteSymbol symbol;
				symbol.header = descriptor.header;
				symbol.type = descriptor.type;
				symbol.name = descriptor.name;
				symbol.comment = descriptor.comment;
				symbol.usesSignature = true;
				symbol.signature = *signatureOpt;
				return symbol;
			}
			break;
			}
		}
		return std::nullopt;
	}

	static std::optional<uintptr_t> ParseAddressComment(const std::string& comment) {
		std::regex addressRegex(R"(///\s*@address\s*\{\s*(.*?)\s*\})");
		std::smatch match;
		if (std::regex_search(comment, match, addressRegex)) {
			if (match.size() > 1) {
				std::string addressStr = match[1].str();
				try {
					uintptr_t address = std::stoull(addressStr, nullptr, 16);
					return address;
				} catch (const std::exception&) {
					return std::nullopt;
				}
			}
		}
		return std::nullopt;
	}

	static std::optional<std::string> ParseSignatureComment(const std::string& comment) {
		std::regex signatureRegex(R"(///\s*@signature\s*\{\s*(.*?)\s*\})");
		std::smatch match;
		if (std::regex_search(comment, match, signatureRegex)) {
			if (match.size() > 1) {
				return match[1].str();
			}
		}
		return std::nullopt;
	}

	static void DeleteSymbolsFor(const fs::path& base, const fs::path& header) {
		fs::path headerSymbolJson = header.parent_path() / (header.stem().string() + ".symbols.json");
		fs::path fullPath = base / headerSymbolJson;
		if (fs::exists(fullPath))
			fs::remove(fullPath);
	}
};