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
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeHashSet
    {
        UnsafeHashCollection _collection;
        IntPtr _typeHandle;

        public static UnsafeHashSet* Allocate<T>(int capacity, bool fixedSize = false)
            where T : unmanaged, IEquatable<T>
        {
            var valStride = sizeof(T);
            var entryStride = sizeof(UnsafeHashCollection.Entry);

            // round capacity up to next prime 
            capacity = UnsafeHashCollection.GetNextPrime(capacity);

            var valAlignment = Memory.GetAlignment(valStride);

            // the alignment for entry/key/val, we can't have less than ENTRY_ALIGNMENT
            // bytes alignment because entries are 16 bytes with 1 x pointer + 2 x 4 byte integers
            var alignment = Math.Max(UnsafeHashCollection.Entry.ALIGNMENT, valAlignment);

            // calculate strides for all elements
            valStride = Memory.RoundToAlignment(valStride, alignment);
            entryStride = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry), alignment);

            // dictionary ptr
            UnsafeHashSet* set;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeHashSet), alignment);
                var sizeOfBucketsBuffer = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry**) * capacity, alignment);
                var sizeofEntriesBuffer = (entryStride + valStride) * capacity;

                // allocate memory
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBucketsBuffer + sizeofEntriesBuffer, alignment);

                // start of memory is the dict itself
                set = (UnsafeHashSet*)ptr;

                // buckets are offset by header size
                set->_collection.Buckets = (UnsafeHashCollection.Entry**)((byte*)ptr + sizeOfHeader);

                // initialize fixed buffer
                UnsafeBuffer.InitFixed(&set->_collection.Entries, (byte*)ptr + (sizeOfHeader + sizeOfBucketsBuffer), capacity, entryStride + valStride);
            }
            else
            {
                // allocate dict, buckets and entries buffer separately
                set = Memory.MallocAndZero<UnsafeHashSet>();
                set->_collection.Buckets = (UnsafeHashCollection.Entry**)Memory.MallocAndZero(sizeof(UnsafeHashCollection.Entry**) * capacity, sizeof(UnsafeHashCollection.Entry**));

                // init dynamic buffer
                UnsafeBuffer.InitDynamic(&set->_collection.Entries, capacity, entryStride + valStride);
            }

            set->_collection.FreeCount = 0;
            set->_collection.UsedCount = 0;
            set->_collection.KeyOffset = entryStride;
            set->_typeHandle = typeof(T).TypeHandle.Value;

            return set;
        }

        public static void Free(UnsafeHashSet* set)
        {
            if (set == null)
                return;

            if (set->_collection.Entries.Dynamic == 1)
            {
                UnsafeHashCollection.Free(&set->_collection);
            }

            *set = default;

            Memory.Free(set);
        }

        public static int GetCapacity(UnsafeHashSet* set)
        {
            UDebug.Assert(set != null);

            return set->_collection.Entries.Length;
        }

        public static int GetCount(UnsafeHashSet* set)
        {
            UDebug.Assert(set != null);

            return set->_collection.UsedCount - set->_collection.FreeCount;
        }

        public static bool IsFixedSize(UnsafeHashSet* set)
        {
            UDebug.Assert(set != null);

            return set->_collection.Entries.Dynamic == 0;
        }

        public static void Clear(UnsafeHashSet* set)
        {
            UDebug.Assert(set != null);

            UnsafeHashCollection.Clear(&set->_collection);
        }

        public static bool Add<T>(UnsafeHashSet* set, T key)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<T>(&set->_collection, key, hash);
            if (entry == null)
            {
                UnsafeHashCollection.Insert<T>(&set->_collection, key, hash);
                return true;
            }

            return false;
        }

        public static bool Remove<T>(UnsafeHashSet* set, T key)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            return UnsafeHashCollection.Remove<T>(&set->_collection, key, key.GetHashCode());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(UnsafeHashSet* set, T key)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            return UnsafeHashCollection.Find(&set->_collection, key, key.GetHashCode()) != null;
        }

        public static void CopyTo<T>(UnsafeHashSet* set, void* destination, int destinationIndex)
            where T : unmanaged
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            var enumerator = GetEnumerator<T>(set);
            var dest = (T*)destination;

            int i = 0;
            while (enumerator.MoveNext())
            {
                dest[destinationIndex + i] = enumerator.Current;
                i++;
            }
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeHashSet* set)
            where T : unmanaged
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);

            return new Enumerator<T>(set);
        }

        /// <summary>
        /// Modifies the current hashset to contain only elements that are present in that hashset and in the specified hashset.
        /// </summary>
        public static void IntersectsWith<T>(UnsafeHashSet* set, UnsafeHashSet* other)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);
            UDebug.Assert(other != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == other->_typeHandle);

            // When this set has no elements, there is nothing to intersect.
            // When this set equals other, it is already intersecting.
            if (GetCount(set) == 0 || set == other)
                return;

            // When the other set has no elements, clear this one completely
            if (GetCount(other) == 0)
            {
                Clear(set);
                return;
            }

            for (int i = set->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&set->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    var key = *(T*)((byte*)entry + set->_collection.KeyOffset);
                    var keyHash = key.GetHashCode();

                    // if we don't find this in other collection, remove it (And)
                    if (UnsafeHashCollection.Find<T>(&other->_collection, key, keyHash) == null)
                    {
                        UnsafeHashCollection.Remove<T>(&set->_collection, key, keyHash);
                    }
                }
            }
        }

        /// <summary>
        /// Modifies the current hashset to contain all elements that are present in itself, the specified hashset, or both.
        /// </summary>
        public static void UnionWith<T>(UnsafeHashSet* set, UnsafeHashSet* other)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);
            UDebug.Assert(other != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == other->_typeHandle);

            for (int i = other->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&other->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    // always add to this collection
                    Add<T>(set, *(T*)((byte*)entry + other->_collection.KeyOffset));
                }
            }
        }

        /// <summary>
        /// Removes all elements in the specified hashset from the current hashset.
        /// </summary>
        public static void ExceptWith<T>(UnsafeHashSet* set, UnsafeHashSet* other)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);
            UDebug.Assert(other != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == other->_typeHandle);

            // When this set has no elements, return
            if (GetCount(set) == 0)
                return;

            // A set except itself is an empty set.
            if (other == set)
            {
                Clear(set);
                return;
            }

            for (int i = other->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&other->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    var key = *(T*)((byte*)entry + other->_collection.KeyOffset);
                    var keyHash = key.GetHashCode();

                    UnsafeHashCollection.Remove(&set->_collection, key, keyHash);
                }
            }
        }

        /// <summary>
        /// Modifies the current hashset to contain only elements that are present either in this hashset or in the specified hashset, but not both.
        /// </summary>
        public static void SymmetricExcept<T>(UnsafeHashSet* set, UnsafeHashSet* other)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(set != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == set->_typeHandle);
            UDebug.Assert(other != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == other->_typeHandle);

            // When this set has no elements, return
            if (GetCount(set) == 0)
                return;

            // A set except itself is an empty set.
            if (other == set)
            {
                Clear(set);
                return;
            }

            for (int i = other->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&other->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    var key = *(T*)((byte*)entry + other->_collection.KeyOffset);
                    var keyHash = key.GetHashCode();

                    if (!UnsafeHashCollection.Remove(&set->_collection, key, keyHash))
                    {
                        UnsafeHashCollection.Insert(&set->_collection, key, keyHash);
                    }
                }
            }
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T>
            where T : unmanaged
        {
            UnsafeHashCollection.Enumerator _iterator;
            readonly int _keyOffset;

            public Enumerator(UnsafeHashSet* set)
            {
                _keyOffset = set->_collection.KeyOffset;
                _iterator = new UnsafeHashCollection.Enumerator(&set->_collection);
            }

            public bool MoveNext()
            {
                return _iterator.MoveNext();
            }

            public void Reset()
            {
                _iterator.Reset();
            }

            object IEnumerator.Current
            {
                get { return Current; }
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