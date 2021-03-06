﻿/*
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe.Concurrent
{
    [DebuggerDisplay("Capacity = {Capacity}")]
    internal unsafe struct QueueSegment
    {
        // Implementation based on https://source.dot.net/#System.Private.CoreLib/ConcurrentQueueSegment.cs

#pragma warning disable IDE0032
        void* _items;    // Readonly
        int _capacity;   // Readonly
        int _slotStride; // Readonly
        int _slotOffset; // Readonly
        int _mask;       // Readonly
#pragma warning restore IDE0032

        internal QueueSegment* _nextSegment;
        internal HeadAndTail _headAndTail;

        internal bool _preserved;
        internal bool _frozen;

        internal int Capacity
            => _capacity;
        internal int FreezeOffset
            => _capacity * 2;
        internal int Mask
            => _mask;

        internal static QueueSegment* Allocate(int capacity, int slotStride, int slotOffset)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), string.Format(ThrowHelper.ArgumentOutOfRange_MustBePositive, nameof(capacity)));

            UDebug.Assert(slotStride > 0);
            UDebug.Assert(slotOffset > 0);

            capacity = Memory.RoundUpToPowerOf2(capacity);

            int alignment = Memory.GetAlignment(slotStride);

            var sizeOfQueue = Memory.RoundToAlignment(sizeof(UnsafeMPSCQueue), alignment);
            var sizeOfArray = slotStride * capacity;

            var ptr = Memory.MallocAndZero(sizeOfQueue + sizeOfArray, alignment);
            QueueSegment* queue = (QueueSegment*)ptr;

            // Readonly values
            queue->_items = (byte*)ptr + sizeOfQueue;
            queue->_capacity = capacity;
            queue->_slotStride = slotStride;
            queue->_slotOffset = slotOffset;
            queue->_mask = capacity - 1;

            queue->_headAndTail = default;
            queue->_nextSegment = default;

            queue->_frozen = false;
            queue->_preserved = false;

            // Set all sequence numbers for this segment
            for (int i = 0; i < queue->_capacity; i++)
                *queue->SequenceNumber(i) = i;

            return queue;
        }

        internal static void Free(QueueSegment* segment)
        {
            if (segment == null)
                return;

            *segment = default;

            Memory.Free(segment);
        }


        internal void EnsureFrozenForEnqueues()
        {
            // Shifts the tail by the entire length of the segment.
            // Makes it impossible for enqueuers to enqueue more data
            // as long as the segment is frozen.

            if (!_frozen)
            {
                _frozen = true;
                Interlocked.Add(ref _headAndTail.Tail, FreezeOffset);
            }
        }


        public bool TryDequeue<T>(out T item) where T : unmanaged
        {
            SpinWait spinner = default;
            while (true)
            {
                int head = Volatile.Read(ref _headAndTail.Head);
                int index = head & _mask;

                int seq = Volatile.Read(ref *SequenceNumber(index));
                int dif = seq - (head + 1);

                if (dif == 0)
                {
                    if (Interlocked.CompareExchange(ref _headAndTail.Head, head + 1, head) == head)
                    {
                        item = *Element<T>(index);

                        // Slot is preserved in case of enumerating, peeking, ToArray, etc.
                        if (!Volatile.Read(ref _preserved))
                        {
                            Volatile.Write(ref *SequenceNumber(index), head + _capacity);
                        }

                        return true;
                    }
                }
                else if (dif < 0)
                {
                    // Slot was empty
                    bool frozen = _frozen;
                    int tail = Volatile.Read(ref _headAndTail.Tail);

                    if (tail - head <= 0 || (frozen && (tail - FreezeOffset - head <= 0)))
                    {
                        item = default;
                        return false;
                    }
                }

                // Lost the race
                spinner.SpinOnce();
            }
        }

        public bool TryPeek<T>(out T item, bool resultUsed) where T : unmanaged
        {
            if (resultUsed)
            {
                _preserved = true;
                Interlocked.MemoryBarrier();
            }

            SpinWait spinner = default;
            while (true)
            {
                int head = Volatile.Read(ref _headAndTail.Head);
                int index = head & _mask;

                int seq = Volatile.Read(ref *SequenceNumber(index));
                int dif = seq - (head + 1);

                if (dif == 0)
                {
                    // Won the race, read item from slot
                    item = resultUsed ? *Element<T>(index) : default;
                    return true;
                }
                else if (dif < 0)
                {
                    bool frozen = _frozen;
                    int tail = Volatile.Read(ref _headAndTail.Tail);

                    // Slot is empty
                    if (tail - head <= 0 || (frozen && (tail - FreezeOffset - head <= 0)))
                    {
                        item = default;
                        return false;
                    }
                }

                // Lost the race
                spinner.SpinOnce();
            }
        }

        public bool TryEnqueue<T>(T item) where T : unmanaged
        {
            SpinWait spinner = default;

            while (true)
            {
                int tail = Volatile.Read(ref _headAndTail.Tail);
                int index = tail & _mask;

                int seq = Volatile.Read(ref *SequenceNumber(index));
                int dif = seq - tail;

                if (dif == 0)
                {
                    if (Interlocked.CompareExchange(ref _headAndTail.Tail, tail + 1, tail) == tail)
                    {
                        // Won the race
                        *Element<T>(index) = item;
                        Volatile.Write(ref *SequenceNumber(index), tail + 1);

                        return true;
                    }
                }
                else if (dif < 0)
                {
                    // No free slot
                    return false;
                }

                // Lost the race
                spinner.SpinOnce();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int* SequenceNumber(int index)
        {
            return (int*)((byte*)_items + (index * _slotStride) + _slotOffset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T* Element<T>(int index) where T : unmanaged
        {
            return (T*)((byte*)_items + (index * _slotStride));
        }
    }
}
