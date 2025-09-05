#pragma once
#include <vector>

#include "parsing/annotations/Annotation.hpp"

class CommentProcessor {
public:
	static std::vector<Annotation> ProcessComment(const std::string& comment);
};