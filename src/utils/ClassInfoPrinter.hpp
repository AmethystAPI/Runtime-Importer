#pragma once
#include <iostream>
#include <format>

#include <parsing/metadata/ClassInfo.hpp>
#include <parsing/metadata/MethodInfo.hpp>

class ClassInfoPrinter {
public:
	static void PrintClassInfo(ClassInfo* cls, int indent = 0) {
		if (!cls) 
			return;
		std::string indentation(indent * 2, ' ');
		std::cout << std::format(
			"{}Class: '{}' | Virtual: {} | Base: {} | Multi-Inheritance: {}",
			indentation,
			cls->Name,
			(cls->HasAtLeastOneVirtualMethod() ? "yes" : "no"),
			(cls->HasNoBases() ? "yes" : "no"),
			(cls->DoesMultiInheritance() ? "yes" : "no")
		) << std::endl;
		if (!cls->Methods.empty()) {
			for (auto* method : cls->Methods) {
				std::cout << std::format(
					"{}  - Function: '{}' | Virtual: {} | VirtualIndex: {} | VirtualTarget: {}",
					indentation,
					method->ShortName,
					(method->IsVirtual ? "yes" : "no"),
					method->VirtualIndex,
					(method->VirtualTableTarget.has_value() ? *method->VirtualTableTarget : "not_virtual")
				);
				if (method->OverrideOf)
					std::cout << " | Overrides: " << method->OverrideOf->DeclaringClass->Name + "::" + method->OverrideOf->ShortName;
				std::cout << "\n";
			}
		}
	}
};