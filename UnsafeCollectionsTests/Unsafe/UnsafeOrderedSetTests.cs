using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeOrderedSetTests
    {
        [Test]
        public void ConstructorTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(10, UnsafeSortedSet.GetCapacity(set));

            UnsafeSortedSet.Free(set);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeSortedSet.Add<float>(set, 4); });

            UnsafeSortedSet.Free(set);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
            {
                // Add in reverse order
                UnsafeSortedSet.Add(set, i);
            }

            var enumerator = UnsafeSortedSet.GetEnumerator<int>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.Current);
            }

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void ExpandFailedTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(8, true);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeSortedSet.Add(set, i + 1);

            Assert.AreEqual(8, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(8, UnsafeSortedSet.GetCapacity(set));

            Assert.Throws<InvalidOperationException>(() =>
            {
                UnsafeSortedSet.Add(set, 42);
            });

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void ExpandTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(8, fixedSize: false);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeSortedSet.Add(set, i + 1);

            Assert.AreEqual(8, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(8, UnsafeSortedSet.GetCapacity(set));

            UnsafeSortedSet.Add(set, 42);

            Assert.AreEqual(9, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedSet.GetCapacity(set));

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void ContainsTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            Assert.IsFalse(UnsafeSortedSet.Contains<int>(set, 1));

            UnsafeSortedSet.Add(set, 1);
            UnsafeSortedSet.Add(set, 7);
            UnsafeSortedSet.Add(set, 51);
            UnsafeSortedSet.Add(set, 13);

            Assert.IsFalse(UnsafeSortedSet.Contains<int>(set, 3));

            Assert.IsTrue(UnsafeSortedSet.Contains<int>(set, 1));
            Assert.IsTrue(UnsafeSortedSet.Contains<int>(set, 7));
            Assert.IsTrue(UnsafeSortedSet.Contains<int>(set, 13));
            Assert.IsTrue(UnsafeSortedSet.Contains<int>(set, 51));

            Assert.IsFalse(UnsafeSortedSet.Contains<int>(set, 14));

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void RemoveTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            Assert.IsFalse(UnsafeSortedSet.Remove<int>(set, 1));

            UnsafeSortedSet.Add(set, 1);
            UnsafeSortedSet.Add(set, 7);
            UnsafeSortedSet.Add(set, 51);
            UnsafeSortedSet.Add(set, 13);

            Assert.IsFalse(UnsafeSortedSet.Remove<int>(set, 3));

            Assert.IsTrue(UnsafeSortedSet.Remove<int>(set, 1));
            Assert.IsTrue(UnsafeSortedSet.Remove<int>(set, 7));
            Assert.IsTrue(UnsafeSortedSet.Remove<int>(set, 13));
            Assert.IsTrue(UnsafeSortedSet.Remove<int>(set, 51));

            Assert.IsFalse(UnsafeSortedSet.Remove<int>(set, 13));

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void AddRandomTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            Random r = new Random();
            for (int i = 0; i < 10; i++)
                UnsafeSortedSet.Add<int>(set, r.Next());

            int* arr = stackalloc int[10];
            UnsafeSortedSet.CopyTo<int>(set, arr, 0);

            // Validate values are from low to high
            int last = arr[0];
            for (int i = 1; i < 10; i++)
            {
                Assert.IsTrue(last <= arr[i]);
                last = arr[i++];
            }

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void CopyToTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
                UnsafeSortedSet.Add(set, i);

            var count = UnsafeSortedSet.GetCount(set);
            int* arr = stackalloc int[count];

            UnsafeSortedSet.CopyTo<int>(set, arr, 0);

            // Check
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++]);

            UnsafeSortedSet.Free(set);
        }

        [Test]
        public void ClearTest()
        {
            var set = UnsafeSortedSet.Allocate<int>(16, fixedSize: false);

            // Add some random data
            Random r = new Random();
            for (int i = 0; i < 10; i++)
                UnsafeSortedSet.Add<int>(set, r.Next());

            // Verify data has been added
            Assert.AreEqual(10, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedSet.GetCapacity(set));

            // Clear set and verify it's cleared
            UnsafeSortedSet.Clear(set);

            Assert.AreEqual(0, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedSet.GetCapacity(set));


            // Validate we can still add data and have it be valid
            // Add data to cleared set
            for (int i = 10; i >= 0; i--)
                UnsafeSortedSet.Add(set, i);

            var count = UnsafeSortedSet.GetCount(set);
            int* arr = stackalloc int[count];

            UnsafeSortedSet.CopyTo<int>(set, arr, 0);

            // Validate data has been written
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++]);

            Assert.AreEqual(count, UnsafeSortedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedSet.GetCapacity(set));

            UnsafeSortedSet.Free(set);
        }
    }
}
