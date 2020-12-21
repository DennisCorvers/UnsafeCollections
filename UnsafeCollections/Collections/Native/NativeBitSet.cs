/*
The MIT License (MIT)

Copyright (c) 2020 Dennis Corvers

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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnsafeCollections.Collections.Unsafe;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Size = {Size}")]
    [DebuggerTypeProxy(typeof(NativeBitSetDebugView))]
    public unsafe struct NativeBitSet : IDisposable, IEnumerable, IEnumerable<(int bit, bool set)>
    {
        private UnsafeBitSet* m_inner;

        public bool IsCreated
        {
            get
            {
                return m_inner != null;
            }
        }
        public int Size
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeBitSet.GetSize(m_inner);
            }
        }

        public bool this[int index]
        {
            get
            {
                return UnsafeBitSet.IsSet(m_inner, index);
            }
            set
            {
                UnsafeBitSet.Set(m_inner, index, value);
            }
        }


        public NativeBitSet(int size)
        {
            m_inner = UnsafeBitSet.Allocate(size);
        }


        public void Clear()
        {
            UnsafeBitSet.Clear(m_inner);
        }

        public byte[] ToArray()
        {
            var arr = new byte[Size];

            int i = 0;
            foreach (var (bit, set) in GetEnumerator())
                arr[i++] = (byte)bit;

            return arr;
        }

        public UnsafeBitSet.Enumerator GetEnumerator()
        {
            return UnsafeBitSet.GetEnumerator(m_inner);
        }
        IEnumerator<(int bit, bool set)> IEnumerable<(int bit, bool set)>.GetEnumerator()
        {
            return UnsafeBitSet.GetEnumerator(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeBitSet.GetEnumerator(m_inner);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeBitSet.Free(m_inner);
            m_inner = null;
        }

        public static NativeBitSet operator |(NativeBitSet set, NativeBitSet other)
        {
            UnsafeBitSet.Or(set.m_inner, other.m_inner);
            return set;
        }

        public static NativeBitSet operator &(NativeBitSet set, NativeBitSet other)
        {
            UnsafeBitSet.And(set.m_inner, other.m_inner);
            return set;
        }

        public static NativeBitSet operator ^(NativeBitSet set, NativeBitSet other)
        {
            UnsafeBitSet.Xor(set.m_inner, other.m_inner);
            return set;
        }
    }
}
