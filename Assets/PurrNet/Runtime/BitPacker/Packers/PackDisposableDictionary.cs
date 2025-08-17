﻿using System;
using PurrNet.Pooling;
using UnityEngine;

namespace PurrNet.Packing
{
    public static class PackDisposableDictionary
    {
        public static void WriteDictionary<K, V>(this BitPacker packer, DisposableDictionary<K, V> value)
        {
            if (value.isDisposed || value.dictionary == null)
            {
                Packer<bool>.Write(packer, false);
                return;
            }

            Packer<bool>.Write(packer, true);

            int length = value.Count;
            packer.WriteInteger(length, 31);

            foreach (var pair in value)
            {
                Packer<K>.Write(packer, pair.Key);
                Packer<V>.Write(packer, pair.Value);
            }
        }

        public static void ReadDictionary<K, V>(this BitPacker packer, ref DisposableDictionary<K, V> value)
        {
            bool hasValue = default;
            packer.Read(ref hasValue);

            if (!hasValue)
            {
                if (!value.isDisposed)
                    value.Dispose();
                return;
            }

            long length = default;

            packer.ReadInteger(ref length, 31);

            if (value.isDisposed || value.dictionary == null)
                value = DisposableDictionary<K, V>.Create();
            else value.Clear();

            for (int i = 0; i < length; i++)
            {
                K key = default;
                V val = default;
                Packer<K>.Read(packer, ref key);
                Packer<V>.Read(packer, ref val);

                try
                {
                    value.Add(key, val);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static bool WriteDeltaDictionary<TKey, TValue>(this BitPacker packer,
            DisposableDictionary<TKey, TValue> old,
            DisposableDictionary<TKey, TValue> value)
        {
            var start = packer.AdvanceBits(1);

            bool hasChanged;

            PackedInt oldCount = old.isDisposed ? -1 : old.Count;
            PackedInt newCount = value.isDisposed ? -1 : value.Count;

            hasChanged = DeltaPacker<PackedInt>.Write(packer, oldCount, newCount);

            if (newCount > 0)
            {
                DisposableList<TKey> oldKeysList = default;
                DisposableList<TValue> oldValuesList = default;

                using var newKeysList = DisposableList<TKey>.Create(newCount.value);
                using var newValuesList = DisposableList<TValue>.Create(newCount.value);

                if (oldCount.value >= 0)
                {
                    oldKeysList = DisposableList<TKey>.Create(oldCount.value);
                    oldValuesList = DisposableList<TValue>.Create(oldCount.value);
                    oldKeysList.AddRange(old.Keys);
                    oldValuesList.AddRange(old.Values);
                }

                newKeysList.AddRange(value.Keys);
                newValuesList.AddRange(value.Values);

                hasChanged = packer.WriteDisposableDeltaList(oldKeysList, newKeysList) || hasChanged;
                hasChanged = packer.WriteDisposableDeltaList(oldValuesList, newValuesList) || hasChanged;

                oldKeysList.Dispose();
                oldValuesList.Dispose();
            }

            packer.WriteAt(start, hasChanged);

            if (!hasChanged)
                packer.SetBitPosition(start + 1);

            return hasChanged;
        }

        public static void ReadDeltaDictionary<TKey, TValue>(BitPacker packer,
            DisposableDictionary<TKey, TValue> oldvalue,
            ref DisposableDictionary<TKey, TValue> value)
        {
            bool hasChanged = default;
            packer.Read(ref hasChanged);

            if (!hasChanged)
            {
                if (oldvalue.isDisposed)
                {
                    value.Dispose();
                    return;
                }

                if (value.isDisposed)
                    value = DisposableDictionary<TKey, TValue>.Create();

                foreach (var (k, v) in oldvalue)
                    value.Add(k, v);

                return;
            }

            PackedInt oldCount = oldvalue.isDisposed ? -1 : oldvalue.Count;
            PackedInt newCount = default;

            DeltaPacker<PackedInt>.Read(packer, oldCount, ref newCount);

            if (newCount.value < 0)
            {
                if (!value.isDisposed)
                    value.Dispose();
                return;
            }

            if (value.isDisposed || value.dictionary == null)
                value = DisposableDictionary<TKey, TValue>.Create();
            else value.Clear();

            if (newCount.value == 0)
            {
                return;
            }

            DisposableList<TKey> oldKeysList = default;
            DisposableList<TValue> oldValuesList = default;

            if (oldCount.value >= 0)
            {
                oldKeysList = DisposableList<TKey>.Create(oldCount.value);
                oldValuesList = DisposableList<TValue>.Create(oldCount.value);
                oldKeysList.AddRange(oldvalue.Keys);
                oldValuesList.AddRange(oldvalue.Values);
            }

            var keysList = DisposableList<TKey>.Create(newCount.value);
            var valuesList = DisposableList<TValue>.Create(newCount.value);

            packer.ReadDisposableDeltaList(oldKeysList, ref keysList);
            packer.ReadDisposableDeltaList(oldValuesList, ref valuesList);

            for (int i = 0; i < newCount.value; i++)
                value.Add(keysList[i], valuesList[i]);

            oldKeysList.Dispose();
            oldValuesList.Dispose();
            keysList.Dispose();
            valuesList.Dispose();
        }
    }
}
