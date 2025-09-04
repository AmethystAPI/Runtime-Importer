#pragma once

namespace mce {
    class Math {
    public:
        template <typename T>
        static constexpr size_t hash_accumulate(size_t hash, const T& toHash)
        {
            return ((hash >> 2) + (hash << 6) + std::hash<T>{}(toHash) + 0x9E3779B9u) ^ hash;
        }

        static constexpr size_t hash_accumulate_hashed(size_t hash, size_t other)
        {
            return ((hash >> 2) + (hash << 6) + other + 0x9E3779B9u) ^ hash;
        }

        static float sin(float x)
        {
            return std::sin(x);
        }

        static float cos(float x)
        {
            return std::cos(x);
        }

        static float tan(float x)
        {
            return std::tan(x);
        }

        static float asin(float x)
        {
            return std::asin(x);
        }

        static float acos(float x)
        {
            return std::acos(x);
        }

        static float atan(float x)
        {
            return std::atan(x);
        }

        static float atan2(float y, float x)
        {
            return std::atan2(y, x);
        }

        static float sqrt(float x)
        {
            return std::sqrt(x);
        }
    };
};