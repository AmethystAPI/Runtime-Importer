#include "Mod.hpp"
#include <fstream>

Mod::Mod(std::string modName)
{
    this->modName = modName;
    fs::path dllPath = GetTempDll();

    // Loads the mod in a temporary directory so that the original DLL can still be built to
    hModule = LoadLibrary(dllPath.string().c_str());
    if (hModule == NULL) {
        DWORD error = GetLastError();

        switch (error) {
            case ERROR_ACCESS_DENIED:
                Assert(false, "[AmethystRuntime] '{}' does not have the required privileges!", dllPath.string());
            case ERROR_MOD_NOT_FOUND:
                Assert(false, "[AmethystRuntime] Failed to find '{}'", dllPath.string());
            default:
                Assert(false, "[AmethystRuntime] Failed to load '{}.dll', error code: 0x{:x}", modName, error);
        }
    }

    // Load the metadata
    this->metadata = Mod::GetMetadata(modName);
}

FARPROC Mod::GetFunction(const char* functionName)
{
    return GetProcAddress(hModule, functionName);
}

void Mod::Shutdown()
{
    FreeLibrary(hModule);
}

Mod::Metadata Mod::GetMetadata(std::string modName)
{
    // Ensure the mod exists
    fs::path modConfigPath = GetAmethystFolder() / L"mods" / modName / L"mod.json";
    Assert(fs::exists(modConfigPath), "[AmethystRuntime] mod.json could not be found, for {}!", modName);

    // Try to read it to a std::string
    std::ifstream modConfigFile(modConfigPath);

    Assert(modConfigFile.is_open(), "[AmethystRuntime] Failed to open mod.json, for {}!", modName);
    

    // Read into a std::string
    std::stringstream buffer;
    buffer << modConfigFile.rdbuf();
    modConfigFile.close();
    std::string fileContents = buffer.str();

    return Mod::ParseMetadata(modName, fileContents);
}

Mod::Metadata Mod::ParseMetadata(std::string modName, std::string fileContents)
{
    // Parse config.json into json
    json data;

    try {
        data = json::parse(fileContents);
    }
    catch (std::exception e) {
        Log::Error("[AmethystRuntime] Failed to parse mod.json, for {}\n Error: {}", modName, e.what());
        throw e;
    }

    // Verify all fields are correct in config.json
    Assert(data["meta"].is_object(), "[AmethystRuntime] Required field \"meta\" should be of type \"object\" in mod.json, for {}", modName);
    Assert(data["meta"]["name"].is_string(), "[AmethystRuntime] Required field \"name\" in \"meta\" should be of type \"string\" in mod.json, for {}", modName);


    std::vector<std::string> authors = {};

    if (data["meta"]["author"].is_string()) {
        authors.push_back(data["meta"]["author"]);
    }
    else if (data["meta"]["author"].is_array()) {
        for (const auto& element : data["meta"]["author"]) {
            Assert(element.is_string(), "[AmethystRuntime] Array \"author\" in \"meta\" should only contain fields of type \"string\" in mod.json, for {}", modName);
            authors.push_back(element);
        }
    }

    Assert(data["meta"]["version"].is_string(), "[AmethystRuntime] Required field \"version\" in \"meta\" should be of type \"string\" in mod.json, for {}", modName);

    // Set values
    Metadata meta;

    meta.name = data["meta"]["name"];
    meta.author = authors;
    meta.version = data["meta"]["version"];

    return meta;
}

fs::path Mod::GetTempDll()
{
    std::string modShortened = modName;
    size_t atPos = modShortened.find("@");

    if (atPos != std::string::npos) {
        modShortened = modShortened.substr(0, atPos);
    }

    // Ensure temp directory exists
    fs::path tempDir = GetAmethystFolder() / L"Temp" / modName;
    if (!fs::exists(tempDir)) fs::create_directories(tempDir);

    fs::path originalDll = GetAmethystFolder() / L"Mods" / modName / std::string(modShortened + ".dll");
    Assert(fs::exists(originalDll), "[AmethystRuntime] Could not find '{}.dll'", modShortened);
    
    fs::path tempDll = tempDir.string() + modShortened + ".dll";

    try {
        fs::copy_file(originalDll, tempDll, fs::copy_options::overwrite_existing);
    }
    catch (const std::filesystem::filesystem_error& e) {
        Assert(false, "[AmethystRuntime] {} (Error code: {})", e.what(), e.code().value());
    }

    return tempDll;
}