using System;
using System.Diagnostics;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeBitSetDebugView
    {
        private readonly NativeBitSet m_bitset;

        public NativeBitSetDebugView(NativeBitSet bitset)
        {
            if (!bitset.IsCreated)
                throw new ArgumentNullException(nameof(bitset));

            m_bitset = bitset;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items => m_bitset.ToArray();
    }
}
