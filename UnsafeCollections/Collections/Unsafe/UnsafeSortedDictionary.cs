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
using System.Runtime.CompilerServices;
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeSortedDictionary
    {
        UnsafeOrderedCollection _collection;
        IntPtr _typeHandleKey;    // Readonly
        IntPtr _typeHandleValue;  // Readonly
        int _valueOffset;         // Readonly

        public static UnsafeSortedDictionary* Allocate<K, V>(int capacity, bool fixedSize = false)
          where K : unmanaged, IComparable<K>
          where V : unmanaged
        {
            var keyStride = sizeof(K);
            var valStride = sizeof(V);
            var entryStride = sizeof(UnsafeHashCollection.Entry);

            var keyAlignment = Memory.GetAlignment(keyStride);
            var valAlignment = Memory.GetAlignment(valStride);

            var alignment = Math.Max(UnsafeHashCollection.Entry.ALIGNMENT, Math.Max(keyAlignment, valAlignment));

            keyStride = Memory.RoundToAlignment(keyStride, alignment);
            valStride = Memory.RoundToAlignment(valStride, alignment);
            entryStride = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry), alignment);

            var totalStride = keyStride + valStride + entryStride;

            UnsafeSortedDictionary* map;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeDictionary), alignment);
                var sizeOfBucketsBuffer = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry**) * capacity, alignment);
                var sizeofEntriesBuffer = totalStride * capacity;

                // allocate memory
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBucketsBuffer + sizeofEntriesBuffer, alignment);

                map = (UnsafeSortedDictionary*)ptr;
                UnsafeBuffer.InitFixed(&map->_collection.Entries, (byte*)ptr + (sizeOfHeader + sizeOfBucketsBuffer), capacity, totalStride);
            }
            else
            {
                map = Memory.MallocAndZero<UnsafeSortedDictionary>();
                UnsafeBuffer.InitDynamic(&map->_collection.Entries, capacity, totalStride);
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

        public static void Free(UnsafeSortedDictionary* map)
        {
            if (map == null)
                return;

            if (map->_collection.Entries.Dynamic == 1)
            {
                UnsafeBuffer.Free(&map->_collection.Entries);
            }

            // clear memory
            *map = default;

            // free it
            Memory.Free(map);
        }

        public static int GetCount(UnsafeSortedDictionary* map)
        {
            UDebug.Assert(map != null);

            return UnsafeOrderedCollection.GetCount(&map->_collection);
        }

        public static int GetCapacity(UnsafeSortedDictionary* map)
        {
            UDebug.Assert(map != null);

            return map->_collection.Entries.Length;
        }

        public static bool IsFixedSize(UnsafeSortedDictionary* map)
        {
            UDebug.Assert(map != null);

            return map->_collection.Entries.Dynamic == 0;
        }

        public static void Add<K, V>(UnsafeSortedDictionary* map, K key, V value)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            var entry = UnsafeOrderedCollection.Find<K>(&map->_collection, key);
            if (entry != null)
            {
                throw new ArgumentException(string.Format(ThrowHelper.Arg_AddingDuplicateWithKey, key));
            }
            else
            {
                entry = UnsafeOrderedCollection.Insert<K>(&map->_collection, key);
                *GetValue<V>(map->_valueOffset, entry) = value;
            }
        }

        public static bool Remove<K>(UnsafeSortedDictionary* map, K Key)
            where K : unmanaged, IComparable<K>
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);

            return UnsafeOrderedCollection.Remove<K>(&map->_collection, Key);
        }

        public static bool Contains<K>(UnsafeSortedDictionary* map, K key)
            where K : unmanaged, IComparable<K>
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);

            return UnsafeOrderedCollection.Find<K>(&map->_collection, key) != null;
        }

        public static void Set<K, V>(UnsafeSortedDictionary* map, K key, V value)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            var entry = UnsafeOrderedCollection.Find<K>(&map->_collection, key);

            if (entry == null)
                entry = UnsafeOrderedCollection.Insert<K>(&map->_collection, key);

            *GetValue<V>(map->_valueOffset, entry) = value;
        }

        public static V Get<K, V>(UnsafeSortedDictionary* map, K key)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            var entry = UnsafeOrderedCollection.Find(&map->_collection, key);
            if (entry == null)
            {
                throw new ArgumentException(string.Format(ThrowHelper.Arg_KeyNotFoundWithKey, key));
            }

            return *GetValue<V>(map->_valueOffset, entry);
        }

        public static bool TryGetValue<K, V>(UnsafeSortedDictionary* map, K key, out V val)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
            var entry = UnsafeOrderedCollection.Find<K>(&map->_collection, key);
            if (entry != null)
            {
                val = *GetValue<V>(map->_valueOffset, entry);
                return true;
            }

            val = default;
            return false;
        }

        public static void Clear(UnsafeSortedDictionary* map)
        {
            UDebug.Assert(map != null);

            UnsafeOrderedCollection.Clear(&map->_collection);
        }

        public static void CopyTo<K, V>(UnsafeSortedDictionary* map, KeyValuePair<K, V>[] destination, int destinationIndex)
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static V* GetValue<V>(int offset, UnsafeOrderedCollection.Entry* pair)
            where V : unmanaged
        {
            return (V*)((byte*)pair + offset);
        }

        public static Enumerator<K, V> GetEnumerator<K, V>(UnsafeSortedDictionary* map)
          where K : unmanaged, IComparable<K>
          where V : unmanaged
        {
            UDebug.Assert(map != null);
            UDebug.Assert(typeof(K).TypeHandle.Value == map->_typeHandleKey);
            UDebug.Assert(typeof(V).TypeHandle.Value == map->_typeHandleValue);

            return new Enumerator<K, V>(map);
        }


        public unsafe struct Enumerator<K, V> : IUnsafeEnumerator<KeyValuePair<K, V>>
            where K : unmanaged, IComparable<K>
            where V : unmanaged
        {

            UnsafeOrderedCollection.Enumerator _iterator;
            readonly int _keyOffset;
            readonly int _valueOffset;

            public Enumerator(UnsafeSortedDictionary* dictionary)
            {
                _valueOffset = dictionary->_valueOffset;
                _keyOffset = dictionary->_collection.KeyOffset;
                _iterator = new UnsafeOrderedCollection.Enumerator(&dictionary->_collection);
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
    }
}

