using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnsafeCollections.Collections.Unsafe.Concurrent;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native.Concurrent
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeReadOnlyCollectionDebugView<>))]
    public unsafe struct NativeSPSCQueue<T> : INativeReadOnlyCollection<T>, IProducerConsumerCollection<T> where T : unmanaged
    {
        private UnsafeSPSCQueue* m_inner;

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
                return UnsafeSPSCQueue.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeSPSCQueue.GetCapacity(m_inner);
            }
        }
        public bool IsEmpty
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeSPSCQueue.IsEmpty<T>(m_inner);
            }
        }

        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => throw new NotSupportedException();


        public NativeSPSCQueue(int capacity)
        {
            m_inner = UnsafeSPSCQueue.Allocate<T>(capacity);
        }

        public void Enqueue(T item)
        {
            UnsafeSPSCQueue.Enqueue<T>(m_inner, item);
        }

        public bool TryEnqueue(T item)
        {
            return UnsafeSPSCQueue.TryEnqueue<T>(m_inner, item);
        }

        public T Dequeue()
        {
            return UnsafeSPSCQueue.Dequeue<T>(m_inner);
        }

        public bool TryDequeue(out T item)
        {
            return UnsafeSPSCQueue.TryDequeue<T>(m_inner, out item);
        }

        public T Peek()
        {
            return UnsafeSPSCQueue.Peek<T>(m_inner);
        }

        public bool TryPeek(out T item)
        {
            return UnsafeSPSCQueue.TryPeek<T>(m_inner, out item);
        }

        public void Clear()
        {
            UnsafeSPSCQueue.Clear(m_inner);
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

            UnsafeSPSCQueue.ToArray<T>(m_inner).CopyTo(array, arrayIndex);
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

        public T[] ToArray()
        {
            return UnsafeSPSCQueue.ToArray<T>(m_inner);
        }


        public UnsafeSPSCQueue.Enumerator<T> GetEnumerator()
        {
            return UnsafeSPSCQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeSPSCQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeSPSCQueue.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeSPSCQueue.Free(m_inner);
            m_inner = null;
        }
    }
}
