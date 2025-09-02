#pragma once
#include <array>

enum class CommandPermissionLevel : __int8
{
  Any = 0x0,
  GameDirectors = 0x1,
  Admin = 0x2,
  Host = 0x3,
  Owner = 0x4,
  Internal = 0x5,
};

class PermissionsHandler
{
  CommandPermissionLevel mCommandPermissions;
  unsigned char mPlayerPermissions[1];
};

class Ability {
public:
  enum class Type : unsigned char {Invalid, Unset, Bool, Float};
  enum class Options : unsigned char {None, NoSave, CommandExposed, PermissionsInterfaceExposed = 4};

  union Value {
    bool mBoolVal;
    float mFloatVal;
  };

  Type mType;
  Value mValue;
  Options mOptions;
};

class Abilities {
public:
  std::array<Ability,19> mAbilities;
};

const class LayeredAbilities {
public:
    PermissionsHandler mPermissions;
    std::array<Abilities,5> mLayers;

    bool getBool(uint8_t abilityIndex) const {
        // Chatgpt impl, might be wrong xD

        // Walk layers from top (last) to bottom (first)
        for (auto it = mLayers.rbegin(); it != mLayers.rend(); ++it) {
            if (abilityIndex >= it->mAbilities.size())
                continue;

            const Ability& ab = it->mAbilities[abilityIndex];
            if (ab.mType == Ability::Type::Bool) {
                return ab.mValue.mBoolVal;
            }
        }

        // If not found, return default
        return false;
    }
};