#pragma once

enum class GameType : int32_t {
  Undefined = 0xff,
  Survival = 0x0000,
  Creative = 0x0001,
  Adventure = 0x0002,
  SurvivalViewer = 0x0003,
  CreativeViewer = 0x0004,
  Default = 0x0005,
  WorldDefault = 0x0000,
};