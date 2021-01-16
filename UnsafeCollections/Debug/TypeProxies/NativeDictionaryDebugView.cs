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
using System.Collections.Generic;
using System.Diagnostics;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeDictionaryDebugView<K, V>
        where K : unmanaged
        where V : unmanaged
    {
        private readonly INativeDictionary<K, V> _dictionary;

        public NativeDictionaryDebugView(INativeDictionary<K, V> dictionary)
        {
            if (dictionary == null || !dictionary.IsCreated)
                throw new ArgumentNullException(nameof(dictionary));
            _dictionary = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<K, V>[] Items
        {
            get
            {
                var items = new KeyValuePair<K, V>[_dictionary.Count];
                _dictionary.CopyTo(items, 0);

                return items;
            }
        }
    }

    internal struct NativeDictionaryKeyCollectionDebugView<K, V>
        where K : unmanaged
        where V : unmanaged
    {
        private readonly ICollection<K> _collection;

        public NativeDictionaryKeyCollectionDebugView(ICollection<K> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public K[] Items
        {
            get
            {
                K[] items = new K[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    internal struct NativeDictionaryValueCollectionDebugView<K, V>
    where K : unmanaged
    where V : unmanaged
    {
        private readonly ICollection<V> _collection;

        public NativeDictionaryValueCollectionDebugView(ICollection<V> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public V[] Items
        {
            get
            {
                V[] items = new V[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}
