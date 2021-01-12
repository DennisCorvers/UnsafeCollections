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
            var set = UnsafeOrderedSet.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(10, UnsafeOrderedSet.GetCapacity(set));

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void InvalidTypeTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeOrderedSet.Add<float>(set, 4); });

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void IteratorTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
            {
                // Add in reverse order
                UnsafeOrderedSet.Add(set, i);
            }

            var enumerator = UnsafeOrderedSet.GetEnumerator<int>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.Current);
            }

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void ExpandFailedTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(8, true);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeOrderedSet.Add(set, i + 1);

            Assert.AreEqual(8, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(8, UnsafeOrderedSet.GetCapacity(set));

            Assert.Throws<InvalidOperationException>(() =>
            {
                UnsafeOrderedSet.Add(set, 42);
            });

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void ExpandTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(8, fixedSize: false);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeOrderedSet.Add(set, i + 1);

            Assert.AreEqual(8, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(8, UnsafeOrderedSet.GetCapacity(set));

            UnsafeOrderedSet.Add(set, 42);

            Assert.AreEqual(9, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeOrderedSet.GetCapacity(set));

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void ContainsTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(10);

            Assert.IsFalse(UnsafeOrderedSet.Contains<int>(set, 1));

            UnsafeOrderedSet.Add(set, 1);
            UnsafeOrderedSet.Add(set, 7);
            UnsafeOrderedSet.Add(set, 51);
            UnsafeOrderedSet.Add(set, 13);

            Assert.IsFalse(UnsafeOrderedSet.Contains<int>(set, 3));

            Assert.IsTrue(UnsafeOrderedSet.Contains<int>(set, 1));
            Assert.IsTrue(UnsafeOrderedSet.Contains<int>(set, 7));
            Assert.IsTrue(UnsafeOrderedSet.Contains<int>(set, 13));
            Assert.IsTrue(UnsafeOrderedSet.Contains<int>(set, 51));

            Assert.IsFalse(UnsafeOrderedSet.Contains<int>(set, 14));

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void RemoveTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(10);

            Assert.IsFalse(UnsafeOrderedSet.Remove<int>(set, 1));

            UnsafeOrderedSet.Add(set, 1);
            UnsafeOrderedSet.Add(set, 7);
            UnsafeOrderedSet.Add(set, 51);
            UnsafeOrderedSet.Add(set, 13);

            Assert.IsFalse(UnsafeOrderedSet.Remove<int>(set, 3));

            Assert.IsTrue(UnsafeOrderedSet.Remove<int>(set, 1));
            Assert.IsTrue(UnsafeOrderedSet.Remove<int>(set, 7));
            Assert.IsTrue(UnsafeOrderedSet.Remove<int>(set, 13));
            Assert.IsTrue(UnsafeOrderedSet.Remove<int>(set, 51));

            Assert.IsFalse(UnsafeOrderedSet.Remove<int>(set, 13));

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void AddRandomTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(10);

            Random r = new Random();
            for (int i = 0; i < 10; i++)
                UnsafeOrderedSet.Add<int>(set, r.Next());

            int* arr = stackalloc int[10];
            UnsafeOrderedSet.CopyTo<int>(set, arr, 0);

            // Validate values are from low to high
            int last = arr[0];
            for (int i = 1; i < 10; i++)
            {
                Assert.IsTrue(last <= arr[i]);
                last = arr[i++];
            }

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void CopyToTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
                UnsafeOrderedSet.Add(set, i);

            var count = UnsafeOrderedSet.GetCount(set);
            int* arr = stackalloc int[count];

            UnsafeOrderedSet.CopyTo<int>(set, arr, 0);

            // Check
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++]);

            UnsafeOrderedSet.Free(set);
        }

        [Test]
        public void ClearTest()
        {
            var set = UnsafeOrderedSet.Allocate<int>(16, fixedSize: false);

            // Add some random data
            Random r = new Random();
            for (int i = 0; i < 10; i++)
                UnsafeOrderedSet.Add<int>(set, r.Next());

            // Verify data has been added
            Assert.AreEqual(10, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeOrderedSet.GetCapacity(set));

            // Clear set and verify it's cleared
            UnsafeOrderedSet.Clear(set);

            Assert.AreEqual(0, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeOrderedSet.GetCapacity(set));


            // Validate we can still add data and have it be valid
            // Add data to cleared set
            for (int i = 10; i >= 0; i--)
                UnsafeOrderedSet.Add(set, i);

            var count = UnsafeOrderedSet.GetCount(set);
            int* arr = stackalloc int[count];

            UnsafeOrderedSet.CopyTo<int>(set, arr, 0);

            // Validate data has been written
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++]);

            Assert.AreEqual(count, UnsafeOrderedSet.GetCount(set));
            Assert.AreEqual(16, UnsafeOrderedSet.GetCapacity(set));

            UnsafeOrderedSet.Free(set);
        }
    }
}
