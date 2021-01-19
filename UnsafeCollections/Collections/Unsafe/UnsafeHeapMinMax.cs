/*
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
    public unsafe struct UnsafeHeapMax
    {
        UnsafeBuffer _items;
        int _count;
        int _keyStride;           // Readonly
        IntPtr _typeHandleKey;    // Readonly
        IntPtr _typeHandleValue;  // Readonly


        public static UnsafeHeapMax* Allocate<K, V>(int capacity, bool fixedSize = false)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), string.Format(ThrowHelper.ArgumentOutOfRange_MustBePositive, nameof(capacity)));

            var keyStride = sizeof(K);
            var valStride = sizeof(V);

            capacity += 1;

            // get alignment for key/val
            var keyAlignment = Memory.GetAlignment(keyStride);
            var valAlignment = Memory.GetAlignment(valStride);

            // pick the max one as our alignment
            var alignment = Math.Max(keyAlignment, valAlignment);

            // align sizes to their respective alignments
            keyStride = Memory.RoundToAlignment(keyStride, alignment);
            valStride = Memory.RoundToAlignment(valStride, alignment);

            UnsafeHeapMax* heap;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeHeapMax), alignment);
                var sizeOfBuffer = (keyStride + valStride) * capacity;

                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBuffer, alignment);

                // heap pointer
                heap = (UnsafeHeapMax*)ptr;

                // initialize our fixed buffer
                UnsafeBuffer.InitFixed(&heap->_items, (byte*)ptr + sizeOfHeader, capacity, keyStride + valStride);
            }
            else
            {
                heap = Memory.MallocAndZero<UnsafeHeapMax>();

                // dynamic buffer (separate memory)
                UnsafeBuffer.InitDynamic(&heap->_items, capacity, keyStride + valStride);
            }

            heap->_count = 1;
            heap->_keyStride = keyStride;
            heap->_typeHandleKey = typeof(K).TypeHandle.Value;
            heap->_typeHandleValue = typeof(V).TypeHandle.Value;
            return heap;
        }

        public static void Free(UnsafeHeapMax* heap)
        {
            if (heap == null)
                return;

            // free dynamic items separately
            if (heap->_items.Dynamic == 1)
            {
                UnsafeBuffer.Free(&heap->_items);
            }

            // clear memory
            *heap = default;

            // free heap
            Memory.Free(heap);
        }

        public static int GetCapacity(UnsafeHeapMax* heap)
        {
            UDebug.Assert(heap != null);

            return heap->_items.Length - 1;
        }

        public static int GetCount(UnsafeHeapMax* heap)
        {
            UDebug.Assert(heap != null);

            return heap->_count - 1;
        }

        public static void Clear(UnsafeHeapMax* heap)
        {
            UDebug.Assert(heap != null);

            heap->_count = 1;
        }

        public static void Push<K, V>(UnsafeHeapMax* heap, K key, V val)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            UDebug.Assert(heap != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == heap->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == heap->_typeHandleValue);

            if (heap->_count == heap->_items.Length)
            {
                if (heap->_items.Dynamic == 1)
                {
                    ExpandHeap(heap);
                }
                else
                {
                    throw new InvalidOperationException(ThrowHelper.InvalidOperation_CollectionFull);
                }
            }

            // index we're bubbling up from
            var bubbleIndex = heap->_count;

            // assign new key/val to it
            SetKeyVal(heap, bubbleIndex, key, val);

            while (bubbleIndex != 1)
            {
                var parentIndex = bubbleIndex / 2;
                var parentIndexKey = *heap->_items.Element<K>(parentIndex);
                if (parentIndexKey.CompareTo(key) < 0)
                {
                    GetKeyVal(heap, parentIndex, out K parentKey, out V parentVal);
                    SetKeyVal(heap, bubbleIndex, parentKey, parentVal);
                    SetKeyVal(heap, parentIndex, key, val);

                    bubbleIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            heap->_count = heap->_count + 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pop<K, V>(UnsafeHeapMax* heap, out K key, out V val)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            UDebug.Assert(heap != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == heap->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == heap->_typeHandleValue);

            if (heap->_count <= 1)
            {
                throw new InvalidOperationException(ThrowHelper.InvalidOperation_EmptyHeap);
            }

            heap->_count = heap->_count - 1;

            GetKeyVal(heap, 1, out key, out val);
            //the last node will be placed on top and then swapped downwards as far as necessarry
            GetKeyVal(heap, heap->_count, out K evacuateKey, out V evacuateVal);
            SetKeyVal(heap, 1, evacuateKey, evacuateVal);

            var swapItem = 1;
            var parent = 1;

            do
            {
                parent = swapItem;

                if ((2 * parent + 1) <= heap->_count)
                {
                    // both children exist
                    if (Key<K>(heap, parent).CompareTo(Key<K>(heap, 2 * parent)) <= 0)
                    {
                        swapItem = 2 * parent;
                    }

                    if (Key<K>(heap, swapItem).CompareTo(Key<K>(heap, 2 * parent + 1)) <= 0)
                    {
                        swapItem = 2 * parent + 1;
                    }
                }
                else if ((2 * parent) <= heap->_count)
                {
                    // only one child exists
                    if (Key<K>(heap, parent).CompareTo(Key<K>(heap, 2 * parent)) <= 0)
                    {
                        swapItem = 2 * parent;
                    }
                }

                // one if the parent's children are smaller or equal, swap them
                if (parent != swapItem)
                {
                    // pull parent/swapItem values
                    GetKeyVal<K, V>(heap, parent, out K tmpParentKey, out V tmpParentVal);
                    GetKeyVal<K, V>(heap, swapItem, out K tmpSwapItemKey, out V tmpSwapItemVal);

                    // switch them
                    SetKeyVal(heap, swapItem, tmpParentKey, tmpParentVal);
                    SetKeyVal(heap, parent, tmpSwapItemKey, tmpSwapItemVal);
                }
            } while (parent != swapItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static K Key<K>(UnsafeHeapMax* heap, int index) where K : unmanaged
        {
            return *heap->_items.Element<K>(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetKeyVal<K, V>(UnsafeHeapMax* heap, int index, out K key, out V val)
            where K : unmanaged
            where V : unmanaged
        {
            var ptr = heap->_items.Element(index);

            // read key
            key = *(K*)(ptr);

            // read val, offset by keyStride
            val = *(V*)((byte*)ptr + heap->_keyStride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetKeyVal<K, V>(UnsafeHeapMax* heap, int index, K key, V val)
            where K : unmanaged
            where V : unmanaged
        {
            var ptr = heap->_items.Element(index);

            // write key
            *(K*)(ptr) = key;

            // write val, offset by keyStride
            *(V*)((byte*)ptr + heap->_keyStride) = val;
        }

        static void ExpandHeap(UnsafeHeapMax* heap)
        {
            UDebug.Assert(heap->_items.Dynamic == 1);

            // new buffer for elements
            UnsafeBuffer newItems = default;

            // initialize to double size of existing one
            UnsafeBuffer.InitDynamic(&newItems, heap->_items.Length * 2, heap->_items.Stride);

            // copy memory over from previous items
            UnsafeBuffer.Copy(heap->_items, 0, newItems, 0, heap->_items.Length);

            // free old buffer
            UnsafeBuffer.Free(&heap->_items);

            // replace buffer with new
            heap->_items = newItems;
        }

        public static UnsafeList.Enumerator<T> GetEnumerator<T>(UnsafeHeapMax* heap) where T : unmanaged
        {
            UDebug.Assert(heap != null);

            return new UnsafeList.Enumerator<T>(heap->_items, 1, heap->_count - 1);
        }
    }

    public unsafe struct UnsafeHeapMin
    {
        UnsafeBuffer _items;
        int _count;
        int _keyStride;           // Readonly
        IntPtr _typeHandleKey;    // Readonly
        IntPtr _typeHandleValue;  // Readonly

        public static UnsafeHeapMin* Allocate<K, V>(int capacity, bool fixedSize = false)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity), string.Format(ThrowHelper.ArgumentOutOfRange_MustBePositive, nameof(capacity)));

            var keyStride = sizeof(K);
            var valStride = sizeof(V);

            capacity += 1;

            // get alignment for key/val
            var keyAlignment = Memory.GetAlignment(keyStride);
            var valAlignment = Memory.GetAlignment(valStride);

            // pick the max one as our alignment
            var alignment = Math.Max(keyAlignment, valAlignment);

            // align sizes to their respective alignments
            keyStride = Memory.RoundToAlignment(keyStride, alignment);
            valStride = Memory.RoundToAlignment(valStride, alignment);

            UnsafeHeapMin* heap;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeHeapMin), alignment);
                var sizeOfBuffer = (keyStride + valStride) * capacity;

                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBuffer, alignment);

                // heap pointer
                heap = (UnsafeHeapMin*)ptr;

                // initialize our fixed buffer
                UnsafeBuffer.InitFixed(&heap->_items, (byte*)ptr + sizeOfHeader, capacity, keyStride + valStride);
            }
            else
            {
                heap = Memory.MallocAndZero<UnsafeHeapMin>();

                // dynamic buffer (separate memory)
                UnsafeBuffer.InitDynamic(&heap->_items, capacity, keyStride + valStride);
            }

            heap->_count = 1;
            heap->_keyStride = keyStride;
            heap->_typeHandleKey = typeof(K).TypeHandle.Value;
            heap->_typeHandleValue = typeof(V).TypeHandle.Value;
            return heap;
        }

        public static void Free(UnsafeHeapMin* heap)
        {
            if (heap == null)
            {
                return;
            }

            // free dynamic items separately
            if (heap->_items.Dynamic == 1)
            {
                UnsafeBuffer.Free(&heap->_items);
            }

            // clear memory
            *heap = default;

            // free heap
            Memory.Free(heap);
        }

        public static int Capacity(UnsafeHeapMin* heap)
        {
            UDebug.Assert(heap != null);

            return heap->_items.Length - 1;
        }

        public static int Count(UnsafeHeapMin* heap)
        {
            UDebug.Assert(heap != null);

            return heap->_count - 1;
        }

        public static void Clear(UnsafeHeapMin* heap)
        {
            UDebug.Assert(heap != null);

            heap->_count = 1;
        }

        public static void Push<K, V>(UnsafeHeapMin* heap, K key, V val)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            UDebug.Assert(heap != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == heap->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == heap->_typeHandleValue);

            if (heap->_count == heap->_items.Length)
            {
                if (heap->_items.Dynamic == 1)
                {
                    ExpandHeap(heap);
                }
                else
                {
                    throw new InvalidOperationException(ThrowHelper.InvalidOperation_CollectionFull);
                }
            }

            // index we're bubbling up from
            var bubbleIndex = heap->_count;

            // assign new key/val to it
            SetKeyVal(heap, bubbleIndex, key, val);

            while (bubbleIndex != 1)
            {
                var parentIndex = bubbleIndex / 2;
                var parentIndexKey = *heap->_items.Element<K>(parentIndex);
                if (parentIndexKey.CompareTo(key) > 0)
                {
                    GetKeyVal(heap, parentIndex, out K parentKey, out V parentVal);
                    SetKeyVal(heap, bubbleIndex, parentKey, parentVal);
                    SetKeyVal(heap, parentIndex, key, val);

                    bubbleIndex = parentIndex;
                }
                else
                {
                    break;
                }
            }

            heap->_count = heap->_count + 1;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pop<K, V>(UnsafeHeapMin* heap, out K key, out V val)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            UDebug.Assert(heap != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == heap->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == heap->_typeHandleValue);

            if (heap->_count <= 1)
            {
                throw new InvalidOperationException(ThrowHelper.InvalidOperation_EmptyHeap);
            }

            heap->_count = heap->_count - 1;

            GetKeyVal(heap, 1, out key, out val);
            //the last node will be placed on top and then swapped downwards as far as necessarry
            GetKeyVal(heap, heap->_count, out K evacuateKey, out V evacuateVal);
            SetKeyVal(heap, 1, evacuateKey, evacuateVal);

            var swapItem = 1;
            var parent = 1;

            do
            {
                parent = swapItem;

                if ((2 * parent + 1) <= heap->_count)
                {
                    // both children exist
                    if (Key<K>(heap, parent).CompareTo(Key<K>(heap, 2 * parent)) >= 0)
                    {
                        swapItem = 2 * parent;
                    }

                    if (Key<K>(heap, swapItem).CompareTo(Key<K>(heap, 2 * parent + 1)) >= 0)
                    {
                        swapItem = 2 * parent + 1;
                    }
                }
                else if ((2 * parent) <= heap->_count)
                {
                    // only one child exists
                    if (Key<K>(heap, parent).CompareTo(Key<K>(heap, 2 * parent)) >= 0)
                    {
                        swapItem = 2 * parent;
                    }
                }

                // one if the parent's children are smaller or equal, swap them
                if (parent != swapItem)
                {
                    // pull parent/swapItem values
                    GetKeyVal<K, V>(heap, parent, out K tmpParentKey, out V tmpParentVal);
                    GetKeyVal<K, V>(heap, swapItem, out K tmpSwapItemKey, out V tmpSwapItemVal);

                    // switch them
                    SetKeyVal(heap, swapItem, tmpParentKey, tmpParentVal);
                    SetKeyVal(heap, parent, tmpSwapItemKey, tmpSwapItemVal);
                }
            } while (parent != swapItem);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static K Key<K>(UnsafeHeapMin* heap, int index) where K : unmanaged
        {
            return *heap->_items.Element<K>(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void GetKeyVal<K, V>(UnsafeHeapMin* heap, int index, out K key, out V val)
            where K : unmanaged
            where V : unmanaged
        {
            var ptr = heap->_items.Element(index);

            // read key
            key = *(K*)(ptr);

            // read val, offset by keyStride
            val = *(V*)((byte*)ptr + heap->_keyStride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void SetKeyVal<K, V>(UnsafeHeapMin* heap, int index, K key, V val)
            where K : unmanaged
            where V : unmanaged
        {
            var ptr = heap->_items.Element(index);

            // write key
            *(K*)ptr = key;

            // write val, offset by keyStride
            *(V*)((byte*)ptr + heap->_keyStride) = val;
        }

        static void ExpandHeap(UnsafeHeapMin* heap)
        {
            UDebug.Assert(heap->_items.Dynamic == 1);

            // new buffer for elements
            UnsafeBuffer newItems = default;

            // initialize to double size of existing one
            UnsafeBuffer.InitDynamic(&newItems, heap->_items.Length * 2, heap->_items.Stride);

            // copy memory over from previous items
            UnsafeBuffer.Copy(heap->_items, 0, newItems, 0, heap->_items.Length);

            // free old buffer
            UnsafeBuffer.Free(&heap->_items);

            // replace buffer with new
            heap->_items = newItems;
        }

        public static UnsafeList.Enumerator<T> GetEnumerator<T>(UnsafeHeapMin* heap) where T : unmanaged
        {
            UDebug.Assert(heap != null);

            return new UnsafeList.Enumerator<T>(heap->_items, 1, heap->_count - 1);
        }
    }
}

