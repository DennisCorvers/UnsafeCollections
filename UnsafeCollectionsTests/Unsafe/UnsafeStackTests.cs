using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeStackTests
    {
        [Test]
        public void ConstructorTest()
        {
            var arr = UnsafeStack.Allocate<int>(10);

            Assert.AreEqual(UnsafeStack.GetCount(arr), 0);

            UnsafeStack.Free(arr);
        }

        [Test]
        public void MutateTest()
        {
            var arr = UnsafeStack.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeStack.Push(arr, i);
            }

            for (int i = 9; i >= 0; i--)
            {
                Assert.AreEqual(i, UnsafeStack.Pop<int>(arr));
            }

            UnsafeStack.Free(arr);
        }

        [Test]
        public void ExpandTest()
        {
            var arr = UnsafeStack.Allocate<int>(8);

            for (int i = 0; i < 10; i++)
            {
                UnsafeStack.Push(arr, i);
            }

            Assert.IsTrue(UnsafeStack.GetCapacity(arr) > 8);

            UnsafeStack.Free(arr);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var arr = UnsafeStack.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeStack.Push<float>(arr, 20); });

            UnsafeStack.Free(arr);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var arr = UnsafeStack.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeStack.Push(arr, i);

            Assert.AreEqual(10, UnsafeStack.GetCount(arr));

            int num = 9;
            foreach (int i in UnsafeStack.GetEnumerator<int>(arr))
            {
                Assert.AreEqual(num--, i);
            }

            UnsafeStack.Free(arr);
        }

        [Test]
        public void PopTest()
        {
            var arr = UnsafeStack.Allocate<int>(10);
            for (int i = 1; i <= 10; i++)
                UnsafeStack.Push(arr, i);

            Assert.AreEqual(10, UnsafeStack.GetCount(arr));

            var val = UnsafeStack.Pop<int>(arr);
            Assert.AreEqual(10, val);
            Assert.AreEqual(9, UnsafeStack.GetCount(arr));

            // Reverse stack-iteration
            for (int i = 9; i > 0; i--)
            {
                var num = UnsafeStack.Pop<int>(arr);
                Assert.AreEqual(i, num);
            }

            Assert.AreEqual(0, UnsafeStack.GetCount(arr));
        }

        [Test]
        public void ContainsTest()
        {
            var arr = UnsafeStack.Allocate<int>(10);
            for (int i = 1; i <= 10; i++)
                UnsafeStack.Push(arr, i);

            Assert.IsTrue(UnsafeStack.Contains(arr, 1));
            Assert.IsTrue(UnsafeStack.Contains(arr, 10));
            Assert.IsFalse(UnsafeStack.Contains(arr, 11));
        }

        [Test]
        public void CopyToTest()
        {
            var q = UnsafeStack.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeStack.Push(q, i);
            }

            var arr = new int[10];
            fixed (void* ptr = arr)
            {
                UnsafeStack.CopyTo<int>(q, ptr, 0);
            }

            int num = 10;
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(--num, arr[i]);
            }
        }
    }
}
