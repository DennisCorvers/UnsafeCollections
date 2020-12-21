﻿/*
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
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeCollectionDebugView<>))]
    public unsafe struct NativeQueue<T> : IDisposable, IEnumerable<T>, IEnumerable, INativeCollection<T> where T : unmanaged
    {
        private UnsafeQueue* m_inner;

        public bool IsCreated
        {
            get
            {
                return m_inner != null;
            }
        }
        public int Count
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeQueue.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeQueue.GetCapacity(m_inner);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeQueue* GetInnerCollection()
        {
            return m_inner;
        }




        public T[] ToArray()
        {
            throw new NotImplementedException();
        }

        public UnsafeQueue.Enumerator<T> GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeQueue.Free(m_inner);
            m_inner = null;
        }


    }

    //Extension methods are used to add extra constraints to <T>
    public unsafe static class NativeQueueExtensions
    {

    }
}
