using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeLinkedListTests
    {
        private void FillList(UnsafeLinkedList* llist)
        {
            for (int i = 0; i < 10; i++)
            {
                UnsafeLinkedList.AddLast<int>(llist, i);
            }
        }


        [Test]
        public void ConstructorTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void AddFirstTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddFirst(llist, 3);
            UnsafeLinkedList.AddFirst(llist, 9);
            UnsafeLinkedList.AddFirst(llist, 0);

            Assert.AreEqual(3, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void AddLastTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddLast(llist, 3);
            UnsafeLinkedList.AddLast(llist, 9);
            UnsafeLinkedList.AddLast(llist, 0);

            Assert.AreEqual(3, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void AddMixedTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddLast(llist, 3);
            UnsafeLinkedList.AddFirst(llist, 2);
            UnsafeLinkedList.AddLast(llist, 4);
            UnsafeLinkedList.AddLast(llist, 5);
            UnsafeLinkedList.AddFirst(llist, 1);

            var index = 0;
            foreach (int num in UnsafeLinkedList.GetEnumerator<int>(llist))
            {
                Assert.AreEqual(++index, num);
            }

            Assert.AreEqual(5, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void ClearTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            Assert.AreEqual(10, UnsafeLinkedList.GetCount(llist));
            UnsafeLinkedList.Clear(llist);
            Assert.AreEqual(0, UnsafeLinkedList.GetCount(llist));


            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void IteratorTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            var enumerator = UnsafeLinkedList.GetEnumerator<int>(llist);

            var index = 0;
            while (enumerator.MoveNext())
            {
                Assert.AreEqual(index++, enumerator.Current);
            }

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void GetFirstTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            Assert.AreEqual(0, UnsafeLinkedList.GetFirst<int>(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void GetLastTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            Assert.AreEqual(9, UnsafeLinkedList.GetLast<int>(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void RemoveToTail()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, UnsafeLinkedList.GetFirst<int>(llist));
                Assert.AreEqual(10 - i, UnsafeLinkedList.GetCount(llist));
                UnsafeLinkedList.RemoveFirst(llist);
            }

            Assert.AreEqual(0, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void RemoveToHead()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            for (int i = 9; i >= 0; i--)
            {
                Assert.AreEqual(i, UnsafeLinkedList.GetLast<int>(llist));
                Assert.AreEqual(i + 1, UnsafeLinkedList.GetCount(llist));
                UnsafeLinkedList.RemoveLast(llist);
            }

            Assert.AreEqual(0, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void RemoveItemTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddLast(llist, 1);
            UnsafeLinkedList.AddLast(llist, 2);
            UnsafeLinkedList.AddLast(llist, 3);

            Assert.AreEqual(3, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Remove(llist, 2);

            Assert.AreEqual(2, UnsafeLinkedList.GetCount(llist));

            var arr = stackalloc int[2];
            UnsafeLinkedList.CopyTo<int>(llist, arr, 0);

            Assert.AreEqual(1, arr[0]);
            Assert.AreEqual(3, arr[1]);

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void FindNodeTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddLast(llist, 1);
            UnsafeLinkedList.AddLast(llist, 2);
            UnsafeLinkedList.AddLast(llist, 3);

            var node = UnsafeLinkedList.FindNode(llist, 2);
            Assert.AreEqual(2, UnsafeLinkedList.Node.GetItem<int>(node));

            node = UnsafeLinkedList.FindNode(llist, 5);
            Assert.IsTrue(node == null);

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void RemoveNodeTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddLast(llist, 1);
            UnsafeLinkedList.AddLast(llist, 2);
            UnsafeLinkedList.AddLast(llist, 3);

            // First grab the node
            var node = UnsafeLinkedList.FindNode<int>(llist, 3);

            // Remove the node
            var result = UnsafeLinkedList.Remove(llist, ref node);

            Assert.IsTrue(result);
            Assert.IsTrue(node == null);

            Assert.AreEqual(2, UnsafeLinkedList.GetCount(llist));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void CopyToTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            var arr = stackalloc int[10];
            UnsafeLinkedList.CopyTo<int>(llist, arr, 0);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, arr[i]);

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void ContainsTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();
            FillList(llist);

            // Fill list adds numbers 0-9.
            Assert.IsTrue(UnsafeLinkedList.Contains(llist, 5));
            Assert.IsTrue(UnsafeLinkedList.Contains(llist, 0));
            Assert.IsFalse(UnsafeLinkedList.Contains(llist, 10));

            UnsafeLinkedList.Free(llist);
        }

        [Test]
        public void AddAfterTest()
        {
            var llist = UnsafeLinkedList.Allocate<int>();

            UnsafeLinkedList.AddLast(llist, 1);
            UnsafeLinkedList.AddLast(llist, 3);
            UnsafeLinkedList.AddLast(llist, 5);

            // Add a node after 1 and 3 so we have a 1 - 5 sequence
            var node = UnsafeLinkedList.FindNode(llist, 1);
            UnsafeLinkedList.AddAfter(llist, ref node, 2);

            node = UnsafeLinkedList.FindNode(llist, 3);
            UnsafeLinkedList.AddAfter(llist, ref node, 4);

            Assert.AreEqual(5, UnsafeLinkedList.GetCount(llist));

            var enumerator = UnsafeLinkedList.GetEnumerator<int>(llist);
            enumerator.MoveNext();

            for (int i = 1; i < 5; i++)
            {
                Assert.AreEqual(i, enumerator.Current);
                enumerator.MoveNext();
            }

            UnsafeLinkedList.Free(llist);
        }
    }
}
