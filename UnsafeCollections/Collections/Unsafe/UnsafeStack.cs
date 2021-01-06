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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeStack
    {
        const string STACK_FULL = "Fixed size stack is full";
        const string STACK_EMPTY = "Stack is empty";

        const int DEFAULT_CAPACITY = 8;

        UnsafeBuffer _items;
        IntPtr _typeHandle;
        int _count;

        public static UnsafeStack* Allocate<T>(int capacity, bool fixedSize = false) where T : unmanaged
        {
            UDebug.Assert(capacity > 0);

            var stride = sizeof(T);
            UnsafeStack* stack;

            // fixedSize stack means we are allocating the memory
            // for the stack header and the items in it as one block
            if (fixedSize)
            {
                var alignment = Memory.GetAlignment(stride);

                // align stack header size to the elements alignment
                var sizeOfStack = Memory.RoundToAlignment(sizeof(UnsafeStack), alignment);
                var sizeOfArray = stride * capacity;

                // allocate memory for stack and array with the correct alignment
                var ptr = Memory.MallocAndZero(sizeOfStack + sizeOfArray, alignment);

                // grab stack ptr
                stack = (UnsafeStack*)ptr;

                // initialize fixed buffer from same block of memory as the stack
                UnsafeBuffer.InitFixed(&stack->_items, (byte*)ptr + sizeOfStack, capacity, stride);
            }

            // dynamic sized stack means we're allocating the stack header
            // and its memory separately
            else
            {
                // allocate stack separately
                stack = Memory.MallocAndZero<UnsafeStack>();

                // initialize dynamic buffer with separate memory
                UnsafeBuffer.InitDynamic(&stack->_items, capacity, stride);
            }

            // just safety, make sure count is 0
            stack->_count = 0;
            stack->_typeHandle = typeof(T).TypeHandle.Value;

            return stack;
        }

        public static void Free(UnsafeStack* stack)
        {
            if (stack == null)
                return;

            // if this is a dynamic sized stack, we need to free the buffer by hand
            if (stack->_items.Dynamic == 1)
            {
                UnsafeBuffer.Free(&stack->_items);
            }

            // clear stack memory just in case
            *stack = default;

            // free stack memory (if this is a fixed size stack, it frees the _items memory also)
            Memory.Free(stack);
        }

        public static int GetCapacity(UnsafeStack* stack)
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);

            return stack->_items.Length;
        }

        public static int GetCount(UnsafeStack* stack)
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);

            return stack->_count;
        }

        public static void Clear(UnsafeStack* stack)
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);

            stack->_count = 0;
        }

        public static bool IsFixedSize(UnsafeStack* stack)
        {
            UDebug.Assert(stack != null);

            return stack->_items.Dynamic == 0;
        }


        public static bool Contains<T>(UnsafeStack* stack, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            int count = stack->_count;

            if (count == 0)
                return false;

            return UnsafeBuffer.LastIndexOf(stack->_items, item, count - 1, count) != -1;
        }

        public static void CopyTo<T>(UnsafeStack* stack, void* destination, int destinationIndex) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);
            UDebug.Assert(destination != null);
            UDebug.Assert(destinationIndex > -1);

            int numToCopy = stack->_count;
            if (numToCopy == 0)
                return;

            int srcIndex = 0;
            int stride = stack->_items.Stride;
            int dstIndex = destinationIndex + numToCopy;

            while (srcIndex < numToCopy)
            {
                *(T*)((byte*)destination + (--dstIndex * stride)) = *stack->_items.Element<T>(srcIndex++);
            }
        }

        public static T Peek<T>(UnsafeStack* stack) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            var count = stack->_count - 1;
            if ((uint)count >= (uint)stack->_items.Length)
            {
                throw new InvalidOperationException(STACK_EMPTY);
            }

            return *stack->_items.Element<T>(count);
        }

        public static bool TryPeek<T>(UnsafeStack* stack, out T item) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            var count = stack->_count - 1;
            if ((uint)count >= (uint)stack->_items.Length)
            {
                item = default;
                return false;
            }

            item = *stack->_items.Element<T>(count);
            return true;
        }

        public static T Pop<T>(UnsafeStack* stack) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            var count = stack->_count - 1;
            if ((uint)count >= (uint)stack->_items.Length)
            {
                throw new InvalidOperationException(STACK_EMPTY);
            }

            stack->_count = count;
            return *stack->_items.Element<T>(count);
        }

        public static bool TryPop<T>(UnsafeStack* stack, out T item) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            var count = stack->_count - 1;
            if ((uint)count >= (uint)stack->_items.Length)
            {
                item = default;
                return false;
            }

            stack->_count = count;
            item = *stack->_items.Element<T>(count);
            return true;
        }

        public static void Push<T>(UnsafeStack* stack, T item) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            var items = stack->_items;
            int count = stack->_count;

            if ((uint)count < (uint)items.Length)
            {
                *items.Element<T>(count) = item;
                stack->_count = count + 1;
            }
            else
            {
                if (items.Dynamic == 1)
                {
                    ResizeAndPush(stack, item);
                }
                else
                {
                    throw new InvalidOperationException(STACK_FULL);
                }
            }
        }

        public static bool TryPush<T>(UnsafeStack* stack, T item) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            var items = stack->_items;
            int count = stack->_count;

            if ((uint)count < (uint)items.Length)
            {
                *items.Element<T>(count) = item;
                stack->_count = count + 1;

                return true;
            }
            else
            {
                if (items.Dynamic == 1)
                {
                    ResizeAndPush(stack, item);
                    return true;
                }
                return false;
            }
        }

        private static void ResizeAndPush<T>(UnsafeStack* stack, T item) where T : unmanaged
        {
            Expand(stack);

            *stack->_items.Element<T>(stack->_count) = item;
            stack->_count++;
        }

        private static void Expand(UnsafeStack* stack)
        {
            // new buffer for elements
            UnsafeBuffer newItems = default;

            // initialize to double size of existing one
            int newSize = stack->_items.Length == 0 ? DEFAULT_CAPACITY : stack->_items.Length * 2;
            UnsafeBuffer.InitDynamic(&newItems, newSize, stack->_items.Stride);

            // copy memory over from previous items
            UnsafeBuffer.Copy(stack->_items, 0, newItems, 0, stack->_items.Length);

            // free old buffer
            UnsafeBuffer.Free(&stack->_items);

            // replace buffer with new
            stack->_items = newItems;
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeStack* stack) where T : unmanaged
        {
            UDebug.Assert(stack != null);
            UDebug.Assert(stack->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == stack->_typeHandle);

            return new Enumerator<T>(stack->_items, stack->_count);
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            T* _current;
            int _index;
            readonly int _count;
            UnsafeBuffer _buffer;

            internal Enumerator(UnsafeBuffer buffer, int count)
            {
                _index = count - 1;
                _count = count;
                _buffer = buffer;
                _current = default;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                if (_index < 0)
                    return false;

                if ((uint)_index >= 0)
                {
                    _current = _buffer.Element<T>(_index--);
                    return true;
                }

                _current = default;
                return false;
            }

            public void Reset()
            {
                _index = _count - 1;
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
                    if (_index == 0 || _index == _count + 1)
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