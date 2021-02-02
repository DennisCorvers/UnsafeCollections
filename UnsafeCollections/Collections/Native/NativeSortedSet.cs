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
    [DebuggerTypeProxy(typeof(NativeReadOnlyCollectionDebugView<>))]
    public unsafe struct NativeSortedSet<T> : INativeReadOnlyCollection<T>, INativeCollection<T>
        where T : unmanaged, IComparable<T>
    {
        private UnsafeSortedSet* m_inner;

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
                return UnsafeSortedSet.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeSortedSet.GetCapacity(m_inner);
            }
        }
        public bool IsFixedSize
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeSortedSet.IsFixedSize(m_inner);
            }
        }

        public bool IsReadOnly => false;

        public NativeSortedSet(int capacity)
        {
            m_inner = UnsafeSortedSet.Allocate<T>(capacity, false);
        }

        public NativeSortedSet(int capacity, bool fixedSize)
        {
            m_inner = UnsafeSortedSet.Allocate<T>(capacity, fixedSize);
        }


        public void Add(T item)
        {
            UnsafeSortedSet.Add<T>(m_inner, item);
        }

        public void Clear()
        {
            UnsafeSortedSet.Clear(m_inner);
        }

        public bool Contains(T item)
        {
            return UnsafeSortedSet.Contains<T>(m_inner, item);
        }

        public bool Remove(T item)
        {
            return UnsafeSortedSet.Remove<T>(m_inner, item);
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
                UnsafeSortedSet.CopyTo<T>(m_inner, ptr, arrayIndex);
        }

        public NativeArray<T> ToNativeArray()
        {
            if (Count == 0)
                return NativeArray.Empty<T>();

            var arr = new NativeArray<T>(Count);
            UnsafeSortedSet.CopyTo<T>(m_inner, UnsafeArray.GetBuffer(arr.GetInnerCollection()), 0);

            return arr;
        }


        public UnsafeSortedSet.Enumerator<T> GetEnumerator()
        {
            return UnsafeSortedSet.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeSortedSet.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeSortedSet.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [Unity.Collections.LowLevel.Unsafe.WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeSortedSet.Free(m_inner);
            m_inner = null;
        }
    }
}
