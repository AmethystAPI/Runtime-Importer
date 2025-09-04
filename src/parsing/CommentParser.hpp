#pragma once
#include <vector>
#include <string>
#include <variant>

class VirtualPointerComment {
public:
	uintptr_t mAddress;
	std::string mForVtable;
};

class VirtualIndexComment {
public:
	int mIndex;
};

class ClassInfo;
class FunctionInfo;
struct ParsingContext {
	ClassInfo* mClass = nullptr;
	FunctionInfo* mFunction = nullptr;
};

class CommentParser {
public:
	using CommentVariant = std::variant<VirtualPointerComment, VirtualIndexComment>;
	static std::vector<CommentVariant> ParseComment(const std::string& comment, const ParsingContext& context);
};