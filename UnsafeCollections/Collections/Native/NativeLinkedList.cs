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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnsafeCollections.Collections.Unsafe;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeCollectionDebugView<>))]
    public unsafe struct NativeLinkedList<T> : INativeCollection<T>, INativeReadOnlyCollection<T> where T : unmanaged
    {
        private UnsafeLinkedList* m_inner;

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
                return UnsafeLinkedList.GetCount(m_inner);
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeLinkedList* GetInnerCollection()
        {
            return m_inner;
        }

        /// <summary>
        /// Constructor for <see cref="NativeLinkedList{T}"/>.
        /// </summary>
        /// <param name="create">Dummy value that does nothing.</param>
        public NativeLinkedList(bool create)
        {
            m_inner = UnsafeLinkedList.Allocate<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetFirst()
        {
            return UnsafeLinkedList.GetFirst<T>(m_inner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetLast()
        {
            return UnsafeLinkedList.GetLast<T>(m_inner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddFirst(T item)
        {
            UnsafeLinkedList.AddFirst(m_inner, item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddLast(T item)
        {
            UnsafeLinkedList.AddLast(m_inner, item);
        }

        /// <summary>
        /// Adds a node to the <see cref="NativeLinkedList{T}"/> after the given <see cref="Node"/>.
        /// The <see cref="Node"/> will be invalidated.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddAfter(ref Node previousNode, T item)
        {
            UnsafeLinkedList.AddAfter(m_inner, previousNode._node, item);
            previousNode._node = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(ref Node node)
        {
            return UnsafeLinkedList.Remove(m_inner, ref node._node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveFirst()
        {
            UnsafeLinkedList.RemoveFirst(m_inner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveLast()
        {
            UnsafeLinkedList.RemoveLast(m_inner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            UnsafeLinkedList.Clear(m_inner);
        }

        public T[] ToArray()
        {
            if (Count == 0)
                return Array.Empty<T>();

            var arr = new T[Count];

            CopyTo(arr, 0);

            return arr;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if ((uint)arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));

            if (array.Length - arrayIndex < Count)
                throw new ArgumentException("Insufficient space in the target location to copy the information.");

            if (array.Length == 0)
                return;

            fixed (void* ptr = array)
                UnsafeLinkedList.CopyTo<T>(m_inner, ptr, arrayIndex);
        }

        public NativeArray<T> ToNativeArray()
        {
            if (Count == 0)
                return NativeArray.Empty<T>();

            var arr = new NativeArray<T>(Count);
            UnsafeLinkedList.CopyTo<T>(m_inner, UnsafeArray.GetBuffer(arr.GetInnerCollection()), 0);

            return arr;
        }

        public UnsafeLinkedList.Enumerator<T> GetEnumerator()
        {
            return UnsafeLinkedList.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeLinkedList.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeLinkedList.GetEnumerator<T>(m_inner);
        }

        void ICollection<T>.Add(T item)
        {
            UnsafeLinkedList.AddLast(m_inner, item);
        }

        bool ICollection<T>.Contains(T item)
        {
            var eq = EqualityComparer<T>.Default;

            foreach (var enumItem in UnsafeLinkedList.GetEnumerator<T>(m_inner))
            {
                if (eq.Equals(item, enumItem))
                    return true;
            }
            return false;
        }

        bool ICollection<T>.Remove(T item)
        {
            return UnsafeLinkedList.RemoveSlow(m_inner, item);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeLinkedList.Free(m_inner);
            m_inner = null;
        }

        [DebuggerDisplay("Item = {Item}")]
        public struct Node
        {
            internal UnsafeLinkedList.Node* _node;

            public bool IsValid
            {
                get => _node != null;
            }
            public T Item
            {
                get => UnsafeLinkedList.Node.GetItem<T>(_node);

                set => UnsafeLinkedList.Node.SetItem<T>(_node, value);
            }

            internal Node(UnsafeLinkedList.Node* node)
            {
                _node = node;
            }
        }
    }

    // Extension methods are used to add extra constraints to <T>
    public unsafe static class NativeLinkedListExtensions
    {
        public static bool Contains<T>(this NativeLinkedList<T> linkedList, T item)
            where T : unmanaged, IEquatable<T>
        {
            return UnsafeLinkedList.Contains<T>(linkedList.GetInnerCollection(), item);
        }

        public static bool Remove<T>(this NativeLinkedList<T> linkedList, T item)
            where T : unmanaged, IEquatable<T>
        {
            return UnsafeLinkedList.Remove<T>(linkedList.GetInnerCollection(), item);
        }

        public static NativeLinkedList<T>.Node Find<T>(this NativeLinkedList<T> linkedList, T item)
            where T : unmanaged, IEquatable<T>
        {
            var node = UnsafeLinkedList.FindNode(linkedList.GetInnerCollection(), item);
            return new NativeLinkedList<T>.Node(node);
        }
    }
}
