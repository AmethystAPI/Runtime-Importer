#include "parsing/ClassInfo.hpp"
#include "parsing/FunctionInfo.hpp"
#include <unordered_set>
#include <functional>

int ClassInfo::GetNextVirtualIndex() {
	return NextVirtualIndex++;
}

bool ClassInfo::HasNoBases() const {
	return BaseClasses.size() == 0;
}

bool ClassInfo::OwnsAtLeastOneVirtualFunction() {
	for (auto* func : Functions) {
		if (func->IsVirtual)
			return true;
	}
	return false;
}

bool ClassInfo::HasAtLeastOneVirtualFunction() {
	if (OwnsAtLeastOneVirtualFunction())
		return true;
	for (auto* base : BaseClasses) {
		if (base->HasAtLeastOneVirtualFunction())
			return true;
	}
	return false;
}

bool ClassInfo::DoesMultiInheritance()
{
	if (!HasAtLeastOneVirtualFunction())
		return false;
	if (BaseClasses.size() > 1)
		return true;
	for (auto* base : BaseClasses) {
		if (base->DoesMultiInheritance())
			return true;
	}
	return false;
}

std::vector<ClassInfo*> ClassInfo::GetRootBases()
{
	std::vector<ClassInfo*> roots;
	std::unordered_set<ClassInfo*> seen;

	std::function<void(ClassInfo*)> visit = [&](ClassInfo* cls) {
		if (!cls) return;
		if (cls->BaseClasses.empty()) {
			if (seen.insert(cls).second)
				roots.push_back(cls);
			return;
		}
		for (auto* base : cls->BaseClasses)
			visit(base);
	};

	visit(this);
	return roots;
}
