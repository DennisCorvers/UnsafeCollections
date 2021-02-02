/*
The MIT License (MIT)

Copyright (c) 2021 Dennis Corvers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnsafeCollections.Collections.Unsafe;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeCollectionDebugView<>))]
    public unsafe struct NativeList<T> : INativeCollection<T>, INativeReadOnlyCollection<T> where T : unmanaged
    {
        private UnsafeList* m_inner;

        public bool IsCreated
        {
            get
            {
                return m_inner != null;
            }
        }
        public int Count
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeList.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeList.GetCapacity(m_inner);
            }
        }
        public bool IsFixedSize
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeList.IsFixedSize(m_inner);
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return UnsafeList.Get<T>(m_inner, index);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                UnsafeList.Set(m_inner, index, value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeList* GetInnerCollection()
        {
            return m_inner;
        }

        public NativeList(int capacity)
        {
            m_inner = UnsafeList.Allocate<T>(capacity, false);
        }

        public NativeList(int capacity, bool fixedSize)
        {
            m_inner = UnsafeList.Allocate<T>(capacity, fixedSize);
        }

        public int SetCapacity(int capacity)
        {
            UnsafeList.SetCapacity(m_inner, capacity);

            return Capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            UnsafeList.Add(m_inner, item);
        }

        public void AddRange(ICollection<T> items)
        {
            if (Capacity < Count + items.Count)
                SetCapacity(Count + items.Count);

            int index = Count;
            using (var enumerator = items.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    Add(enumerator.Current);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef(int index)
        {
            return ref UnsafeList.GetRef<T>(m_inner, index);
        }

        public void RemoteAt(int index)
        {
            UnsafeList.RemoveAt(m_inner, index);
        }

        public void RemoveAtUnordered(int index)
        {
            UnsafeList.RemoveAtUnordered(m_inner, index);
        }

        public void Clear()
        {
            UnsafeList.Clear(m_inner);
        }

        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();

            var arr = new T[Count];

            CopyTo(arr, 0);

            return arr;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if ((uint)arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Insufficient space in the target location to copy the information.");

            if (array.Length == 0)
                return;

            fixed (void* ptr = array)
                UnsafeList.CopyTo<T>(m_inner, ptr, arrayIndex);
        }

        public NativeArray<T> ToNativeArray()
        {
            if (Count == 0)
                return NativeArray.Empty<T>();

            var arr = new NativeArray<T>(Count);
            UnsafeList.CopyTo<T>(m_inner, UnsafeArray.GetBuffer(arr.GetInnerCollection()), 0);

            return arr;
        }

        public UnsafeList.Enumerator<T> GetEnumerator()
        {
            return UnsafeList.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeList.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeList.GetEnumerator<T>(m_inner);
        }

        bool ICollection<T>.Contains(T item)
        {
            return IndexOfSlow(item) > -1;
        }

        bool ICollection<T>.Remove(T item)
        {
            int removeIndex = IndexOfSlow(item);

            if (removeIndex > -1)
            {
                UnsafeList.RemoveAt(m_inner, removeIndex);
                return true;
            }

            return false;
        }

        private int IndexOfSlow(T item)
        {
            var comparer = EqualityComparer<T>.Default;

            for (int i = 0; i < Count; i++)
            {
                if (comparer.Equals(this[i], item))
                    return i;
            }

            return -1;
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeList.Free(m_inner);
            m_inner = null;
        }
    }

    // Extension methods are used to add extra constraints to <T>
    public unsafe static class NativeListExtensions
    {
        public static bool Contains<T>(this NativeList<T> list, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeList.Contains(list.GetInnerCollection(), item);
        }

        public static int IndexOf<T>(this NativeList<T> list, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeList.IndexOf(list.GetInnerCollection(), item);
        }

        public static int LastIndexOf<T>(this NativeList<T> list, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeList.LastIndexOf(list.GetInnerCollection(), item);
        }

        public static bool Remove<T>(this NativeList<T> list, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeList.Remove(list.GetInnerCollection(), item);
        }

        public static bool RemoveUnordered<T>(this NativeList<T> list, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeList.RemoveUnordered(list.GetInnerCollection(), item);
        }
    }
}
