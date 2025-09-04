#pragma once
#include <string>
#include <set>
#include <vector>
#include <json/json.h>

class UIResolvedDef {
public:
    bool mIgnored;
    const std::string mDefNamespace;
    const std::string mDefName;

    // anything below here seems to be completely broken
    const Json::Value& mBaseVal;
    const Json::Value& mOverrideVal;
    Json::Value mVariables;
    std::vector<Json::Value*>* mVariablesStack;
    std::set<std::string> mUnvalidatedProperties;

    std::string getAsString(std::string_view key) const {
        using function = decltype(&UIResolvedDef::getAsString);
        static function func = std::bit_cast<function>(SigScan("40 55 53 56 57 41 56 41 57 48 8D 6C 24 ? 48 81 EC ? ? ? ? 4D 8B F1 48 8B F2"));
        return (this->*func)(key);
    }
};