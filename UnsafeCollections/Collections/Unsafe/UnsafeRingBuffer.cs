/*
The MIT License (MIT)

Copyright (c) 2021 Dennis Corvers

This software is based on, a modification of and/or an extention 
of "UnsafeCollections" originally authored by:

The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

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
using System.Runtime.CompilerServices;
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeRingBuffer
    {
        UnsafeBuffer _items;
        IntPtr _typeHandle;

        int _head;
        int _tail;
        int _count;
        int _overwrite;

        public static UnsafeRingBuffer* Allocate<T>(int capacity) where T : unmanaged
        {
            return Allocate<T>(capacity, true);
        }

        public static UnsafeRingBuffer* Allocate<T>(int capacity, bool overwrite) where T : unmanaged
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), string.Format(ThrowHelper.ArgumentOutOfRange_MustBePositive, nameof(capacity)));

            int stride = sizeof(T);

            // fixedSize means we are allocating the memory for the collection header and the items in it as one block
            var alignment = Memory.GetAlignment(stride);

            // align header size to the elements alignment
            var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeRingBuffer), alignment);
            var sizeOfBuffer = stride * capacity;

            // allocate memory for list and array with the correct alignment
            var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBuffer, alignment);

            // grab header ptr
            var ring = (UnsafeRingBuffer*)ptr;

            // initialize fixed buffer from same block of memory as the collection, offset by sizeOfHeader
            UnsafeBuffer.InitFixed(&ring->_items, (byte*)ptr + sizeOfHeader, capacity, stride);

            // initialize count to 0
            ring->_count = 0;
            ring->_overwrite = overwrite ? 1 : 0;
            ring->_typeHandle = typeof(T).TypeHandle.Value;
            return ring;
        }

        public static void Free(UnsafeRingBuffer* ring)
        {
            if (ring == null)
                return;

            // clear memory just in case
            *ring = default;

            // release ring memory
            Memory.Free(ring);
        }

        public static int GetCapacity(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            return ring->_items.Length;
        }

        public static int GetCount(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            return ring->_count;
        }

        public static void Clear(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);

            ring->_tail = 0;
            ring->_head = 0;
            ring->_count = 0;
        }

        public static bool IsFull(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            return ring->_count == ring->_items.Length;
        }

        public static void Set<T>(UnsafeRingBuffer* ring, int index, T value) where T : unmanaged
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ring->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)ring->_count)
            {
                throw new IndexOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);
            }

            // assign element
            *ring->_items.Element<T>((ring->_tail + index) % ring->_items.Length) = value;
        }


        public static T Get<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged
        {
            return *GetPtr<T>(ring, index);
        }

        public static T* GetPtr<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ring->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)ring->_count)
            {
                throw new IndexOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);
            }
            return ring->_items.Element<T>((ring->_tail + index) % ring->_items.Length);
        }

        public static ref T GetRef<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged
        {
            return ref *GetPtr<T>(ring, index);
        }


        public static bool Push<T>(UnsafeRingBuffer* ring, T item) where T : unmanaged
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ring->_typeHandle);

            var count = ring->_count;
            var items = ring->_items;

            if (count == items.Length)
            {
                if (ring->_overwrite == 1)
                {
                    MoveNext(items.Length, ref ring->_head);
                    ring->_count--;
                }
                else
                {
                    return false;
                }
            }

            var tail = ring->_tail;
            *items.Element<T>(tail) = item;

            ring->_count++;
            MoveNext(items.Length, ref ring->_tail);

            return true;
        }

        public static bool Pop<T>(UnsafeRingBuffer* ring, out T value) where T : unmanaged
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ring->_typeHandle);

            var count = ring->_count;
            if (count == 0)
            {
                value = default;
                return false;
            }

            var head = ring->_head;
            var items = ring->_items;

            // grab result
            value = *items.Element<T>(head);

            // decrement count and head index
            ring->_count--;
            MoveNext(items.Length, ref ring->_head);

            return true;
        }

        public static bool Peek<T>(UnsafeRingBuffer* ring, out T value) where T : unmanaged
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ring->_typeHandle);

            if (ring->_count == 0)
            {
                value = default;
                return false;
            }

            value = *ring->_items.Element<T>(ring->_head);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void MoveNext(int length, ref int index)
        {
            // Taken from the .NET Core implementation:
            // It is tempting to use the remainder operator here but it is actually much slower
            // than a simple comparison and a rarely taken branch.
            // JIT produces better code than with ternary operator ?:
            int tmp = index + 1;
            if (tmp == length)
            {
                tmp = 0;
            }
            index = tmp;
        }


        public static bool Contains<T>(UnsafeRingBuffer* ringbuffer, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(ringbuffer != null);
            UDebug.Assert(ringbuffer->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ringbuffer->_typeHandle);

            int count = ringbuffer->_count;
            int head = ringbuffer->_head;
            int tail = ringbuffer->_tail;

            if (count == 0)
            {
                return false;
            }

            if (head < tail)
            {
                return UnsafeBuffer.IndexOf(ringbuffer->_items, item, head, count) > -1;
            }

            return UnsafeBuffer.IndexOf(ringbuffer->_items, item, head, ringbuffer->_items.Length - head) > -1 ||
                   UnsafeBuffer.IndexOf(ringbuffer->_items, item, 0, tail) > -1;
        }

        public static void CopyTo<T>(UnsafeRingBuffer* ringbuffer, void* destination, int destinationIndex) where T : unmanaged
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);

            UDebug.Assert(ringbuffer != null);
            UDebug.Assert(ringbuffer->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ringbuffer->_typeHandle);


            int numToCopy = ringbuffer->_count;
            if (numToCopy == 0)
            {
                return;
            }

            int bufferLength = ringbuffer->_items.Length;
            int head = ringbuffer->_head;

            int firstPart = Math.Min(bufferLength - head, numToCopy);
            UnsafeBuffer.CopyTo<T>(ringbuffer->_items, head, destination, destinationIndex, firstPart);
            numToCopy -= firstPart;
            if (numToCopy > 0)
            {
                UnsafeBuffer.CopyTo<T>(ringbuffer->_items, 0, destination, destinationIndex + bufferLength - head, numToCopy);
            }
        }

        public static UnsafeList.Enumerator<T> GetEnumerator<T>(UnsafeRingBuffer* ringbuffer) where T : unmanaged
        {
            UDebug.Assert(ringbuffer != null);
            UDebug.Assert(ringbuffer->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == ringbuffer->_typeHandle);

            return new UnsafeList.Enumerator<T>(ringbuffer->_items, ringbuffer->_head, ringbuffer->_count);
        }
    }
}