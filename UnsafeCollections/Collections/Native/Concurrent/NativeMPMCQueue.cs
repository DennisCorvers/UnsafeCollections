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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnsafeCollections.Collections.Unsafe.Concurrent;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native.Concurrent
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeReadOnlyCollectionDebugView<>))]
    public unsafe struct NativeMPMCQueue<T> : INativeReadOnlyCollection<T>, IProducerConsumerCollection<T> where T : unmanaged
    {
        private UnsafeMPMCQueue* m_inner;

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
                return UnsafeMPMCQueue.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeMPMCQueue.GetCapacity(m_inner);
            }
        }
        public bool IsEmpty
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeMPMCQueue.IsEmpty<T>(m_inner);
            }
        }

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw new NotSupportedException();


        public NativeMPMCQueue(int segmentSize)
        {
            m_inner = UnsafeMPMCQueue.Allocate<T>(segmentSize);
        }

        public NativeMPMCQueue(int segmentSize, bool fixedSize)
        {
            m_inner = UnsafeMPMCQueue.Allocate<T>(segmentSize, fixedSize);
        }


        public bool TryEnqueue(T item)
        {
            return UnsafeMPMCQueue.TryEnqueue<T>(m_inner, item);
        }

        public bool TryDequeue(out T result)
        {
            return UnsafeMPMCQueue.TryDequeue<T>(m_inner, out result);
        }

        public bool TryPeek(out T result)
        {
            return UnsafeMPMCQueue.TryPeek<T>(m_inner, out result);
        }

        public void Clear()
        {
            UnsafeMPMCQueue.Clear(m_inner);
        }


        bool IProducerConsumerCollection<T>.TryAdd(T item)
        {
            return TryEnqueue(item);
        }

        bool IProducerConsumerCollection<T>.TryTake(out T item)
        {
            return TryDequeue(out item);
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

            UnsafeMPMCQueue.ToArray<T>(m_inner).CopyTo(array, arrayIndex);
        }

        public T[] ToArray()
        {
            return UnsafeMPMCQueue.ToArray<T>(m_inner);
        }


        void ICollection.CopyTo(Array array, int index)
        {
            if (array is T[] szArray)
            {
                CopyTo(szArray, index);
                return;
            }

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            ToArray().CopyTo(array, index);
        }

        public UnsafeMPMCQueue.Enumerator<T> GetEnumerator()
        {
            return UnsafeMPMCQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeMPMCQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeMPMCQueue.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeMPMCQueue.Free(m_inner);
            m_inner = null;
        }
    }
}
