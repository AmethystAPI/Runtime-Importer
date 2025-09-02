#pragma once
#include <format>
#include <minecraft/src/common/world/item/ItemStack.hpp>  
#include <minecraft/src/common/world/item/Item.hpp>  
#include <minecraft/src-client/common/client/renderer/screen/MinecraftUIRenderContext.hpp>

template <>
struct std::formatter<ItemStack> {
    constexpr auto parse(std::format_parse_context& ctx)
    {
        return ctx.begin();
    }

    template <typename FormatContext>
    auto format(const ItemStack& stack, FormatContext& ctx) const
    {
        if (stack.isNull() || !stack.mItem) {
            return std::format_to(ctx.out(), "ItemStack(Null)");
        }

        return std::format_to(ctx.out(), "ItemStack(ID: {}, Count: {})",
            stack.getItem()->mFullName.getString(),
            stack.mCount
        );
    }
};

template <>
struct std::formatter<RectangleArea> {
    constexpr auto parse(std::format_parse_context& ctx)
    {
        return ctx.begin();
    }

    template <typename FormatContext>
    auto format(const RectangleArea& rect, FormatContext& ctx) const
    {
        return std::format_to(ctx.out(), "RectangleArea[({}, {}) to ({}, {})]",
                              rect._x0, rect._y0, rect._x1, rect._y1);
    }
};