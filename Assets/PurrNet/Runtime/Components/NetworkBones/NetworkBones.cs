using PurrNet.Modules;
using PurrNet.Packing;
using PurrNet.Pooling;
using PurrNet.Transports;
using PurrNet.Utils;
using UnityEngine;

namespace PurrNet
{
    public class NetworkBones : NetworkIdentity
    {
        [Header("Settings")]
        [SerializeField] private bool _ownerAuth = true;
        [SerializeField, Range(1, 128), PurrLock] private int _sendRatePerSecond = 10;
        [Header("Accuracy")]
        [SerializeField, PurrLock] private float _positionAccuracy = 0.01f;
        [SerializeField, PurrLock] private float _angleAccuracy = 0.5f;
        [SerializeField, PurrLock] private float _scaleAccuracy = 0.05f;

        private DisposableList<Transform> _bones = DisposableList<Transform>.Create(512);
        private BoneInfo[] _bonesInfo;

        private Interpolated<Vector3>[] _positions;
        private Interpolated<Quaternion>[] _rotations;
        private Interpolated<Vector3>[] _scales;

        private DeltaModule _clientDeltaModule;
        private DeltaModule _serverDeltaModule;

        private float _sendDelta;

        protected override void OnSpawned()
        {
            _sendDelta = 1f / _sendRatePerSecond;

            GatherBones();
            GatherBonesInfo(ref _bonesInfo);

            networkManager.TryGetModule<DeltaModule>(out _clientDeltaModule, false);
            networkManager.TryGetModule<DeltaModule>(out _serverDeltaModule, true);
        }

        private void OnEnable()
        {
            if (!isSpawned)
                return;

            for (var bIdx = 0; bIdx < _bones.Count; bIdx++)
            {
                _positions[bIdx].Teleport(_bones[bIdx].localPosition);
                _rotations[bIdx].Teleport(_bones[bIdx].localRotation);
                _scales[bIdx].Teleport(_bones[bIdx].localScale);
            }
        }

        private void GatherBones()
        {
            using var renderers = DisposableList<SkinnedMeshRenderer>.Create(32);

            gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(renderers.list);
            _bones.Clear();

            for (var rIdx = 0; rIdx < renderers.list.Count; rIdx++)
            {
                var r = renderers.list[rIdx];
                var bones = r.bones;

                for (int bIdx = 0; bIdx < bones.Length; bIdx++)
                {
                    if (!_bones.Contains(bones[bIdx]))
                        _bones.Add(bones[bIdx]);
                }
            }

            _positions = new Interpolated<Vector3>[_bones.Count];
            _rotations = new Interpolated<Quaternion>[_bones.Count];
            _scales = new Interpolated<Vector3>[_bones.Count];
            _bonesInfo = new BoneInfo[_bones.Count];

            for (var bIdx = 0; bIdx < _bones.Count; bIdx++)
            {
                _positions[bIdx] = new Interpolated<Vector3>(Vector3.Lerp, _sendDelta, _bones[bIdx].localPosition);
                _rotations[bIdx] = new Interpolated<Quaternion>(Quaternion.Slerp, _sendDelta, _bones[bIdx].localRotation);
                _scales[bIdx] = new Interpolated<Vector3>(Vector3.Lerp, _sendDelta, _bones[bIdx].localScale);
            }
        }

        private void GatherBonesInfo(ref BoneInfo[] info)
        {
            var nid = id!.Value;
            for (var i = 0; i < _bones.Count; i++)
            {
                var bone = _bones[i];
                var boneInfo = new BoneInfo
                {
                    localScale = bone.localScale,
                    posHash = new NetworkBoneID(sceneId, nid, i, BoneInfoType.Position),
                    rotHash = new NetworkBoneID(sceneId, nid, i, BoneInfoType.Rotation),
                    scaleHash = new NetworkBoneID(sceneId, nid, i, BoneInfoType.Scale)
                };

                bone.GetLocalPositionAndRotation(out boneInfo.localPosition, out boneInfo.localRotation);
                info[i] = boneInfo;
            }
        }

        private float _accumulateTime;

        private void LateUpdate()
        {
            if (!isSpawned)
                return;

            if (_bones.Count == 0)
                return;

            // if we dont control it, update from incoming data
            if (!IsController(_ownerAuth))
            {
                UpdateVisuals();

                // if we are server we still need to propagate it to the rest
                if (!isServer)
                    return;
            }

            _accumulateTime += Time.deltaTime;
            if (_accumulateTime < _sendDelta)
                return;

            int deltasNeeded = (int)(_accumulateTime / _sendDelta);
            _accumulateTime -= _sendDelta * deltasNeeded;

            bool asServer = isServer;
            var module = asServer ? _serverDeltaModule : _clientDeltaModule;

            for (int i = 0; i < deltasNeeded; i++)
                InternalTick(module, asServer);
        }

