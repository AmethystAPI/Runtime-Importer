#pragma once
#include <format>

class Vec2 {
public:
    float x;
    float y;
};

template <>
struct std::formatter<Vec2> {
    constexpr auto parse(std::format_parse_context& ctx) {
        return ctx.begin(); 
    }

    template <typename FormatContext>
    auto format(const Vec2& pos, FormatContext& ctx) const {
        return std::format_to(ctx.out(), "Vec2(x: {}, y: {})", pos.x, pos.y);
    }
};