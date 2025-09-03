#pragma once
#include <string>
#include <vector>

class FunctionInfo;
class ClassInfo {
public:
	std::string mName;
	std::vector<std::string> mDirectBases;
	std::vector<ClassInfo*> mDirectBaseInfos;
	std::vector<std::string> mAllFunctions;
	std::vector<FunctionInfo*> mAllFunctionInfos;
	bool isVirtual = false;
};