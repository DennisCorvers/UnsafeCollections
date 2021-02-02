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
    public unsafe struct NativeRingBuffer<T> : INativeReadOnlyCollection<T> where T : unmanaged
    {
        private UnsafeRingBuffer* m_inner;

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
                return UnsafeRingBuffer.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeRingBuffer.GetCapacity(m_inner);
            }
        }
        public bool IsFull
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeRingBuffer.IsFull(m_inner);
            }
        }

        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return UnsafeRingBuffer.Get<T>(m_inner, index);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                UnsafeRingBuffer.Set(m_inner, index, value);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeRingBuffer* GetInnerCollection()
        {
            return m_inner;
        }

        public NativeRingBuffer(int capacity)
        {
            m_inner = UnsafeRingBuffer.Allocate<T>(capacity);
        }

        public NativeRingBuffer(int capacity, bool overwrite)
        {
            m_inner = UnsafeRingBuffer.Allocate<T>(capacity, overwrite);
        }


        public void Clear()
        {
            UnsafeRingBuffer.Clear(m_inner);
        }

        public ref T GetRef(int index)
        {
            return ref UnsafeRingBuffer.GetRef<T>(m_inner, index);
        }


        public bool Push(T item)
        {
            return UnsafeRingBuffer.Push(m_inner, item);
        }

        public bool Pop(out T value)
        {
            return UnsafeRingBuffer.Pop(m_inner, out value);
        }

        public bool Peek(out T value)
        {
            return UnsafeRingBuffer.Peek(m_inner, out value);
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
                UnsafeRingBuffer.CopyTo<T>(m_inner, ptr, arrayIndex);
        }

        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();

            var arr = new T[Count];

            CopyTo(arr, 0);

            return arr;
        }

        public NativeArray<T> ToNativeArray()
        {
            if (Count == 0)
                return NativeArray.Empty<T>();

            var arr = new NativeArray<T>(Count);
            UnsafeRingBuffer.CopyTo<T>(m_inner, UnsafeArray.GetBuffer(arr.GetInnerCollection()), 0);

            return arr;
        }

        public UnsafeList.Enumerator<T> GetEnumerator()
        {
            return UnsafeRingBuffer.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeRingBuffer.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeRingBuffer.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [Unity.Collections.LowLevel.Unsafe.WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeRingBuffer.Free(m_inner);
            m_inner = null;
        }
    }

    //Extension methods are used to add extra constraints to <T>
    public unsafe static class NativeRingBufferExtensions
    {
        public static bool Contains<T>(this NativeRingBuffer<T> ringBuffer, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeRingBuffer.Contains(ringBuffer.GetInnerCollection(), item);
        }
    }
}
