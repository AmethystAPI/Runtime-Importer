#pragma once
#include <string>
#include <unordered_map>
#include <vector>
#include <filesystem>

#include "generation/symbol/SymbolData.hpp"

#include "parsing/metadata/ClassInfo.hpp"
#include "parsing/metadata/MethodInfo.hpp"
#include "parsing/CursorLocation.hpp"

namespace fs = std::filesystem;

class SymbolGenerator {
public:
	static std::unordered_map<fs::path, std::vector<SymbolData>> GenerateSymbolData(const std::vector<ClassInfo*>& classes, const std::vector<MethodInfo*>& methods) {
		std::unordered_map<fs::path, std::vector<SymbolData>> symbolMap;
		for (const auto& cls : classes) {
			if (cls->Location == nullptr || cls->Location->FilePath == "unknown") {
				continue;
			}
			auto& file = cls->Location->FilePath;
			if (symbolMap.count(file) == 0) {
				symbolMap[file] = std::vector<SymbolData>();
			}

			for (auto& [vtableName, vtableAddress] : cls->AnnotationVirtualTables) {
				SymbolData vtableSymbol;
				vtableSymbol.Type = SymbolType::VirtualTableSymbol;
				vtableSymbol.Name = vtableName;
				vtableSymbol.Address = vtableAddress;
				symbolMap[file].push_back(vtableSymbol);
			}
		}

		for (const auto& method : methods) {
			if (method->Location == nullptr || method->Location->FilePath == "unknown") {
				continue;
			}
			auto& file = method->Location->FilePath;
			if (symbolMap.count(file) == 0) {
				symbolMap[file] = std::vector<SymbolData>();
			}
			if (!method->IsImported)
				continue;

			if (method->IsVirtual && method->DeclaringClass->AnnotationVirtualTables.size() > 0) {
				if (method->AnnotationVirtualIndex.has_value()) {
					SymbolData vfuncSymbol;
					vfuncSymbol.Type = SymbolType::VirtualFunctionSymbol;
					vfuncSymbol.Name = method->MangledName;
					vfuncSymbol.VirtualTableTarget = method->AnnotationVirtualIndex->first;
					vfuncSymbol.VirtualIndex = method->AnnotationVirtualIndex->second;
					symbolMap[file].push_back(vfuncSymbol);
				}
				else {
					SymbolData vfuncSymbol;
					vfuncSymbol.Type = SymbolType::VirtualFunctionSymbol;
					vfuncSymbol.Name = method->MangledName;
					vfuncSymbol.VirtualTableTarget = method->VirtualTableTarget;
					vfuncSymbol.VirtualIndex = method->VirtualIndex;
					symbolMap[file].push_back(vfuncSymbol);
				}
			}
			else {
				if (method->AnnotationAddress.has_value()) {
					SymbolData funcSymbol;
					funcSymbol.Type = SymbolType::FunctionSymbol;
					funcSymbol.Name = method->MangledName;
					funcSymbol.Address = method->AnnotationAddress;
					symbolMap[file].push_back(funcSymbol);
				}
				else if (method->AnnotationSignature.has_value()) {
					SymbolData funcSymbol;
					funcSymbol.Type = SymbolType::FunctionSymbol;
					funcSymbol.Name = method->MangledName;
					funcSymbol.Signature = method->AnnotationSignature;
					symbolMap[file].push_back(funcSymbol);
				}
			}
		}

		std::erase_if(symbolMap, [](auto const& kv) {
			return kv.second.empty();
		});

		return symbolMap;
	}
};