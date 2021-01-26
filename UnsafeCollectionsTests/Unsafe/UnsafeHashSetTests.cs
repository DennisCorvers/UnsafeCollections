using NUnit.Framework;
using System;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeHashSetTests
    {
        [Test]
        public void FreeFixedSet()
        {
            var s = UnsafeHashSet.Allocate<int>(2, true);
            UnsafeHashSet.Free(s);
        }

        [Test]
        public void FreeDynamicSet()
        {
            var s = UnsafeHashSet.Allocate<int>(2, false);
            UnsafeHashSet.Free(s);
        }

        [Test]
        public void ClearHashSet()
        {
            var set = UnsafeHashSet.Allocate<int>(3);
            UnsafeHashSet.Add(set, 1);
            UnsafeHashSet.Add(set, 2);
            UnsafeHashSet.Add(set, 3);

            Assert.IsTrue(UnsafeHashSet.Contains(set, 2));
            Assert.AreEqual(3, UnsafeHashSet.GetCount(set));

            UnsafeHashSet.Add(set, 4);
            Assert.AreEqual(4, UnsafeHashSet.GetCount(set));

            UnsafeHashSet.Clear(set);
            Assert.AreEqual(0, UnsafeHashSet.GetCount(set));
            Assert.IsFalse(UnsafeHashSet.Contains(set, 2));

            UnsafeHashSet.Add(set, 4);
            Assert.AreEqual(1, UnsafeHashSet.GetCount(set));
            Assert.IsTrue(UnsafeHashSet.Contains(set, 4));

            UnsafeHashSet.Clear(set);
            Assert.AreEqual(0, UnsafeHashSet.GetCount(set));

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void ConstructorTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeHashSet.GetCount(set));
            // Next expected prime is 17
            Assert.AreEqual(17, UnsafeHashSet.GetCapacity(set));

            UnsafeHashSet.Free(set);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeHashSet.Add<float>(set, 4); });

            UnsafeHashSet.Free(set);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            // Fill set
            for (int i = 0; i < 10; i++)
                UnsafeHashSet.Add(set, i);

            var enumerator = UnsafeHashSet.GetEnumerator<int>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.Current);
            }

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void ExpandFailedTest()
        {
            var initialCapacity = 7;
            var set = UnsafeHashSet.Allocate<int>(initialCapacity, true);

            // Valid adds
            for (int i = 0; i < initialCapacity; i++)
                UnsafeHashSet.Add(set, i + 1);

            Assert.AreEqual(initialCapacity, UnsafeHashSet.GetCount(set));
            Assert.AreEqual(initialCapacity, UnsafeHashSet.GetCapacity(set));

            Assert.Throws<InvalidOperationException>(() =>
            {
                UnsafeHashSet.Add(set, 42);
            });

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void ExpandTest()
        {
            var initialCapacity = 7;
            var set = UnsafeHashSet.Allocate<int>(initialCapacity, fixedSize: false);

            // Valid adds
            for (int i = 0; i < initialCapacity; i++)
                UnsafeHashSet.Add(set, i + 1);

            Assert.AreEqual(initialCapacity, UnsafeHashSet.GetCount(set));
            Assert.AreEqual(initialCapacity, UnsafeHashSet.GetCapacity(set));

            UnsafeHashSet.Add(set, 42);
            UnsafeHashSet.Add(set, 18);

            var nextCapacity = UnsafeHashCollection.GetNextPrime(initialCapacity + 1);
            Assert.AreEqual(9, UnsafeHashSet.GetCount(set));
            Assert.AreEqual(nextCapacity, UnsafeHashSet.GetCapacity(set));

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void ContainsTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            Assert.IsFalse(UnsafeHashSet.Contains<int>(set, 1));

            UnsafeHashSet.Add(set, 1);
            UnsafeHashSet.Add(set, 7);
            UnsafeHashSet.Add(set, 51);
            UnsafeHashSet.Add(set, 13);

            Assert.IsFalse(UnsafeHashSet.Contains<int>(set, 3));

            Assert.IsTrue(UnsafeHashSet.Contains<int>(set, 1));
            Assert.IsTrue(UnsafeHashSet.Contains<int>(set, 7));
            Assert.IsTrue(UnsafeHashSet.Contains<int>(set, 13));
            Assert.IsTrue(UnsafeHashSet.Contains<int>(set, 51));

            Assert.IsFalse(UnsafeHashSet.Contains<int>(set, 14));

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void RemoveTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            Assert.IsFalse(UnsafeHashSet.Remove<int>(set, 1));

            UnsafeHashSet.Add(set, 1);
            UnsafeHashSet.Add(set, 7);
            UnsafeHashSet.Add(set, 51);
            UnsafeHashSet.Add(set, 13);

            Assert.IsFalse(UnsafeHashSet.Remove<int>(set, 3));

            Assert.IsTrue(UnsafeHashSet.Remove<int>(set, 1));
            Assert.IsTrue(UnsafeHashSet.Remove<int>(set, 7));
            Assert.IsTrue(UnsafeHashSet.Remove<int>(set, 13));
            Assert.IsTrue(UnsafeHashSet.Remove<int>(set, 51));

            Assert.IsFalse(UnsafeHashSet.Remove<int>(set, 13));

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void AddTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeHashSet.Add<int>(set, i * i * i);

            int* arr = stackalloc int[10];
            UnsafeHashSet.CopyTo<int>(set, arr, 0);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i * i * i, arr[i]);

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void AddDuplicateTest()
        {
            var set = UnsafeHashSet.Allocate<int>(3);

            Assert.IsTrue(UnsafeHashSet.Add(set, 5));
            Assert.IsTrue(UnsafeHashSet.Add(set, 8));
            Assert.IsTrue(UnsafeHashSet.Add(set, 9));
            Assert.IsFalse(UnsafeHashSet.Add(set, 5));

            Assert.AreEqual(3, UnsafeHashSet.GetCapacity(set));

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void AddHashCollisionTest()
        {
            // Tests linked-list functionality when hash collisions occur.
            var set = UnsafeHashSet.Allocate<DuplicateKey>(3);

            Assert.IsTrue(UnsafeHashSet.Add(set, new DuplicateKey(1)));
            Assert.IsTrue(UnsafeHashSet.Add(set, new DuplicateKey(2)));
            Assert.IsTrue(UnsafeHashSet.Add(set, new DuplicateKey(3)));
            Assert.IsFalse(UnsafeHashSet.Add(set, new DuplicateKey(1)));

            Assert.IsTrue(UnsafeHashSet.Contains(set, new DuplicateKey(2)));

            Assert.AreEqual(3, UnsafeHashSet.GetCapacity(set));

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void CopyHashCollisionTest()
        {
            // Tests linked-list functionality when hash collisions occur.
            var set = UnsafeHashSet.Allocate<DuplicateKey>(3);

            Assert.IsTrue(UnsafeHashSet.Add(set, new DuplicateKey(1)));
            Assert.IsTrue(UnsafeHashSet.Add(set, new DuplicateKey(2)));
            Assert.IsTrue(UnsafeHashSet.Add(set, new DuplicateKey(3)));

            var arr = stackalloc DuplicateKey[3];
            UnsafeHashSet.CopyTo<DuplicateKey>(set, arr, 0);

            for (int i = 0; i < 3; i++)
                Assert.AreEqual(new DuplicateKey(i + 1), arr[i]);

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void CopyToTest()
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            // Fill set
            for (int i = 0; i < 10; i++)
                UnsafeHashSet.Add(set, i);

            var count = UnsafeHashSet.GetCount(set);
            int* arr = stackalloc int[count];

            UnsafeHashSet.CopyTo<int>(set, arr, 0);

            // Check
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++]);

            UnsafeHashSet.Free(set);
        }

        [Test]
        public void IntersectsWithTest()
        {
            var setEven = GetOddEvenSet(false);
            var setOdd = GetOddEvenSet(true);

            UnsafeHashSet.IntersectsWith<int>(setEven, setOdd);

            // Resulting collection should be empty.
            Assert.AreEqual(0, UnsafeHashSet.GetCount(setEven));

            UnsafeHashSet.Free(setEven);
            UnsafeHashSet.Free(setOdd);
        }

        [Test]
        public void UnionWithTest()
        {
            var setEven = GetOddEvenSet(false);
            var setOdd = GetOddEvenSet(true);

            UnsafeHashSet.UnionWith<int>(setEven, setOdd);

            // Resulting collection should contain both sets
            Assert.AreEqual(10, UnsafeHashSet.GetCount(setEven));

            UnsafeHashSet.Free(setEven);
            UnsafeHashSet.Free(setOdd);
        }

        [Test]
        public void ExceptWithTest()
        {
            var setEven = GetOddEvenSet(false);
            var setOdd = GetOddEvenSet(true);

            UnsafeHashSet.ExceptWith<int>(setEven, setOdd);

            // Resulting collection should only contain Even
            Assert.AreEqual(5, UnsafeHashSet.GetCount(setEven));

            UnsafeHashSet.Free(setEven);
            UnsafeHashSet.Free(setOdd);
        }

        [Test]
        public void SymmetricExceptTest()
        {
            var setEven = GetOddEvenSet(false);
            var setOdd = GetOddEvenSet(true);

            UnsafeHashSet.SymmetricExcept<int>(setEven, setOdd);

            // Resulting collection should contain both (XOr)
            Assert.AreEqual(10, UnsafeHashSet.GetCount(setEven));

            UnsafeHashSet.Free(setEven);
            UnsafeHashSet.Free(setOdd);
        }

        private UnsafeHashSet* GetOddEvenSet(bool isOdd = false)
        {
            var set = UnsafeHashSet.Allocate<int>(10);

            int num = isOdd ? 1 : 0;
            for (int i = 0; i < 5; i++)
            {
                UnsafeHashSet.Add(set, num);
                num += 2;
            }

            return set;
        }
    }

    internal struct DuplicateKey : IEquatable<DuplicateKey>
    {
        private decimal m_value;

        public DuplicateKey(decimal value)
        {
            m_value = value;
        }

        public bool Equals(DuplicateKey other)
        {
            return m_value == other.m_value;
        }

        public override int GetHashCode()
        {
            return 1;
        }
    }
}
