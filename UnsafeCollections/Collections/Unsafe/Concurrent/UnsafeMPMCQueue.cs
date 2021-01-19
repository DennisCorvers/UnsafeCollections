using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe.Concurrent
{
    public unsafe struct UnsafeMPMCQueue
    {
        // Implementation based on https://source.dot.net/#System.Private.CoreLib/ConcurrentQueue.cs

        const int INITIAL_SEGMENT_LENGTH = 32;
        const int MAX_SEGMENT_LENGTH = 1024 * 1024;

        IntPtr _typeHandle; // Readonly
        int _slotStride;    // Readonly
        int _slotOffset;    // Readonly

        UnsafeLock _crossSegmentLock;
        volatile QueueSegment* _tail;
        volatile QueueSegment* _head;

        bool _fixedSize;    // Readonly

        /// <summary>
        /// Creates a Multi-Producer, Multi-Consumer concurrent queue.
        /// </summary>
        public static UnsafeMPMCQueue* Allocate<T>() where T : unmanaged
        {
            return Allocate<T>(INITIAL_SEGMENT_LENGTH, false);
        }

        /// <summary>
        /// Creates a Multi-Producer, Multi-Consumer concurrent queue.
        /// </summary>
        /// <param name="segmentSize">The size of a queue segment or the queue capacity when fixedSize = true.</param>
        /// <param name="fixedSize">Creates a queue with a fixed size if true.</param>
        public static UnsafeMPMCQueue* Allocate<T>(int segmentSize = INITIAL_SEGMENT_LENGTH, bool fixedSize = false) where T : unmanaged
        {
            if (segmentSize < 1)
                throw new ArgumentOutOfRangeException(nameof(segmentSize), string.Format(ThrowHelper.ArgumentOutOfRange_MustBePositive, nameof(segmentSize)));

            int slotStride = Marshal.SizeOf(new QueueSlot<T>());
            int slotAlign = Memory.GetMaxAlignment(sizeof(T), sizeof(int));
            int slotOffset = Memory.RoundToAlignment(sizeof(T), slotAlign);

            var queue = Memory.MallocAndZero<UnsafeMPMCQueue>();
            queue->_typeHandle = typeof(T).TypeHandle.Value;
            queue->_slotStride = slotStride;
            queue->_slotOffset = slotOffset;
            queue->_fixedSize = fixedSize;
            queue->_tail = queue->_head = QueueSegment.Allocate(segmentSize, slotStride, slotOffset);

            return queue;
        }

        public static void Free(UnsafeMPMCQueue* queue)
        {
            if (queue == null)
                return;

            // Free all segments
            var segment = queue->_head;
            do
            {
                var next = segment->_nextSegment;
                QueueSegment.Free(segment);
                segment = next;
            }
            while (segment != null);

            // Clear queue memory (just in case)
            *queue = default;

            // Free queue memory, if this is a fixed queue it frees the items memory at the same time
            Memory.Free(queue);
        }

        public static int GetCapacity(UnsafeMPMCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);

            SpinWait spinner = default;
            while (true)
            {
                // Capture head and tail
                var headSeg = queue->_head;
                var tailSeg = queue->_tail;

                // Single QueueSegment
                if (headSeg == tailSeg)
                {
                    if (headSeg == queue->_head && tailSeg == queue->_tail)
                        return headSeg->Capacity;
                }
                // Two QueueSegments
                else if (headSeg->_nextSegment == tailSeg)
                {
                    if (headSeg == queue->_head && tailSeg == queue->_tail)
                        return headSeg->Capacity + tailSeg->Capacity;
                }
                // Mutliple QueueSegments (requires locking)
                else
                {
                    // Aquire cross-segment lock
                    queue->_crossSegmentLock.Lock();

                    if (headSeg == queue->_head && tailSeg == queue->_tail)
                    {
                        int capacity = headSeg->Capacity + tailSeg->Capacity;

                        for (QueueSegment* s = headSeg->_nextSegment; s != tailSeg; s = s->_nextSegment)
                        {
                            UDebug.Assert(s->_frozen, "Internal segment must be frozen as there's a following segment.");
                            capacity += s->Capacity;
                        }

                        // Release cross-segment lock
                        queue->_crossSegmentLock.Unlock();

                        return capacity;
                    }

                    // Release cross-segment lock
                    queue->_crossSegmentLock.Unlock();
                }

                spinner.SpinOnce();
            }
        }

        public static bool IsEmpty<T>(UnsafeMPMCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            return !queue->TryPeek(out T result, false);
        }

        public static int GetCount(UnsafeMPMCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);

            SpinWait spinner = default;
            while (true)
            {
                // Capture head and tail
                var headSeg = queue->_head;
                var tailSeg = queue->_tail;
                int headSeg_Head = Volatile.Read(ref headSeg->_headAndTail.Head);
                int headSeg_Tail = Volatile.Read(ref headSeg->_headAndTail.Tail);

                // Single QueueSegment
                if (headSeg == tailSeg)
                {
                    // Count can be reliably determined if the captured segments still match.
                    if (headSeg == queue->_head && tailSeg == queue->_tail &&
                        headSeg_Head == Volatile.Read(ref headSeg->_headAndTail.Head) &&
                        headSeg_Tail == Volatile.Read(ref headSeg->_headAndTail.Tail))
                    {
                        return GetCount(headSeg, headSeg_Head, headSeg_Tail);
                    }
                }
                // Two QueueSegments
                else if (headSeg->_nextSegment == tailSeg)
                {
                    int tailSeg_Head = Volatile.Read(ref tailSeg->_headAndTail.Head);
                    int tailSeg_Tail = Volatile.Read(ref tailSeg->_headAndTail.Tail);

                    if (headSeg == queue->_head &&
                        tailSeg == queue->_tail &&
                        headSeg_Head == Volatile.Read(ref headSeg->_headAndTail.Head) &&
                        headSeg_Tail == Volatile.Read(ref headSeg->_headAndTail.Tail) &&
                        tailSeg_Head == Volatile.Read(ref tailSeg->_headAndTail.Head) &&
                        tailSeg_Tail == Volatile.Read(ref tailSeg->_headAndTail.Tail))
                    {
                        return GetCount(headSeg, headSeg_Head, headSeg_Tail) + GetCount(tailSeg, tailSeg_Head, tailSeg_Tail);
                    }
                }
                // Mutliple QueueSegments (requires locking)
                else
                {
                    // Aquire cross-segment lock
                    queue->_crossSegmentLock.Lock();

                    if (headSeg == queue->_head && tailSeg == queue->_tail)
                    {
                        int tailSeg_Head = Volatile.Read(ref tailSeg->_headAndTail.Head);
                        int tailSeg_Tail = Volatile.Read(ref tailSeg->_headAndTail.Tail);

                        if (headSeg_Head == Volatile.Read(ref headSeg->_headAndTail.Head) &&
                            headSeg_Tail == Volatile.Read(ref headSeg->_headAndTail.Tail) &&
                            tailSeg_Head == Volatile.Read(ref tailSeg->_headAndTail.Head) &&
                            tailSeg_Tail == Volatile.Read(ref tailSeg->_headAndTail.Tail))
                        {
                            int count = GetCount(headSeg, headSeg_Head, headSeg_Tail) + GetCount(tailSeg, tailSeg_Head, tailSeg_Tail);

                            for (QueueSegment* s = headSeg->_nextSegment; s != tailSeg; s = s->_nextSegment)
                            {
                                UDebug.Assert(s->_frozen, "Internal segment must be frozen as there's a following segment.");
                                count += s->_headAndTail.Tail - s->FreezeOffset;
                            }

                            // Release cross-segment lock
                            queue->_crossSegmentLock.Unlock();

                            return count;
                        }
                    }

                    // Release cross-segment lock
                    queue->_crossSegmentLock.Unlock();
                }

                spinner.SpinOnce();
            }
        }

        /// <summary>
        /// Calculates the amount of items in a specific QueueSegment.
        /// </summary>
        private static int GetCount(QueueSegment* segment, int head, int tail)
        {
            if (head != tail && head != tail - segment->FreezeOffset)
            {
                head &= segment->Mask;
                tail &= segment->Mask;
                return head < tail ? tail - head : segment->Capacity - head + tail;
            }
            return 0;
        }

        /// <summary>
        /// Calculates the amount of items over a snapped region of QueueSegments.
        /// </summary>
        private static long GetCount(QueueSegment* headSeg, int headSeg_Head, QueueSegment* tailSeg, int tailSeg_Tail)
        {
            UDebug.Assert(headSeg->_preserved);
            UDebug.Assert(headSeg->_frozen);
            UDebug.Assert(tailSeg->_preserved);
            UDebug.Assert(tailSeg->_frozen);

            long count = 0;

            int headSeg_Tail = (headSeg == tailSeg ? tailSeg_Tail : Volatile.Read(ref headSeg->_headAndTail.Tail)) - headSeg->FreezeOffset;
            if (headSeg_Head < headSeg_Tail)
            {
                headSeg_Head &= headSeg->Mask;
                headSeg_Tail &= headSeg->Mask;

                count += headSeg_Head < headSeg_Tail ?
                    headSeg_Tail - headSeg_Head :
                    headSeg->Capacity - headSeg_Head + headSeg_Tail;
            }

            if (headSeg != tailSeg)
            {
                for (QueueSegment* s = headSeg->_nextSegment; s != tailSeg; s = s->_nextSegment)
                {
                    UDebug.Assert(s->_preserved);
                    UDebug.Assert(s->_frozen);
                    count += s->_headAndTail.Tail - s->FreezeOffset;
                }

                count += tailSeg_Tail - tailSeg->FreezeOffset;
            }

            return count;
        }

        private void SnapForObservation(out QueueSegment* headSeg, out int headSeg_Head, out QueueSegment* tailSeg, out int tailSeg_Tail)
        {
            _crossSegmentLock.Lock();

            headSeg = _head;
            tailSeg = _tail;

            UDebug.Assert(headSeg != null);
            UDebug.Assert(tailSeg != null);
            UDebug.Assert(tailSeg->_nextSegment == null);

            for (QueueSegment* s = headSeg; ; s = s->_nextSegment)
            {
                s->_preserved = true;
                if (s == tailSeg) break;
                UDebug.Assert(s->_frozen);
            }

            tailSeg->EnsureFrozenForEnqueues();

            headSeg_Head = Volatile.Read(ref headSeg->_headAndTail.Head);
            tailSeg_Tail = Volatile.Read(ref tailSeg->_headAndTail.Tail);

            _crossSegmentLock.Unlock();
        }

        /// <summary>
        /// Tries to enqueue an item in the queue. Returns false if there's no space in the queue.
        /// </summary>
        public static bool TryEnqueue<T>(UnsafeMPMCQueue* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            if (!queue->_tail->TryEnqueue(item))
            {
                return queue->TryEnqueueSlow(item);
            }

            return true;
        }

        private bool TryEnqueueSlow<T>(T item) where T : unmanaged
        {
            while (true)
            {
                QueueSegment* tail = _tail;

                if (tail->TryEnqueue(item))
                    return true;

                // Do not create a new segment if the queue has a fixed size.
                if (_fixedSize)
                    return false;

                _crossSegmentLock.Lock();
                if (tail == _tail)
                {
                    tail->EnsureFrozenForEnqueues();

                    int nextSize = tail->_preserved ? INITIAL_SEGMENT_LENGTH : Math.Min(tail->Capacity * 2, MAX_SEGMENT_LENGTH);
                    var newTail = QueueSegment.Allocate(nextSize, _slotStride, _slotOffset);

                    tail->_nextSegment = newTail;
                    _tail = newTail;
                }

                _crossSegmentLock.Unlock();
            }
        }

        /// <summary>
        /// Tries to dequeue an item from the queue. Returns false if there's no items in the queue.
        /// </summary>
        public static bool TryDequeue<T>(UnsafeMPMCQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            QueueSegment* head = queue->_head;

            if (head->TryDequeue(out result))
                return true;

            if (head->_nextSegment == null)
            {
                result = default;
                return false;
            }

            return queue->TryDequeueSlow(out result);
        }

        private bool TryDequeueSlow<T>(out T result) where T : unmanaged
        {
            while (true)
            {
                QueueSegment* head = _head;

                if (head->TryDequeue(out result))
                    return true;

                if (head->_nextSegment == null)
                {
                    result = default;
                    return false;
                }

                UDebug.Assert(head->_frozen);

                if (head->TryDequeue(out result))
                    return true;

                _crossSegmentLock.Lock();
                if (head == _head)
                {
                    var next = head->_nextSegment;
                    QueueSegment.Free(head);

                    _head = next;
                }
                _crossSegmentLock.Unlock();
            }
        }

        public static bool TryPeek<T>(UnsafeMPMCQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);


            return queue->TryPeek(out result, true);
        }

        private bool TryPeek<T>(out T result, bool useResult = false) where T : unmanaged
        {
            QueueSegment* head = _head;

            while (true)
            {
                QueueSegment* next = VolatileRead(ref head->_nextSegment);

                if (head->TryPeek(out result, useResult))
                    return true;

                if (next != null)
                {
                    UDebug.Assert(next == head->_nextSegment);
                    head = next;
                }
                else if (VolatileRead(ref head->_nextSegment) == null)
                {
                    break;
                }
            }

            result = default;
            return false;
        }

        private static QueueSegment* VolatileRead(ref QueueSegment* segment)
        {
            var value = segment;
            Thread.MemoryBarrier();
            return value;
        }

        private static T* GetItemWhenAvailable<T>(QueueSegment* segment, int index) where T : unmanaged
        {
            UDebug.Assert(segment->_preserved);
            int expectedSeq = (index + 1) & segment->Mask;

            if ((*segment->SequenceNumber(index) & segment->Mask) != expectedSeq)
            {
                SpinWait spinner = default;
                while ((Volatile.Read(ref *segment->SequenceNumber(index)) & segment->Mask) != expectedSeq)
                    spinner.SpinOnce();
            }

            return segment->Element<T>(index);
        }

        public static T[] ToArray<T>(UnsafeMPMCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);


            queue->SnapForObservation(out QueueSegment* head, out int headHead, out QueueSegment* tail, out int tailTail);

            long count = GetCount(head, headHead, tail, tailTail);

            var arr = new T[count];
            int i = 0;

            var enumerator = GetEnumerator<T>(queue);
            while (enumerator.MoveNext())
                arr[i++] = enumerator.Current;

            UDebug.Assert(count == i);

            return arr;
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeMPMCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            queue->SnapForObservation(out QueueSegment* headSeg, out int headSeg_Head, out QueueSegment* tailSeg, out int tailSeg_Tail);

            var enumerator = new Enumerator<T>(headSeg, headSeg_Head, tailSeg, tailSeg_Tail);
            return enumerator;
        }

        public static void Clear(UnsafeMPMCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_head != null);

            queue->_crossSegmentLock.Lock();
            queue->_tail->EnsureFrozenForEnqueues();

            var segmentsize = INITIAL_SEGMENT_LENGTH;
            if (queue->_fixedSize)
                segmentsize = queue->_tail->Capacity;

            // Free all segments
            var segment = queue->_head;
            do
            {
                var next = segment->_nextSegment;
                QueueSegment.Free(segment);
                segment = next;
            }
            while (segment != null);

            queue->_tail = queue->_head = QueueSegment.Allocate(segmentsize, queue->_slotStride, queue->_slotOffset);

            queue->_crossSegmentLock.Unlock();
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            // Fields normally generated by Yield
            int __state;
            int __index;
            int __headTail;
            int __sTail;
            QueueSegment* __segIndex;

            T* _current;
            QueueSegment* _head;
            QueueSegment* _tail;
            int _headHead;
            int _tailTail;


            internal Enumerator(QueueSegment* head, int headHead, QueueSegment* tail, int tailTail)
            {
                UDebug.Assert(head->_preserved);
                UDebug.Assert(head->_frozen);
                UDebug.Assert(tail->_preserved);
                UDebug.Assert(tail->_frozen);

                __state = 0;
                __index = 0;
                __headTail = 0;
                __sTail = 0;
                __segIndex = null;

                _current = null;
                _head = head;
                _tail = tail;
                _headHead = headHead;
                _tailTail = tailTail;


                SetupState();
            }

            private void SetupState()
            {
                // This function determines the first state the enumerator has to enter.
                __headTail = (_head == _tail ? _tailTail : Volatile.Read(ref _head->_headAndTail.Tail)) - _head->FreezeOffset;

                if (_headHead < __headTail)
                {
                    _headHead &= _head->Mask;
                    __headTail &= _head->Mask;

                    if (_headHead < __headTail)
                    {
                        // for (int i = headHead; i < headTail; i++)
                        __state = 0;
                    }
                    else
                    {
                        // for (int i = headHead; i < head._slots.Length; i++)
                        // for (int i = 0; i < headTail; i++)
                        __state = 2;
                    }
                }
            }

            public void Dispose()
            {
                __state = -1;
                _current = null;
            }

            public bool MoveNext()
            {
                // Uses "decompiled" Yield implementation. See: https://csharpindepth.com/articles/IteratorBlockImplementation for more information.
                // For loop has been deconstructed to allow re-entry from another state.
                switch (__state)
                {
                    #region for (int i = headHead; i < headTail; i++)
                    case 0:
                        {
                            __state = -1;
                            __index = _headHead - 1;
                            goto case 1;
                        }
                    case 1:
                        {
                            __state = -1;
                            __index++;

                            if (__index < __headTail)
                            {
                                _current = GetItemWhenAvailable<T>(_head, __index);
                                __state = 1;
                                return true;
                            }

                            goto case 6;
                        }
                    #endregion

                    #region for (int i = headHead; i < head._slots.Length; i++)
                    case 2:
                        {
                            __state = -1;
                            __index = _headHead - 1;
                            goto case 3;
                        }
                    case 3:
                        {
                            __state = -1;
                            __index++;

                            if (__index < _head->Capacity)
                            {
                                _current = GetItemWhenAvailable<T>(_head, __index);
                                __state = 3;
                                return true;
                            }

                            // Fall over into next state once this state ends.
                            goto case 4;
                        }
                    #endregion

                    #region for (int i = 0; i < headTail; i++)
                    case 4:
                        {
                            __state = -1;
                            __index = -1;
                            goto case 5;
                        }
                    case 5:
                        {
                            __state = -1;
                            __index++;

                            if (__index < __headTail)
                            {
                                _current = GetItemWhenAvailable<T>(_head, __index);
                                __state = 5;
                                return true;
                            }

                            // Goto state that checks if head and tail are equal
                            goto case 6;
                        }
                    #endregion

                    case 6: // head != tail
                        {
                            __state = -1;
                            if (_head == _tail)
                                break; // Head and Tail are the same. All items are iterated.

                            // More QueueSegments to iterate.
                            goto case 7;
                        }

                    #region for (ConcurrentQueueSegment<T> s = head._nextSegment!; s != tail; s = s._nextSegment!)
                    case 7: // for initialise
                        {
                            __state = -1;
                            __segIndex = _head->_nextSegment;

                            goto case 8;
                        }
                    case 8: // for condition + body
                        {
                            if (__segIndex != _tail)
                            {
                                UDebug.Assert(__segIndex->_preserved);
                                UDebug.Assert(__segIndex->_frozen);
                                __sTail = __segIndex->_headAndTail.Tail - __segIndex->FreezeOffset;

                                goto case 10; // Goto nested for loop
                            }

                            goto case 12; // Break out of loop and enter final state
                        }
                    case 9: // for increment
                        {
                            __segIndex = __segIndex->_nextSegment;
                            goto case 8; // Next iteration of for
                        }

                    #endregion

                    #region for (int i = 0; i < sTail; i++)
                    case 10:
                        {
                            __state = -1;
                            __index = -1;
                            goto case 11;
                        }
                    case 11:
                        {
                            __state = -1;
                            __index++;

                            if (__index < __sTail)
                            {
                                _current = GetItemWhenAvailable<T>(__segIndex, __index);
                                __state = 11;
                                return true;
                            }
                            // Increment parent for-loop when this one ends
                            goto case 9;
                        }
                    #endregion

                    #region for (int i = 0; i < tailTail; i++)
                    case 12:
                        {
                            __state = -1;
                            __index = -1;
                            _tailTail -= _tail->FreezeOffset;
                            goto case 13;
                        }
                    case 13:
                        {
                            __state = -1;
                            __index++;

                            if (__index < _tailTail)
                            {
                                _current = GetItemWhenAvailable<T>(_tail, __index);
                                __state = 13;
                                return true;
                            }
                            break;
                        }
                        #endregion
                }

                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
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
                    if (_current == null)
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
