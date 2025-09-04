#pragma once
#include <cstddef>
#include <minecraft/src-deps/core/utility/NonOwnerPointer.hpp>
#include <minecraft/src/common/CommonTypes.hpp>
#include <minecraft/src/common/world/level/LevelListener.hpp>
#include <minecraft/src/common/network/NetEventCallback.hpp>

class ServerPlayer;
class ILevel;
class NetworkIdentifier;
class LoginPacket;
class RequestNetworkSettingsPacket;

class TaskGroup {};

// temp to get calling virtuals working
namespace Bedrock::Threading {
class EnableQueueForMainThread {
private:
    // std::unique_ptr<TaskGroup>
    std::unique_ptr<TaskGroup> mQueueForMainThreadTaskGroup;

    virtual ~EnableQueueForMainThread() = default;
    virtual void padding0sgdresgr();
};
}

namespace Social {
class MultiplayerServiceObserver {
public:
    virtual ~MultiplayerServiceObserver() = default;
    virtual void padding0_eagfeg();
};

class XboxLiveUserObserver {
public:
    virtual ~XboxLiveUserObserver() = default;
    virtual void padding0_afdefe();
};
}

class ServerNetworkHandler : Bedrock::Threading::EnableQueueForMainThread, NetEventCallback, LevelListener, Social::MultiplayerServiceObserver, Social::XboxLiveUserObserver {
public:
    /*  */ std::byte padding32[56];
    /* this + 88 */ Bedrock::NonOwnerPointer<ILevel> mLevel;

public:
    virtual ~ServerNetworkHandler();

    // virtuals: cba to deal with virtual inheritance
    virtual ServerPlayer* _getServerPlayer(const NetworkIdentifier& source, SubClientId subId);
    
    /*{
        using function = decltype(&ServerNetworkHandler::_getServerPlayer);
        static auto func = std::bit_cast<function>(SigScan("48 89 5C 24 ? 48 89 6C 24 ? 56 57 41 56 48 83 EC ? 48 8B 41 ? 41 0F B6 E8"));
        return (this->*func)(source, subId);
    }*/
};

//static_assert(offsetof(ServerNetworkHandler, mLevel) == 88);