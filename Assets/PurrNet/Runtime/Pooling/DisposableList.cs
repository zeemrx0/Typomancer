using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace PurrNet.Pooling
{
    public struct DisposableList<T> : IList<T>, IDisposable, IReadOnlyList<T>
    {
        private bool _shouldDispose;

        public List<T> list { get; private set; }

        [Obsolete("Use DisposableList<T>.Create instead")]
        public DisposableList(int capacity)
        {
            var newList = ListPool<T>.Instantiate();

            if (newList.Capacity < capacity)
                newList.Capacity = capacity;

            list = newList;
            _isAllocated = true;
            _shouldDispose = true;
        }

        public static DisposableList<T> Create(int capacity)
        {
            var val = new DisposableList<T>();
            var newList = ListPool<T>.Instantiate();

            if (newList.Capacity < capacity)
                newList.Capacity = capacity;

            val.list = newList;
            val._isAllocated = true;
            val._shouldDispose = true;
            return val;
        }

        public static DisposableList<T> Create(IEnumerable<T> copyFrom)
        {
            var val = new DisposableList<T>();
            val.list = ListPool<T>.Instantiate();
            val.list.AddRange(copyFrom);
            val._isAllocated = true;
            val._shouldDispose = true;
            return val;
        }

        public static DisposableList<T> Create()
        {
            var val = new DisposableList<T>();
            val.list = ListPool<T>.Instantiate();
            val._isAllocated = true;
            val._shouldDispose = true;
            return val;
        }

        public void AddRange(IList<T> collection)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            for (var i = 0; i < collection.Count; i++)
                list.Add(collection[i]);
            NotifyUsage();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            foreach (var item in collection)
                list.Add(item);
            NotifyUsage();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void NotifyUsage()
        {
#if UNITY_EDITOR && PURR_LEAKS_CHECK
            AllocationTracker.UpdateUsage(list);
#endif
        }

        public void Dispose()
        {
            if (isDisposed) return;

            if (_shouldDispose && list != null)
                ListPool<T>.Destroy(list);
            _isAllocated = false;
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            return GetEnumerator();
        }

        public void Add(T item)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            list.Add(item);
            NotifyUsage();
        }

        public void Clear()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            list.Clear();
            NotifyUsage();
        }

        public bool Contains(T item)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            list.CopyTo(array, arrayIndex);
            NotifyUsage();
        }

        public bool Remove(T item)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            return list.Remove(item);
        }

        public int Count
        {
            get
            {
                if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
                NotifyUsage();
                return list.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
                NotifyUsage();
                return false;
            }
        }

        private bool _isAllocated;

        public bool isDisposed => !_isAllocated;

        public int IndexOf(T item)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
            NotifyUsage();
            list.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
                NotifyUsage();
                return list[index];
            }
            set
            {
                if (isDisposed) throw new ObjectDisposedException(nameof(DisposableList<T>));
                NotifyUsage();
                list[index] = value;
            }
        }
    }
}
