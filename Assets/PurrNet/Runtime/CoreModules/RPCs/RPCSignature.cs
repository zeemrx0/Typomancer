﻿using System.Collections.Generic;
using JetBrains.Annotations;
using PurrNet.Modules;
using PurrNet.Pooling;
using PurrNet.Transports;

namespace PurrNet
{
    public struct RPCInfo
    {
        public NetworkManager manager;
        public PlayerID sender;
        public bool asServer;

        [UsedByIL] public RPCSignature compileTimeSignature;
    }

    public enum RPCType
    {
        ServerRPC,
        ObserversRPC,
        TargetRPC
    }

    public struct RPCSignature
    {
        public RPCType type;
        public Channel channel;
        public bool isStatic;
        public bool runLocally;
        public bool requireOwnership;
        public bool bufferLast;
        public bool requireServer;
        public bool excludeOwner;
        public bool excludeSender;
        public string rpcName;
        public float asyncTimeoutInSec;
        public CompressionLevel compressionLevel;
        public PlayerID? targetPlayer;
        public IEnumerable<PlayerID> targetPlayerEnumerable;
        public IList<PlayerID> targetPlayerList;
        public StripCodeModeOverride stripCodeMode;

        public DisposableList<PlayerID> GetTargets()
        {
            var players = DisposableList<PlayerID>.Create();

            if (targetPlayer.HasValue)
                players.Add(targetPlayer.Value);
            else if (targetPlayerList != null)
                players.AddRange(targetPlayerList);
            else if (targetPlayerEnumerable != null)
                players.AddRange(targetPlayerEnumerable);

            return players;
        }

        [UsedImplicitly]
        public static RPCSignature Make(RPCType type, Channel channel, bool runLocally, bool requireOwnership,
            bool bufferLast, bool requireServer, bool excludeOwner, string name, bool isStatic, float asyncTimoutInSec,
            CompressionLevel compressionLevel, bool excludeSender)
        {
            return new RPCSignature
            {
                type = type,
                channel = channel,
                runLocally = runLocally,
                requireOwnership = requireOwnership,
                bufferLast = bufferLast,
                requireServer = requireServer,
                excludeOwner = excludeOwner,
                excludeSender = excludeSender,
                targetPlayer = null,
                targetPlayerEnumerable = null,
                targetPlayerList = null,
                isStatic = isStatic,
                rpcName = name,
                asyncTimeoutInSec = asyncTimoutInSec,
                compressionLevel = compressionLevel
            };
        }

        [UsedImplicitly]
        public static RPCSignature MakeWithTarget(RPCType type, Channel channel, bool runLocally, bool requireOwnership,
            bool bufferLast, bool requireServer, bool excludeOwner, string name, bool isStatic, float asyncTimoutInSec,
            CompressionLevel compressionLevel, bool excludeSender, PlayerID? playerID, IEnumerable<PlayerID> players, IList<PlayerID> playersList)
        {
            return new RPCSignature
            {
                type = type,
                channel = channel,
                runLocally = runLocally,
                requireOwnership = requireOwnership,
                bufferLast = bufferLast,
                requireServer = requireServer,
                excludeOwner = excludeOwner,
                excludeSender = excludeSender,
                targetPlayer = playerID,
                targetPlayerEnumerable = players,
                targetPlayerList = playersList,
                isStatic = isStatic,
                rpcName = name,
                asyncTimeoutInSec = asyncTimoutInSec,
                compressionLevel = compressionLevel
            };
        }
    }
}
