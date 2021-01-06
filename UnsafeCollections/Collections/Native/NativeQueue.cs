/*
The MIT License (MIT)

Copyright (c) 2020 Dennis Corvers

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
    public unsafe struct NativeQueue<T> : INativeReadOnlyCollection<T> where T : unmanaged
    {
        private UnsafeQueue* m_inner;

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
                return UnsafeQueue.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeQueue.GetCapacity(m_inner);
            }
        }
        public bool IsFixedSize
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeQueue.IsFixedSize(m_inner);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeQueue* GetInnerCollection()
        {
            return m_inner;
        }

        public NativeQueue(int capacity)
        {
            m_inner = UnsafeQueue.Allocate<T>(capacity, false);
        }

        public NativeQueue(int capacity, bool fixedSize)
        {
            m_inner = UnsafeQueue.Allocate<T>(capacity, fixedSize);
        }


        public void Clear()
        {
            UnsafeQueue.Clear(m_inner);
        }

        public void Enqueue(T item)
        {
            UnsafeQueue.Enqueue(m_inner, item);
        }

        public bool TryEnqueue(T item)
        {
            return UnsafeQueue.TryEnqueue(m_inner, item);
        }

        public T Dequeue()
        {
            return UnsafeQueue.Dequeue<T>(m_inner);
        }

        public bool TryDequeue(out T result)
        {
            return UnsafeQueue.TryDequeue(m_inner, out result);
        }

        public T Peek()
        {
            return UnsafeQueue.Peek<T>(m_inner);
        }

        public bool TryPeek(out T result)
        {
            return UnsafeQueue.TryPeek<T>(m_inner, out result);
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
                UnsafeQueue.CopyTo<T>(m_inner, ptr, arrayIndex);
        }


        public UnsafeList.Enumerator<T> GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeQueue.Free(m_inner);
            m_inner = null;
        }
    }

    //Extension methods are used to add extra constraints to <T>
    public unsafe static class NativeQueueExtensions
    {
        public static bool Contains<T>(this NativeQueue<T> queue, T item) where T : unmanaged, IEquatable<T>
        {
            return UnsafeQueue.Contains(queue.GetInnerCollection(), item);
        }
    }
}
