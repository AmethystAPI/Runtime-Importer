#include "CommentProcessor.hpp"

#include <regex>
#include <sstream>

std::vector<Annotation> CommentProcessor::ProcessComment(const std::string& comment)
{
    std::vector<Annotation> annotations;
    static std::regex re(R"(@(\w+)\s*\{([^}]*)\})");

    std::istringstream stream(comment);
    std::string line;
    while (std::getline(stream, line)) {
        std::smatch match;
        std::string::const_iterator searchStart(line.cbegin());
        while (std::regex_search(searchStart, line.cend(), match, re)) {
            annotations.push_back({ match[1].str(), match[2].str(), line });
            searchStart = match.suffix().first;
        }
    }

    return annotations;
}
