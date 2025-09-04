#pragma once
#include <cstdint>

class InputMapping;

class InputMappingFactory {
public:
    virtual ~InputMappingFactory();
    virtual const InputMapping* getMapping(const std::string& mappingName);
};

class InputMappingFactoryMap {
public:
    virtual ~InputMappingFactoryMap();
    virtual InputMappingFactory& getInputMappingFactory(int32_t controllerId);
};