using System;
using System.Diagnostics;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeReadOnlyCollectionDebugView<T> where T : unmanaged
    {
        private readonly INativeReadOnlyCollection<T> m_collection;

        public NativeReadOnlyCollectionDebugView(INativeReadOnlyCollection<T> collection)
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
