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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeSortedSet
    {
        UnsafeOrderedCollection _collection;
        IntPtr _typeHandle;

        public static UnsafeSortedSet* Allocate<T>(int capacity, bool fixedSize = false)
            where T : unmanaged, IComparable<T>
        {
            var valStride = sizeof(T);
            var entryStride = sizeof(UnsafeOrderedCollection.Entry);
            var valAlignment = Memory.GetAlignment(valStride);

            // the alignment for entry/key/val, we can't have less than ENTRY_ALIGNMENT
            // bytes alignment because entries are 12 bytes with 3 x 32 bit integers
            var alignment = Math.Max(UnsafeOrderedCollection.Entry.ALIGNMENT, valAlignment);

            // calculate strides for all elements
            valStride = Memory.RoundToAlignment(valStride, alignment);
            entryStride = Memory.RoundToAlignment(entryStride, alignment);

            // dictionary ptr
            UnsafeSortedSet* set;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeSortedSet), alignment);
                var sizeofEntriesBuffer = (entryStride + valStride) * capacity;

                // allocate memory
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeofEntriesBuffer, alignment);

                // start of memory is the set itself
                set = (UnsafeSortedSet*)ptr;

                // initialize fixed buffer
                UnsafeBuffer.InitFixed(&set->_collection.Entries, (byte*)ptr + sizeOfHeader, capacity, entryStride + valStride);
            }
            else
            {
                // allocate set separately
                set = Memory.MallocAndZero<UnsafeSortedSet>();

                // init dynamic buffer
                UnsafeBuffer.InitDynamic(&set->_collection.Entries, capacity, entryStride + valStride);
            }

            set->_collection.FreeCount = 0;
            set->_collection.UsedCount = 0;
            set->_collection.KeyOffset = entryStride;
            set->_typeHandle = typeof(T).TypeHandle.Value;

            return set;
        }

        public static void Free(UnsafeSortedSet* set)
        {
            if (set == null)
                return;

            if (set->_collection.Entries.Dynamic == 1)
            {
                UnsafeBuffer.Free(&set->_collection.Entries);
            }

            // clear memory
            *set = default;

            // free it
            Memory.Free(set);
        }

        public static int GetCount(UnsafeSortedSet* set)
        {
            UDebug.Assert(set != null);

            return UnsafeOrderedCollection.GetCount(&set->_collection);
        }

        public static int GetCapacity(UnsafeSortedSet* set)
        {
            UDebug.Assert(set != null);

            return set->_collection.Entries.Length;
        }

        public static bool IsFixedSize(UnsafeSortedSet* set)
        {
            UDebug.Assert(set != null);

            return set->_collection.Entries.Dynamic == 0;
        }

        public static void Add<T>(UnsafeSortedSet* set, T item)
            where T : unmanaged, IComparable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            UnsafeOrderedCollection.Insert<T>(&set->_collection, item);
        }

        public static bool Remove<T>(UnsafeSortedSet* set, T item)
            where T : unmanaged, IComparable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            return UnsafeOrderedCollection.Remove<T>(&set->_collection, item);
        }

        public static bool Contains<T>(UnsafeSortedSet* set, T item)
            where T : unmanaged, IComparable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            return UnsafeOrderedCollection.Find<T>(&set->_collection, item) != null;
        }

        public static void Clear(UnsafeSortedSet* set)
        {
            UDebug.Assert(set != null);

            UnsafeOrderedCollection.Clear(&set->_collection);
        }

        public static void CopyTo<T>(UnsafeSortedSet* set, void* destination, int destinationIndex)
            where T : unmanaged
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);
            UDebug.Assert(destination != null);

            var enumerator = GetEnumerator<T>(set);
            var dest = (T*)destination;

            int i = 0;
            while (enumerator.MoveNext())
            {
                dest[destinationIndex + i] = enumerator.Current;
                i++;
            }
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeSortedSet* set) where T : unmanaged
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            return new Enumerator<T>(set);
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            readonly int _keyOffset;
            UnsafeOrderedCollection.Enumerator _iterator;

            public Enumerator(UnsafeSortedSet* set)
            {
                _keyOffset = set->_collection.KeyOffset;
                _iterator = new UnsafeOrderedCollection.Enumerator(&set->_collection);
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_iterator.Current != null);
                    return *(T*)((byte*)_iterator.Current + _keyOffset);
                }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _iterator.MoveNext();
            }

            public void Reset()
            {
                _iterator.Reset();
            }

            public void Dispose()
            { }

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