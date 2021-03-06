﻿/*
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
    public unsafe struct UnsafeDictionary
    {
        UnsafeHashCollection _collection;
        IntPtr _typeHandleKey;    // Readonly
        IntPtr _typeHandleValue;  // Readonly
        int _valueOffset;         // Readonly

        public static UnsafeDictionary* Allocate<K, V>(int capacity, bool fixedSize = false)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            var keyStride = sizeof(K);
            var valStride = sizeof(V);
            var entryStride = sizeof(UnsafeHashCollection.Entry);

            // round capacity up to next prime 
            capacity = UnsafeHashCollection.GetNextPrime(capacity);

            var keyAlignment = Memory.GetAlignment(keyStride);
            var valAlignment = Memory.GetAlignment(valStride);

            // the alignment for entry/key/val, we can't have less than ENTRY_ALIGNMENT
            // bytes alignment because entries are 8 bytes with 2 x 32 bit integers
            var alignment = Math.Max(UnsafeHashCollection.Entry.ALIGNMENT, Math.Max(keyAlignment, valAlignment));

            // calculate strides for all elements
            keyStride = Memory.RoundToAlignment(keyStride, alignment);
            valStride = Memory.RoundToAlignment(valStride, alignment);
            entryStride = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry), alignment);

            // map ptr
            UnsafeDictionary* map;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeDictionary), alignment);
                var sizeOfBucketsBuffer = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry**) * capacity, alignment);
                var sizeofEntriesBuffer = (entryStride + keyStride + valStride) * capacity;

                // allocate memory
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBucketsBuffer + sizeofEntriesBuffer, alignment);

                // start of memory is the dict itself
                map = (UnsafeDictionary*)ptr;

                // buckets are offset by header size
                map->_collection.Buckets = (UnsafeHashCollection.Entry**)((byte*)ptr + sizeOfHeader);

                // initialize fixed buffer
                UnsafeBuffer.InitFixed(&map->_collection.Entries, (byte*)ptr + (sizeOfHeader + sizeOfBucketsBuffer), capacity, entryStride + keyStride + valStride);
            }
            else
            {
                // allocate dict, buckets and entries buffer separately
                map = Memory.MallocAndZero<UnsafeDictionary>();
                map->_collection.Buckets = (UnsafeHashCollection.Entry**)Memory.MallocAndZero(sizeof(UnsafeHashCollection.Entry**) * capacity, sizeof(UnsafeHashCollection.Entry**));

                // init dynamic buffer
                UnsafeBuffer.InitDynamic(&map->_collection.Entries, capacity, entryStride + keyStride + valStride);
            }

            // header init
            map->_collection.FreeCount = 0;
            map->_collection.UsedCount = 0;
            map->_collection.KeyOffset = entryStride;
            map->_typeHandleKey = typeof(K).TypeHandle.Value;
            map->_typeHandleValue = typeof(V).TypeHandle.Value;

            map->_valueOffset = entryStride + keyStride;

            return map;
        }

        public static void Free(UnsafeDictionary* set)
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

        public static int GetCapacity(UnsafeDictionary* map)
        {
            UDebug.Assert(map != null);

            return map->_collection.Entries.Length;
        }

        public static int GetCount(UnsafeDictionary* map)
        {
            UDebug.Assert(map != null);

            return map->_collection.UsedCount - map->_collection.FreeCount;
        }

        public static bool IsFixedSize(UnsafeDictionary* map)
        {
            UDebug.Assert(map != null);

            return map->_collection.Entries.Dynamic == 0;
        }

        public static void Clear(UnsafeDictionary* map)
        {
            UDebug.Assert(map != null);

            UnsafeHashCollection.Clear(&map->_collection);
        }

        public static bool ContainsKey<K>(UnsafeDictionary* map, K key) where K : unmanaged, IEquatable<K>
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);

            return UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode()) != null;
        }

        public static bool ContainsValue<V>(UnsafeDictionary* map, V value)
            where V : unmanaged, IEquatable<V>
        {
            var iterator = new ValueEnumerator<V>(map);
            while (iterator.MoveNext())
            {
                if (value.Equals(iterator.Current))
                    return true;
            }

            return false;
        }

        public static void AddOrGet<K, V>(UnsafeDictionary* map, K key, ref V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
            if (entry == null)
            {
                // insert new entry for key
                entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);

                // assign value to entry
                *GetValue<V>(map->_valueOffset, entry) = value;
            }
            else
            {
                value = *GetValue<V>(map->_valueOffset, entry);
            }
        }

        public static void Add<K, V>(UnsafeDictionary* map, K key, V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            TryInsert(map, key, value, MapInsertionBehaviour.ThrowIfExists);
        }

        public static bool TryAdd<K, V>(UnsafeDictionary* map, K key, V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            return TryInsert(map, key, value, MapInsertionBehaviour.None);
        }

        private static bool TryInsert<K, V>(UnsafeDictionary* map, K key, V value, MapInsertionBehaviour behaviour)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);

            // Entry is already present
            if (entry != null)
            {
                if (behaviour == MapInsertionBehaviour.Overwrite)
                {
                    *GetValue<V>(map->_valueOffset, entry) = value;
                    return true;
                }

                if (behaviour == MapInsertionBehaviour.ThrowIfExists)
                {
                    throw new ArgumentException(string.Format(ThrowHelper.Arg_AddingDuplicateWithKey, key));
                }

                return false;
            }
            // Create new entry
            else
            {
                entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);
                *GetValue<V>(map->_valueOffset, entry) = value;
                return true;
            }
        }

        public static void Set<K, V>(UnsafeDictionary* map, K key, V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            TryInsert(map, key, value, MapInsertionBehaviour.Overwrite);
        }

        public static V Get<K, V>(UnsafeDictionary* map, K key)
          where K : unmanaged, IEquatable<K>
          where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            var entry = UnsafeHashCollection.Find(&map->_collection, key, key.GetHashCode());
            if (entry == null)
            {
                throw new ArgumentException(string.Format(ThrowHelper.Arg_KeyNotFoundWithKey, key));
            }

            return *GetValue<V>(map->_valueOffset, entry);
        }

        public static bool TryGetValue<K, V>(UnsafeDictionary* map, K key, out V val)
          where K : unmanaged, IEquatable<K>
          where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
            if (entry != null)
            {
                val = *GetValue<V>(map->_valueOffset, entry);
                return true;
            }

            val = default;
            return false;
        }

        public static bool Remove<K>(UnsafeDictionary* map, K key) where K : unmanaged, IEquatable<K>
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);

            return UnsafeHashCollection.Remove<K>(&map->_collection, key, key.GetHashCode());
        }

        public static void CopyTo<K, V>(UnsafeDictionary* map, KeyValuePair<K, V>[] destination, int destinationIndex)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (GetCount(map) + (uint)destinationIndex > destination.Length)
                throw new ArgumentOutOfRangeException(ThrowHelper.Arg_ArrayPlusOffTooSmall);

            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);
            UDebug.Assert(destination != null);
            UDebug.Assert(destination.Length >= GetCount(map) + destinationIndex);

            var enumerator = GetEnumerator<K, V>(map);

            int i = 0;
            while (enumerator.MoveNext())
            {
                destination[destinationIndex + i] = enumerator.Current;
                i++;
            }
        }

        public static Enumerator<K, V> GetEnumerator<K, V>(UnsafeDictionary* map)
            where K : unmanaged
            where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            return new Enumerator<K, V>(map);
        }

        public static KeyEnumerator<K> GetKeyEnumerator<K>(UnsafeDictionary* map)
            where K : unmanaged, IEquatable<K>
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);

            return new KeyEnumerator<K>(map);
        }

        public static ValueEnumerator<V> GetValueEnumerator<V>(UnsafeDictionary* map)
            where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            return new ValueEnumerator<V>(map);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static V* GetValue<V>(int offset, UnsafeHashCollection.Entry* pair)
            where V : unmanaged
        {
            return (V*)((byte*)pair + offset);
        }

        public unsafe struct Enumerator<K, V> : IUnsafeEnumerator<KeyValuePair<K, V>>
            where K : unmanaged
            where V : unmanaged
        {

            UnsafeHashCollection.Enumerator _iterator;
            readonly int _keyOffset;
            readonly int _valueOffset;

            public Enumerator(UnsafeDictionary* map)
            {
                _valueOffset = map->_valueOffset;
                _keyOffset = map->_collection.KeyOffset;
                _iterator = new UnsafeHashCollection.Enumerator(&map->_collection);
            }

            public K CurrentKey
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_iterator.Current != null);
                    return *(K*)((byte*)_iterator.Current + _keyOffset);
                }
            }

            public V CurrentValue
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_iterator.Current != null);
                    return *(V*)((byte*)_iterator.Current + _valueOffset);
                }
            }

            public KeyValuePair<K, V> Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get { return new KeyValuePair<K, V>(CurrentKey, CurrentValue); }
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

            public Enumerator<K, V> GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
            IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
            {
                return this;
            }
        }

        /// <summary>
        /// Enumerator specifically used for enumerating the Keys of a OrderedSet
        /// </summary>
        public unsafe struct KeyEnumerator<K> : IUnsafeEnumerator<K>
            where K : unmanaged, IEquatable<K>
        {
            UnsafeHashCollection.Enumerator _iterator;
            readonly int _keyOffset;

            public KeyEnumerator(UnsafeDictionary* dictionary)
            {
                _keyOffset = dictionary->_collection.KeyOffset;
                _iterator = new UnsafeHashCollection.Enumerator(&dictionary->_collection);
            }

            public K Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_iterator.Current != null);
                    return *(K*)((byte*)_iterator.Current + _keyOffset);
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

            public KeyEnumerator<K> GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
            IEnumerator<K> IEnumerable<K>.GetEnumerator()
            {
                return this;
            }
        }

        /// <summary>
        /// Enumerator specifically used for enumerating the Values of a OrderedSet
        /// </summary>
        public unsafe struct ValueEnumerator<V> : IUnsafeEnumerator<V>
            where V : unmanaged
        {
            UnsafeHashCollection.Enumerator _iterator;
            readonly int _valueOffset;

            public ValueEnumerator(UnsafeDictionary* dictionary)
            {
                _valueOffset = dictionary->_valueOffset;
                _iterator = new UnsafeHashCollection.Enumerator(&dictionary->_collection);
            }

            public V Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_iterator.Current != null);
                    return *(V*)((byte*)_iterator.Current + _valueOffset);
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

            public ValueEnumerator<V> GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return this;
            }
        }
    }
}