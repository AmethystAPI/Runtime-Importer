#include "CommentParser.hpp"
#include <sstream>
#include <regex>
#include <iostream>
#include <cstddef>
#include "parsing/ClassInfo.hpp"
#include "parsing/FunctionInfo.hpp"

std::vector<CommentParser::CommentVariant> CommentParser::ParseComment(const std::string& comment, const ParsingContext& context)
{
	std::istringstream stream(comment);
	std::string line;
	std::vector<CommentVariant> comments;
	while (std::getline(stream, line))
	{
		std::regex virtualPointerRegex(R"(///\s*@vPointer\s*\{\s*(0x[0-9A-Fa-f]+)\s*,\s*([^}]+)\s*\})");
		std::regex virtualIndexRegex(R"(///\s*@vIndex\s*\{\s*(\d+)\s*\})");
		std::regex virtualTargetRegex(R"(///\s*@vTarget\s*\{\s*(\w+)\s*\})");
		std::smatch match;

		// Virtual pointer match
		if (std::regex_search(line, match, virtualPointerRegex)) {
			if (context.mClass == nullptr) {
				std::cerr << "[Warning]: " << "Couldn't parse comment: " << line << std::endl;
				continue;
			}

			std::string strHex = match[1];
			std::string vtableFor = match[2];
			try {
				size_t idx = 0;
				auto hexVal = static_cast<uintptr_t>(std::stoull(strHex, &idx, 16));

				if (idx != strHex.size())
					throw std::runtime_error("Failed to parse comment: " + line);

				std::string vtableForFull = context.mClass->Name + "_vtable_for_" + vtableFor;
				VirtualPointerComment parsed;
				parsed.mForVtable = vtableForFull;
				parsed.mAddress = hexVal;
				comments.push_back(parsed);
			}
			catch (const std::exception& exception) {
				std::cerr << "[Warning]: " << exception.what() << std::endl;
				continue;
			}
		}
		else if (std::regex_search(line, match, virtualIndexRegex)) {
			if (context.mFunction == nullptr) {
				std::cerr << "[Warning]: " << "Couldn't parse comment: " << line << std::endl;
				continue;
			}

			std::string index = match[1];
			try {
				int indexVal = std::stoi(index);
				VirtualIndexComment parsed;
				parsed.mIndex = indexVal;
				comments.push_back(parsed);
			}
			catch (const std::exception& exception) {
				std::cerr << "[Warning]: " << exception.what() << std::endl;
				continue;
			}
		}
	}
	return comments;
}
