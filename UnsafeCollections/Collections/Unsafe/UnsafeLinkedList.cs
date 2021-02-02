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
    public unsafe struct UnsafeLinkedList
    {
        private const int ALIGNMENT = 8;

        Node* _head;
        Node* _tail;
        IntPtr _typeHandle;
        int _count;
        int _nodestride;

        public static UnsafeLinkedList* Allocate<T>()
            where T : unmanaged
        {
            UnsafeLinkedList* linkedList;

            var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeLinkedList), ALIGNMENT);
            var ptr = Memory.MallocAndZero(sizeOfHeader, ALIGNMENT);

            linkedList = (UnsafeLinkedList*)ptr;

            linkedList->_nodestride = Memory.RoundToAlignment(sizeof(T), ALIGNMENT) + ALIGNMENT;
            linkedList->_count = 0;
            linkedList->_head = default;
            linkedList->_typeHandle = typeof(T).TypeHandle.Value;

            return linkedList;
        }

        public static void Free(UnsafeLinkedList* llist)
        {
            if (llist == null)
                return;

            Clear(llist);

            // clear memory
            *llist = default;

            // free list
            Memory.Free(llist);
        }


        public static int GetCount(UnsafeLinkedList* llist)
        {
            UDebug.Assert(llist != null);
            return llist->_count;
        }

        public static void Clear(UnsafeLinkedList* llist)
        {
            UDebug.Assert(llist != null);

            for (Node* node = llist->_head; node != null; node = node->_next)
            {
                Memory.Free(node);
            }

            llist->_head = null;
            llist->_tail = null;
            llist->_count = 0;
        }


        public static T GetFirst<T>(UnsafeLinkedList* llist)
            where T : unmanaged
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            var head = llist->_head;
            return head == null ? default : *GetItemFromNode<T>(head);
        }

        public static T GetLast<T>(UnsafeLinkedList* llist)
            where T : unmanaged
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            var tail = llist->_tail;
            return tail == null ? default : *GetItemFromNode<T>(tail);
        }


        public static void AddFirst<T>(UnsafeLinkedList* llist, T item)
            where T : unmanaged
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            var node = CreateNode(item, llist->_nodestride);

            if (llist->_head == null)
            {
                llist->_head = llist->_tail = node;
            }
            else
            {
                // Set the head->next to this node and make node the new head.
                node->_next = llist->_head;
                llist->_head = node;
            }

            llist->_count++;
        }

        public static void AddLast<T>(UnsafeLinkedList* llist, T item)
            where T : unmanaged
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            var node = CreateNode(item, llist->_nodestride);

            if (llist->_head == null)
            {
                llist->_head = llist->_tail = node;
            }
            else
            {
                // Set the tail->next to this node and make node the new tail.
                llist->_tail->_next = node;
                llist->_tail = node;
            }

            llist->_count++;
        }

        public static void AddAfter<T>(UnsafeLinkedList* llist, Node* previousNode, T item)
            where T : unmanaged
        {
            if (previousNode == null)
                throw new ArgumentNullException(nameof(previousNode));

            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            var node = CreateNode(item, llist->_nodestride);
            node->_next = previousNode->_next;
            previousNode->_next = node;

            // If the next node is null, this is the tail.
            if (node->_next == null)
                llist->_tail = node;

            llist->_count++;
        }

        public static bool Contains<T>(UnsafeLinkedList* llist, T item)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            return FindNode(llist, item) != null;
        }


        public static bool Remove(UnsafeLinkedList* llist, ref Node* node)
        {
            UDebug.Assert(llist != null);

            if (node == null)
                throw new ArgumentNullException(nameof(node));

            Node* prev = null;
            Node* head = llist->_head;

            while (head != null)
            {
                if (head == node)
                {
                    llist->DeleteNode(head, prev);
                    node = null;

                    return true;
                }

                // Advance to next node and remember the prev node.
                prev = head;
                head = head->_next;
            }

            return false;
        }

        public static bool Remove<T>(UnsafeLinkedList* llist, T item)
            where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            Node* prev = null;
            Node* node = llist->_head;

            while (node != null)
            {
                if (item.Equals(*GetItemFromNode<T>(node)))
                {
                    llist->DeleteNode(node, prev);
                    return true;
                }

                // Advance to next node and remember the prev node.
                prev = node;
                node = node->_next;
            }

            return false;
        }

        internal static bool RemoveSlow<T>(UnsafeLinkedList* llist, T item)
            where T : unmanaged
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            Node* prev = null;
            Node* node = llist->_head;

            var eq = EqualityComparer<T>.Default;

            while (node != null)
            {
                if (eq.Equals(item, *GetItemFromNode<T>(node)))
                {
                    llist->DeleteNode(node, prev);
                    return true;
                }

                // Advance to next node and remember the prev node.
                prev = node;
                node = node->_next;
            }

            return false;
        }

        public static void RemoveFirst(UnsafeLinkedList* llist)
        {
            UDebug.Assert(llist != null);

            var head = llist->_head;
            if (head == null)
                throw new InvalidOperationException(ThrowHelper.@InvalidOperation_EmptyLinkedList);

            // One item in the list.
            if (head->_next == null)
                llist->_head = llist->_tail = null;

            llist->_head = head->_next;
            Memory.Free(head);

            llist->_count--;
        }

        public static void RemoveLast(UnsafeLinkedList* llist)
        {
            UDebug.Assert(llist != null);

            var tail = llist->_tail;
            if (tail == null)
                throw new InvalidOperationException(ThrowHelper.@InvalidOperation_EmptyLinkedList);

            var node = llist->_head;
            // One item in the list
            if (node->_next == null)
            {
                llist->_head = llist->_tail = null;
                Memory.Free(node);
                llist->_count--;
                return;
            }

            // Grab second-last node.
            while (node->_next->_next != null)
                node = node->_next;

            Memory.Free(node->_next);
            node->_next = null;

            llist->_tail = node;
            llist->_count--;
            return;
        }


        public static void CopyTo<T>(UnsafeLinkedList* llist, void* destination, int destinationIndex)
            where T : unmanaged
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);

            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            int numToCopy = llist->_count;
            if (numToCopy == 0)
                return;

            var dest = (T*)destination;
            var enumerator = GetEnumerator<T>(llist);

            var index = 0;
            while (enumerator.MoveNext())
            {
                dest[destinationIndex + index] = enumerator.Current;
                index++;
            }
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeLinkedList* llist)
            where T : unmanaged
        {
            UDebug.Assert(llist != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == llist->_typeHandle);

            return new Enumerator<T>(llist->_head);
        }


        public static Node* FindNode<T>(UnsafeLinkedList* llist, T item)
            where T : unmanaged, IEquatable<T>
        {
            for (Node* node = llist->_head; node != null; node = node->_next)
            {
                if (item.Equals(*GetItemFromNode<T>(node)))
                    return node;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DeleteNode(Node* node, Node* prev)
        {
            UDebug.Assert(node != null);

            // Removing the head.
            if (node == _head)
            {
                _head = node->_next;
            }
            else
            {
                prev->_next = node->_next;
            }

            // Removing the tail
            if (node->_next == null)
                _tail = prev;

            // Free the node
            Memory.Free(node);
            _count--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T* GetItemFromNode<T>(Node* node)
            where T : unmanaged
        {
            return (T*)((byte*)node + ALIGNMENT);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Node* CreateNode<T>(T item, int stride)
            where T : unmanaged
        {
            var node = (Node*)Memory.Malloc(stride);

            // Clear next
            node->_next = default;

            // Add item to slot
            *(T*)((byte*)node + ALIGNMENT) = item;

            return node;
        }

        /// <summary>
        /// A LinkedList node belonging to a <see cref="UnsafeLinkedList"/>.
        /// </summary>
        public struct Node
        {
            internal Node* _next;

            public static T GetItem<T>(Node* node)
                where T : unmanaged
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                return *(T*)((byte*)node + ALIGNMENT);
            }

            public static void SetItem<T>(Node* node, T item)
                where T : unmanaged
            {
                if (node == null)
                {
                    throw new ArgumentNullException(nameof(node));
                }

                *(T*)((byte*)node + ALIGNMENT) = item;
            }
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            T* _current;
            Node* _node;

            internal Enumerator(Node* head)
            {
                _current = default;
                _node = head;
            }

            public void Dispose()
            {
                _node = null;
                _current = null;
            }

            public bool MoveNext()
            {
                if (_node == null)
                    return false;

                _current = GetItemFromNode<T>(_node);

                // Move to next head.
                _node = _node->_next;

                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_current != null);
                    return *_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_current == null)
                        throw new InvalidOperationException(ThrowHelper.InvalidOperation_EnumOpCantHappen);

                    return Current;
                }
            }

            public Enumerator<T> GetEnumerator()
            {
                return this;
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
}