        private void UpdateVisuals()
        {
            for (var i = 0; i < _bones.Count; i++)
            {
                var bone = _bones[i];
                bone.SetLocalPositionAndRotation(
                    _positions[i].Advance(Time.unscaledDeltaTime),
                    _rotations[i].Advance(Time.unscaledDeltaTime));
                bone.localScale = _scales[i].Advance(Time.unscaledDeltaTime);
            }
        }

        void InternalTick(DeltaModule module, bool asServer)
        {
            GatherBonesInfo(ref _bonesInfo);

            if (asServer)
            {
                for (var i = 0; i < observers.Count; i++)
                {
                    SendPositions(observers[i], module);
                    SendRotations(observers[i], module);
                    SendScales(observers[i], module);
                }
            }
            else
            {
                SendPositions(default, module);
            }
        }

        private Vector3Int CompressPosition(Vector3 position)
        {
            int x = Mathf.RoundToInt(position.x / _positionAccuracy);
            int y = Mathf.RoundToInt(position.y / _positionAccuracy);
            int z = Mathf.RoundToInt(position.z / _positionAccuracy);
            return new Vector3Int(x, y, z);
        }

        private Vector3 DecompressPosition(Vector3Int position)
        {
            return new Vector3(
                position.x * _positionAccuracy,
                position.y * _positionAccuracy,
                position.z * _positionAccuracy
            );
        }

        private Vector3Int CompressEuler(Vector3 euler)
        {
            int x = Mathf.RoundToInt(euler.x / _angleAccuracy);
            int y = Mathf.RoundToInt(euler.y / _angleAccuracy);
            int z = Mathf.RoundToInt(euler.z / _angleAccuracy);
            return new Vector3Int(x, y, z);
        }

        private Vector3 DecompressEuler(Vector3Int euler)
        {
            return new Vector3(
                euler.x * _angleAccuracy,
                euler.y * _angleAccuracy,
                euler.z * _angleAccuracy
            );
        }

        private Vector3Int CompressScale(Vector3 scale)
        {
            int x = Mathf.RoundToInt(scale.x / _scaleAccuracy);
            int y = Mathf.RoundToInt(scale.y / _scaleAccuracy);
            int z = Mathf.RoundToInt(scale.z / _scaleAccuracy);
            return new Vector3Int(x, y, z);
        }

        private Vector3 DecompressScale(Vector3Int scale)
        {
            return new Vector3(
                scale.x * _scaleAccuracy,
                scale.y * _scaleAccuracy,
                scale.z * _scaleAccuracy
            );
        }

        const int MTU = 1100;


        delegate void Forward(PlayerID observer, PackedUInt startingIdx, PackedUInt count, BitPacker data);
        delegate bool Write(BitPacker packer, DeltaModule module, PlayerID player, BoneInfo info, ref PackedUInt cachedKey);

        void Pack(PlayerID observer, DeltaModule module, Forward forward, Write write)
        {
            using var packer = BitPackerPool.Get();
            PackedUInt cache = default;
            uint lastIndex = 0;
            bool writtenAny = false;

            for (uint b = 0; b <_bones.Count; b++)
            {
                writtenAny = write(packer, module, observer, _bonesInfo[b], ref cache) || writtenAny;

                if (packer.positionInBytes > MTU)
                {
                    if (writtenAny)
                    {
                        var count = b - lastIndex + 1;
                        forward(observer, lastIndex, count, packer);
                    }
                    lastIndex = b;
                    packer.ResetPosition();
                    writtenAny = false;
                }
            }

            if (writtenAny && packer.positionInBits > 0)
            {
                var count = (uint)_bones.Count - lastIndex;
                forward(observer, lastIndex, count, packer);
            }
        }

        private void SendPositions(PlayerID observer, DeltaModule module) =>
            Pack(observer, module, ForwardPositions, WritePosition);

        bool WritePosition(BitPacker packer, DeltaModule module, PlayerID player, BoneInfo info, ref PackedUInt cachedKey)
        {
            var newPos = CompressPosition(info.localPosition);
            return module.Write(packer, player, info.posHash, newPos, ref cachedKey);
        }

        private void ForwardPositions(PlayerID observer, PackedUInt startingIdx, PackedUInt count, BitPacker data)
        {
            if (observer == default)
                 RpcPositionsToServer(startingIdx, count, data);
            else RpcPositions(observer, startingIdx, count, data);
        }

        [TargetRpc(channel: Channel.Unreliable)]
        private void RpcPositions(PlayerID target, PackedUInt startingIdx, PackedUInt count, BitPacker data,
            RPCInfo info = default)
        {
            using (data)
                ReadPositions(info.sender, startingIdx, count, data, _clientDeltaModule);
        }

