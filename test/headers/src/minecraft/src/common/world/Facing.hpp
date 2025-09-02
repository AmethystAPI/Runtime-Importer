#pragma once
#include <format>

class Facing {
public:
    enum class Name : unsigned char {
        DOWN,
        UP,
        NORTH,
        SOUTH,
        WEST,
        EAST,
        MAX
    };

    static Facing::Name getOpposite(Facing::Name face);
    static Facing::Name getClockWise(Facing::Name face);
    static Facing::Name getCounterClockWise(Facing::Name face);
    static float getYAngle(Facing::Name face);
};

typedef Facing::Name FacingID;


template <>
struct std::formatter<FacingID> {
    constexpr auto parse(std::format_parse_context& ctx) {
        return ctx.begin(); // No custom specifiers
    }

    template <typename FormatContext>
    auto format(const FacingID& face, FormatContext& ctx) const {
        std::string_view name;
        switch (face) {
            case FacingID::DOWN:  name = "down";  break;
            case FacingID::UP:    name = "up";    break;
            case FacingID::NORTH: name = "north"; break;
            case FacingID::SOUTH: name = "south"; break;
            case FacingID::WEST:  name = "west";  break;
            case FacingID::EAST:  name = "east";  break;
            default:              name = "unknown"; break;
        }
        return std::format_to(ctx.out(), "{}", name);
    }
};