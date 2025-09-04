#pragma once
#include "Utility.hpp"
#include <Windows.h>
#include <iostream>
#include <format>

namespace Log {
    void InitializeConsole();
    void DestroyConsole();
    void HideConsole();

    constexpr const char* RESET   = "\033[0m";
    constexpr const char* YELLOW  = "\033[1;33m";
    constexpr const char* RED     = "\033[1;31m";

    template <typename... T>
    void Info(const std::format_string<T...> fmt, T&&... args) {
        std::string formatted_string = std::format(fmt, std::forward<T>(args)...);
        std::cout << formatted_string << "\n";
    }

    template <typename... T>
    void Info(const std::wformat_string<T...> fmt, T&&... args) {
        std::wstring formatted_string = std::format(fmt, std::forward<T>(args)...);
        std::wcout << formatted_string << L"\n";
    }

    template <typename... T>
    void Warning(const std::format_string<T...> fmt, T&&... args) {
        std::string formatted_string = std::format(fmt, std::forward<T>(args)...);
        std::cout << YELLOW << formatted_string << RESET << "\n";
    }

    template <typename... T>
    void Warning(const std::wformat_string<T...> fmt, T&&... args) {
        std::wstring formatted_wstring = std::format(fmt, std::forward<T>(args)...);
        std::string formatted_string = StringFromWstring(formatted_wstring);
        std::cout << YELLOW << formatted_string << RESET << "\n";
    }

    template <typename... T>
    void Error(const std::format_string<T...> fmt, T&&... args) {
        std::string formatted_string = std::format(fmt, std::forward<T>(args)...);
        std::cerr << RED << formatted_string << RESET << "\n";
    }

    template <typename... T>
    void Error(const std::wformat_string<T...> fmt, T&&... args) {
        std::wstring formatted_wstring = std::format(fmt, std::forward<T>(args)...);
        std::string formatted_string = StringFromWstring(formatted_wstring);
        std::cerr << RED << formatted_string << RESET << "\n";
    }

    template <typename... T>
    inline void _Assert(bool condition, const char* function, int line, const std::format_string<T...> fmt, T&&... args)
    {
        if (condition) return;

        std::string formatted_string = std::format(fmt, std::forward<T>(args)...);
        formatted_string += std::format("\n\tin: {}, line: {}", function, line);
        Log::Error("{}", formatted_string);
        throw std::runtime_error(formatted_string);
    }
}; // namespace Log

#define Assert(condition, ...) Log::_Assert(condition, __FUNCTION__, __LINE__, __VA_ARGS__)