        [ServerRpc(channel: Channel.Unreliable)]
        private void RpcPositionsToServer(PackedUInt startingIdx, PackedUInt count, BitPacker data, RPCInfo info = default)
        {
            if (!_ownerAuth)
                return;

            using (data)
                ReadPositions(info.sender, startingIdx, count, data, _serverDeltaModule);
        }

        private void ReadPositions(PlayerID sender, PackedUInt startingIdx, PackedUInt count, BitPacker packer, DeltaModule module)
        {
            uint lastIndex = startingIdx + count;
            PackedUInt cache = default;

            for (uint i = startingIdx; i < lastIndex; i++)
            {
                var boneInfo = _bonesInfo[(int)i];
                Vector3Int newPos = default;
                module.Read(packer, boneInfo.posHash, sender, ref newPos, ref cache);
                _positions[i].Add(DecompressPosition(newPos));
            }
        }

        private void SendRotations(PlayerID observer, DeltaModule module)
            => Pack(observer, module, ForwardRotations, WriteRotation);

        bool WriteRotation(BitPacker packer, DeltaModule module, PlayerID player, BoneInfo info, ref PackedUInt cachedKey)
        {
            var newRot = CompressEuler(info.localRotation.eulerAngles);
            return module.Write(packer, player, info.rotHash, newRot, ref cachedKey);
        }

        private void ForwardRotations(PlayerID observer, PackedUInt startingIdx, PackedUInt count, BitPacker data)
        {
            if (observer == default)
                RpcRotationsToServer(startingIdx, count, data);
            else RpcRotations(observer, startingIdx, count, data);
        }

        [TargetRpc(channel: Channel.Unreliable)]
        private void RpcRotations(PlayerID target, PackedUInt startingIdx, PackedUInt count, BitPacker data,
            RPCInfo info = default)
        {
            using (data)
                ReadRotations(info.sender, startingIdx, count, data, _clientDeltaModule);
        }

        [ServerRpc(channel: Channel.Unreliable)]
        private void RpcRotationsToServer(PackedUInt startingIdx, PackedUInt count, BitPacker data, RPCInfo info = default)
        {
            if (!_ownerAuth)
                return;

            using (data)
                ReadRotations(info.sender, startingIdx, count, data, _serverDeltaModule);
        }

        private void ReadRotations(PlayerID sender, PackedUInt startingIdx, PackedUInt count, BitPacker packer, DeltaModule module)
        {
            uint lastIndex = startingIdx + count;
            PackedUInt cache = default;

            for (uint i = startingIdx; i < lastIndex; i++)
            {
                var boneInfo = _bonesInfo[(int)i];
                Vector3Int newRot = default;
                module.Read(packer, boneInfo.rotHash, sender, ref newRot, ref cache);
                _rotations[i].Add(Quaternion.Euler(DecompressEuler(newRot)));
            }
        }

        private void SendScales(PlayerID observer, DeltaModule module)
            => Pack(observer, module, ForwardScales, WriteScale);

        bool WriteScale(BitPacker packer, DeltaModule module, PlayerID player, BoneInfo info, ref PackedUInt cachedKey)
        {
            var newScale = CompressScale(info.localScale);
            return module.Write(packer, player, info.scaleHash, newScale, ref cachedKey);
        }

        private void ForwardScales(PlayerID observer, PackedUInt startingIdx, PackedUInt count, BitPacker data)
        {
            if (observer == default)
                RpcScalesToServer(startingIdx, count, data);
            else RpcScales(observer, startingIdx, count, data);
        }

        [TargetRpc(channel: Channel.Unreliable)]
        private void RpcScales(PlayerID target, PackedUInt startingIdx, PackedUInt count, BitPacker data,
            RPCInfo info = default)
        {
            using (data)
                ReadScales(info.sender, startingIdx, count, data, _clientDeltaModule);
        }

        [ServerRpc(channel: Channel.Unreliable)]
        private void RpcScalesToServer(PackedUInt startingIdx, PackedUInt count, BitPacker data, RPCInfo info = default)
        {
            if (!_ownerAuth)
                return;

            using (data)
                ReadScales(info.sender, startingIdx, count, data, _serverDeltaModule);
        }

        private void ReadScales(PlayerID sender, PackedUInt startingIdx, PackedUInt count, BitPacker packer, DeltaModule module)
        {
            uint lastIndex = startingIdx + count;
            PackedUInt cache = default;

            for (uint i = startingIdx; i < lastIndex; i++)
            {
                var boneInfo = _bonesInfo[(int)i];
                Vector3Int newScale = default;
                module.Read(packer, boneInfo.scaleHash, sender, ref newScale, ref cache);
                _scales[i].Add(DecompressScale(newScale));
            }
        }
    }
}
