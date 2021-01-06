﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeCollectionDebugView<T> where T : unmanaged
    {
        private readonly INativeCollection<T> m_collection;

        public NativeCollectionDebugView(INativeCollection<T> collection)
        {
            if (collection == null || !collection.IsCreated)
                throw new ArgumentNullException(nameof(collection));

            m_collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                var arr = new T[m_collection.Count];
                m_collection.CopyTo(arr, 0);

                return arr;
            }
        }
    }
}
