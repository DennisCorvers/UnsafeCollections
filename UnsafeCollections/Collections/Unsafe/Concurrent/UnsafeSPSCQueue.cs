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
using System.Runtime.InteropServices;
using System.Threading;
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe.Concurrent
{
    /// <summary>
    /// A ringbuffer that acts as a queue. Buffer has a fixed size.
    /// </summary>
    public unsafe struct UnsafeSPSCQueue
    {
        UnsafeBuffer _items;
        IntPtr _typeHandle;         // Readonly
        HeadAndTail _headAndTail;

        /// <summary>
        /// Allocates a new SPSCRingbuffer. Capacity will be set to a power of 2.
        /// </summary>
        public static UnsafeSPSCQueue* Allocate<T>(int capacity) where T : unmanaged
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), string.Format(ThrowHelper.ArgumentOutOfRange_MustBePositive, nameof(capacity)));

            // Requires one extra element to distinguish between empty and full queue.
            capacity++;

            int stride = sizeof(T);

            var alignment = Memory.GetAlignment(stride);
            var sizeOfQueue = Memory.RoundToAlignment(sizeof(UnsafeSPSCQueue), alignment);
            var sizeOfArray = stride * capacity;

            var ptr = Memory.MallocAndZero(sizeOfQueue + sizeOfArray, alignment);

            UnsafeSPSCQueue* queue = (UnsafeSPSCQueue*)ptr;

            // initialize fixed buffer from same block of memory as the stack
            UnsafeBuffer.InitFixed(&queue->_items, (byte*)ptr + sizeOfQueue, capacity, stride);

            queue->_headAndTail = new HeadAndTail();
            queue->_typeHandle = typeof(T).TypeHandle.Value;

            return queue;
        }

        public static void Free(UnsafeSPSCQueue* queue)
        {
            if (queue == null)
                return;

            // clear queue memory (just in case)
            *queue = default;

            // free queue memory, if this is a fixed queue it frees the items memory at the same time
            Memory.Free(queue);
        }

        public static bool IsEmpty<T>(UnsafeSPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var nextHead = Volatile.Read(ref queue->_headAndTail.Head) + 1;

            return (Volatile.Read(ref queue->_headAndTail.Tail) < nextHead);
        }

        public static int GetCapacity(UnsafeSPSCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            return queue->_items.Length - 1;
        }

        public static int GetCount(UnsafeSPSCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            var head = Volatile.Read(ref queue->_headAndTail.Head);
            var tail = Volatile.Read(ref queue->_headAndTail.Tail);

            var dif = tail - head;
            if (dif < 0)
                dif += queue->_items.Length;

            return dif;
        }

        public static void Clear(UnsafeSPSCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            queue->_headAndTail = new HeadAndTail();
        }

        /// <summary>
        /// Enqueues an item in the queue. Blocks the thread until there is space in the queue.
        /// </summary>
        public static void Enqueue<T>(UnsafeSPSCQueue* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = default;
            var tail = Volatile.Read(ref queue->_headAndTail.Tail);
            var nextTail = GetNext(tail, queue->_items.Length);


            // Full Queue
            while (nextTail == Volatile.Read(ref queue->_headAndTail.Head))
                spinner.SpinOnce();

            *queue->_items.Element<T>(tail) = item;

            Volatile.Write(ref queue->_headAndTail.Tail, nextTail);
        }

        /// <summary>
        /// Tries to enqueue an item in the queue. Returns false if there's no space in the queue.
        /// </summary>
        public static bool TryEnqueue<T>(UnsafeSPSCQueue* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var tail = Volatile.Read(ref queue->_headAndTail.Tail);
            var nextTail = GetNext(tail, queue->_items.Length);

            // Full Queue
            if (nextTail == Volatile.Read(ref queue->_headAndTail.Head))
                return false;

            *queue->_items.Element<T>(tail) = item;

            Volatile.Write(ref queue->_headAndTail.Tail, nextTail);
            return true;
        }

        /// <summary>
        /// Dequeues an item from the queue. Blocks the thread until there is space in the queue.
        /// </summary>
        public static T Dequeue<T>(UnsafeSPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = default;
            var head = Volatile.Read(ref queue->_headAndTail.Head);

            // Queue empty
            while (Volatile.Read(ref queue->_headAndTail.Tail) == head)
            {
                spinner.SpinOnce();
            }

            var result = queue->_items.Element<T>(head);
            var nextHead = GetNext(head, queue->_items.Length);
            Volatile.Write(ref queue->_headAndTail.Head, nextHead);

            return *result;
        }

        /// <summary>
        /// Tries to dequeue an item from the queue. Returns false if there's no items in the queue.
        /// </summary>
        public static bool TryDequeue<T>(UnsafeSPSCQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var head = Volatile.Read(ref queue->_headAndTail.Head);

            // Queue empty
            if (Volatile.Read(ref queue->_headAndTail.Tail) == head)
            {
                result = default;
                return false;
            }

            result = *queue->_items.Element<T>(head);
            var nextHead = GetNext(head, queue->_items.Length);
            Volatile.Write(ref queue->_headAndTail.Head, nextHead);

            return true;
        }

        public static bool TryPeek<T>(UnsafeSPSCQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var head = Volatile.Read(ref queue->_headAndTail.Head);

            // Queue empty
            if (Volatile.Read(ref queue->_headAndTail.Tail) == head)
            {
                result = default;
                return false;
            }

            result = *queue->_items.Element<T>(head);

            return true;
        }

        /// <summary>
        /// Peeks the next item in the queue. Blocks the thread until an item is available.
        /// </summary>
        public static T Peek<T>(UnsafeSPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = default;
            var head = Volatile.Read(ref queue->_headAndTail.Head);

            // Queue empty
            if (Volatile.Read(ref queue->_headAndTail.Tail) == head)
            {
                spinner.SpinOnce();
            }

            return *queue->_items.Element<T>(head);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int GetNext(int value, int length)
        {
            value++;

            if (value == length)
                value = 0;
            return value;
        }

        /// <summary>
        /// Returns a snapshot of the elements in the queue.
        /// </summary>
        /// <returns></returns>
        public static T[] ToArray<T>(UnsafeSPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var head = Volatile.Read(ref queue->_headAndTail.Head);
            var tail = Volatile.Read(ref queue->_headAndTail.Tail);

            var count = tail - head;
            if (count < 0)
                count += queue->_items.Length;

            if (count <= 0)
                return Array.Empty<T>();

            var arr = new T[count];

            int numToCopy = count;
            int bufferLength = queue->_items.Length;
            int ihead = head;

            int firstPart = Math.Min(bufferLength - ihead, numToCopy);

            fixed (void* ptr = arr)
            {
                UnsafeBuffer.CopyTo<T>(queue->_items, ihead, ptr, 0, firstPart);
                numToCopy -= firstPart;

                if (numToCopy > 0)
                    UnsafeBuffer.CopyTo<T>(queue->_items, 0, ptr, 0 + bufferLength - ihead, numToCopy);
            }

            return arr;
        }

        /// <summary>
        /// Creates an enumerator for the current snapshot of the queue.
        /// </summary>
        public static Enumerator<T> GetEnumerator<T>(UnsafeSPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            return new Enumerator<T>(queue);
        }


        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            // Enumerates over the provided SPSCRingBuffer. Enumeration counts as a READ/Consume operation.
            // The amount of items enumerated can vary depending on if the TAIL moves during enumeration.
            // The HEAD is frozen in place when the enumerator is created. This means that the maximum 
            // amount of items read is always the capacity of the queue and no more.
            readonly UnsafeSPSCQueue* _queue;
            readonly int _headStart;
            int _index;
            int _capacity;
            T* _current;

            internal Enumerator(UnsafeSPSCQueue* queue)
            {
                _queue = queue;
                _index = -1;
                _current = default;
                _headStart = Volatile.Read(ref queue->_headAndTail.Head);
                _capacity = queue->_items.Length;
            }

            public void Dispose()
            {
                _index = -2;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_index == -2)
                    return false;

                var head = Volatile.Read(ref _queue->_headAndTail.Head);
                if (_headStart != head)
                    throw new InvalidOperationException(ThrowHelper.InvalidOperation_EnumFailedVersion);

                var headIndex = head + ++_index;

                if (headIndex >= _capacity)
                {
                    // Wrap around if needed
                    headIndex -= _capacity;
                }

                // Queue empty
                if (Volatile.Read(ref _queue->_headAndTail.Tail) == headIndex)
                {
                    _current = default;
                    return false;
                }

                _current = _queue->_items.Element<T>(headIndex);

                return true;
            }

            public void Reset()
            {
                _index = -1;
                _current = default;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_current != null);
                    return *_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index < 0)
                        throw new InvalidOperationException();

                    return Current;
                }
            }

            public Enumerator<T> GetEnumerator()
            {
                return this;
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
}
