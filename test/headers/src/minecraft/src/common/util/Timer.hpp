#pragma once
#include <functional>

class Timer {
public:
    float mTicksPerSecond;
    int mTicks;
    float mAlpha;
    float mTimeScale;
    float mPassedTime;
    float mFrameStepAlignmentRemainder;
    float mLastTimeSeconds;
    float mLastTimestep;
    int64_t mLastMs;
    int64_t mLastMsSysTime;
    float mAdjustTime;
    int mSteppingTick;
    std::function<int64_t __cdecl(void)> mGetTimeMSCallback;
};