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

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeOrderedMap
    {
        UnsafeOrderedCollection _collection;
        IntPtr _typeHandle;

        public static UnsafeHashMap* Allocate<K, V>(int capacity, bool fixedSize = false)
          where K : unmanaged, IComparable<K>
          where V : unmanaged
        {
            var keyStride = sizeof(K);
            var valStride = sizeof(V);
            var entryStride = sizeof(UnsafeHashCollection.Entry);


            throw new NotImplementedException();
        }

        public static void Free(UnsafeOrderedMap* map)
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
    }
}

