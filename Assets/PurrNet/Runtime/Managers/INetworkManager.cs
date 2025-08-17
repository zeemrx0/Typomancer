﻿using PurrNet.Modules;
using PurrNet.Transports;

namespace PurrNet
{
    public interface INetworkManager
    {
        bool isOffline { get; }

        bool isServer { get; }

        bool isClient { get; }

        ITransport rawTransport { get; }

        ConnectionState serverState  { get; }

        ConnectionState clientState { get; }

        bool shouldAutoStartServer { get; }

        bool shouldAutoStartClient { get; }

        NetworkRules networkRules { get; }

        void StartServer();

        void StartClient();

        void StopServer();

        void StopClient();

        void InternalRegisterClientModules();
        void InternalRegisterServerModules();
        void InternalUnregisterClientModules();
        void InternalUnregisterServerModules();

        bool HasModule<T>(bool asServer) where T : INetworkModule;
    }
}
