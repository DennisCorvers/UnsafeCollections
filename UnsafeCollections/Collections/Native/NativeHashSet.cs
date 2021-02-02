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
using UnsafeCollections.Collections.Unsafe;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeReadOnlyCollectionDebugView<>))]
    public unsafe struct NativeHashSet<T> : INativeReadOnlyCollection<T>, INativeCollection<T>
        where T : unmanaged, IEquatable<T>
    {
        private UnsafeHashSet* m_inner;

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
                return UnsafeHashSet.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeHashSet.GetCapacity(m_inner);
            }
        }
        public bool IsFixedSize
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeHashSet.IsFixedSize(m_inner);
            }
        }

        public bool IsReadOnly => false;

        public NativeHashSet(int capacity)
        {
            m_inner = UnsafeHashSet.Allocate<T>(capacity, false);
        }

        public NativeHashSet(int capacity, bool fixedSize)
        {
            m_inner = UnsafeHashSet.Allocate<T>(capacity, fixedSize);
        }


        public void Add(T item)
        {
            UnsafeHashSet.Add<T>(m_inner, item);
        }

        public void Clear()
        {
            UnsafeHashSet.Clear(m_inner);
        }

        public bool Contains(T item)
        {
            return UnsafeHashSet.Contains<T>(m_inner, item);
        }

        public bool Remove(T item)
        {
            return UnsafeHashSet.Remove<T>(m_inner, item);
        }


        public void IntersectsWith(NativeHashSet<T> other)
        {
            UnsafeHashSet.IntersectsWith<T>(m_inner, other.m_inner);
        }

        public void UnionWith(NativeHashSet<T> other)
        {
            UnsafeHashSet.UnionWith<T>(m_inner, other.m_inner);
        }

        public void ExceptWith(NativeHashSet<T> other)
        {
            UnsafeHashSet.ExceptWith<T>(m_inner, other.m_inner);
        }

        public void SymmetricExcept(NativeHashSet<T> other)
        {
            UnsafeHashSet.SymmetricExcept<T>(m_inner, other.m_inner);
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
                UnsafeHashSet.CopyTo<T>(m_inner, ptr, arrayIndex);
        }

        public NativeArray<T> ToNativeArray()
        {
            if (Count == 0)
                return NativeArray.Empty<T>();

            var arr = new NativeArray<T>(Count);
            UnsafeHashSet.CopyTo<T>(m_inner, UnsafeArray.GetBuffer(arr.GetInnerCollection()), 0);

            return arr;
        }


        public UnsafeHashSet.Enumerator<T> GetEnumerator()
        {
            return UnsafeHashSet.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeHashSet.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeHashSet.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [Unity.Collections.LowLevel.Unsafe.WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeHashSet.Free(m_inner);
            m_inner = null;
        }
    }
}
