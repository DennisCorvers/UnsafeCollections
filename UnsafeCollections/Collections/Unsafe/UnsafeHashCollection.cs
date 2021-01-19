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
using System.Runtime.InteropServices;
using UnsafeCollections.Debug;

namespace UnsafeCollections.Collections.Unsafe
{
    internal unsafe struct UnsafeHashCollection
    {
        public enum EntryState
        {
            None = 0,
            Free = 1,
            Used = 2,
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Entry
        {
            public const int ALIGNMENT = 8;

            public Entry* Next;
            public int Hash;
            public EntryState State;
        }

        internal struct Enumarator
        {
            int _index;

            // current entry and collection
            public Entry* Current;
            public UnsafeHashCollection* Collection;

            public Enumarator(UnsafeHashCollection* collection)
            {
                _index = -1;

                //
                Current = null;
                Collection = collection;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                while (++_index < Collection->UsedCount)
                {
                    var entry = GetEntry(Collection, _index);
                    if (entry->State == EntryState.Used)
                    {
                        Current = entry;
                        return true;
                    }
                }

                Current = null;
                return false;
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        static int[] _primeTable = new[] {
      3,
      7,
      17,
      29,
      53,
      97,
      193,
      389,
      769,
      1543,
      3079,
      6151,
      12289,
      24593,
      49157,
      98317,
      196613,
      393241,
      786433,
      1572869,
      3145739,
      6291469,
      12582917,
      25165843,
      50331653,
      100663319,
      201326611,
      402653189,
      805306457,
      1610612741
    };

        public Entry** Buckets;
        public Entry* FreeHead;

        // buffer for our entries
        public UnsafeBuffer Entries;

        public int UsedCount;
        public int FreeCount;
        public int KeyOffset;

        public static int GetNextPrime(int value)
        {
            value--;

            // Start at index 0 so the minimum size is 3.
            for (int i = 0; i < _primeTable.Length; i++)
            {
                var prime = _primeTable[i];

                if(prime > value)
                {
                    return prime; 
                }
            }
            throw new InvalidOperationException($"HashCollection can't get larger than {_primeTable[_primeTable.Length - 1]}");
        }

        public static void Free(UnsafeHashCollection* collection)
        {
            if (collection == null)
                return;

            UDebug.Assert(collection->Entries.Dynamic == 1);

            Memory.Free(collection->Buckets);
            Memory.Free(collection->Entries.Ptr);

            *collection = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Entry* GetEntry(UnsafeHashCollection* collection, int index)
        {
            return collection->Entries.Element<Entry>(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetKey<T>(UnsafeHashCollection* collection, Entry* entry) where T : unmanaged
        {
            return *(T*)((byte*)entry + collection->KeyOffset);
        }

        public static Entry* Find<T>(UnsafeHashCollection* collection, T value, int valueHash) where T : unmanaged, IEquatable<T>
        {
            var bucketHead = collection->Buckets[valueHash % collection->Entries.Length];

            while (bucketHead != null)
            {
                if (bucketHead->Hash == valueHash && value.Equals(*(T*)((byte*)bucketHead + collection->KeyOffset)))
                {
                    return bucketHead;
                }
                else
                {
                    bucketHead = bucketHead->Next;
                }
            }

            return null;
        }

        public static bool Remove<T>(UnsafeHashCollection* collection, T value, int valueHash) where T : unmanaged, IEquatable<T>
        {
            var bucketHash = valueHash % collection->Entries.Length;
            var bucketHead = collection->Buckets[valueHash % collection->Entries.Length];
            var bucketPrev = default(Entry*);

            while (bucketHead != null)
            {
                if (bucketHead->Hash == valueHash && value.Equals(*(T*)((byte*)bucketHead + collection->KeyOffset)))
                {
                    // if previous was null, this means we're at the head of the list
                    if (bucketPrev == null)
                    {
                        collection->Buckets[bucketHash] = bucketHead->Next;
                    }

                    // previous was not null, it means we're in the middle
                    // of the list so stitch the elements together
                    else
                    {
                        bucketPrev->Next = bucketHead->Next;
                    }

                    UDebug.Assert(bucketHead->State == EntryState.Used);

                    // next for the node we removed becomes current free list head
                    bucketHead->Next = collection->FreeHead;
                    bucketHead->State = EntryState.Free;

                    // set it as free list head, and increment count
                    collection->FreeHead = bucketHead;
                    collection->FreeCount = collection->FreeCount + 1;
                    return true;
                }
                else
                {
                    bucketPrev = bucketHead;
                    bucketHead = bucketHead->Next;
                }
            }

            return false;
        }

        public static Entry* Insert<T>(UnsafeHashCollection* collection, T value, int valueHash) where T : unmanaged
        {
            Entry* entry;

            if (collection->FreeHead != null)
            {
                UDebug.Assert(collection->FreeCount > 0);

                // entry we're adding
                entry = collection->FreeHead;

                // update free list
                collection->FreeHead = entry->Next;
                collection->FreeCount = collection->FreeCount - 1;

                // this HAS to be a FREE state entry, or something is seriously wrong
                UDebug.Assert(entry->State == EntryState.Free);
            }
            else
            {
                if (collection->UsedCount == collection->Entries.Length)
                {
                    // Cannot expand fixed-size HashCollection
                    if (collection->Entries.Dynamic == 0)
                    {
                        throw new InvalidOperationException(ThrowHelper.InvalidOperation_CollectionFull);
                    }

                    // !! IMPORTANT !!
                    // when this happens, it's very important to be
                    // aware of the fact that all pointers to to buckets
                    // or entries etc. are not valid anymore as we have
                    // re-allocated all of the memory
                    Expand(collection);
                }

                // grab 'next' element maintained by _count
                entry = collection->Entries.Element<Entry>(collection->UsedCount);

                // step up used count
                collection->UsedCount = collection->UsedCount + 1;

                // this HAS to be a NONE state entry, or something is seriously wrong
                UDebug.Assert(entry->State == EntryState.None);
            }

            // compute bucket hash
            var bucketHash = valueHash % collection->Entries.Length;

            // hook up entry
            entry->Hash = valueHash;
            entry->Next = collection->Buckets[bucketHash];
            entry->State = EntryState.Used;

            // store value
            *(T*)((byte*)entry + collection->KeyOffset) = value;

            // store as head on bucket
            collection->Buckets[bucketHash] = entry;

            // done!
            return entry;
        }

        public static void Clear(UnsafeHashCollection* collection)
        {
            collection->FreeHead = null;
            collection->FreeCount = 0;
            collection->UsedCount = 0;

            var length = collection->Entries.Length;

            Memory.ZeroMem(collection->Buckets, length * sizeof(Entry**));
            UnsafeBuffer.Clear(&collection->Entries);
        }

        static void Expand(UnsafeHashCollection* collection)
        {
            UDebug.Assert(collection->Entries.Dynamic == 1);

            var capacity = GetNextPrime(collection->Entries.Length);

            UDebug.Assert(capacity >= collection->Entries.Length);

            var newBuckets = (Entry**)Memory.MallocAndZero(capacity * sizeof(Entry**), sizeof(Entry**));
            var newEntries = default(UnsafeBuffer);

            UnsafeBuffer.InitDynamic(&newEntries, capacity, collection->Entries.Stride);
            UnsafeBuffer.Copy(collection->Entries, 0, newEntries, 0, collection->Entries.Length);

            collection->FreeHead = null;
            collection->FreeCount = 0;

            for (int i = collection->Entries.Length - 1; i >= 0; --i)
            {
                var entry = (Entry*)((byte*)newEntries.Ptr + (i * newEntries.Stride));
                if (entry->State == EntryState.Used)
                {
                    var bucketHash = entry->Hash % capacity;

                    // assign current entry in buckets as next
                    entry->Next = newBuckets[bucketHash];

                    // assign entry as new bucket head
                    newBuckets[bucketHash] = entry;
                }

                // entry is in free list
                else if (entry->State == EntryState.Free)
                {
                    // assign free list as next
                    entry->Next = collection->FreeHead;

                    // re-assign free list to entry
                    collection->FreeHead = entry;
                    collection->FreeCount = collection->FreeCount + 1;
                }
            }

            // free old memory
            Memory.Free(collection->Buckets);
            UnsafeBuffer.Free(&collection->Entries);

            // new storage
            collection->Buckets = newBuckets;
            collection->Entries = newEntries;
        }
    }
}