using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeOrderedDictionaryTests
    {
        [Test]
        public void ConstructorTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, bool>(10);

            Assert.AreEqual(0, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(10, UnsafeSortedDictionary.GetCapacity(set));

            UnsafeSortedDictionary.Free(set);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, bool>(10);

            // Test key
            Assert.Catch<AssertException>(() =>
            {
                UnsafeSortedDictionary.Add<float, bool>(set, 4, true);
            });

            // Test value
            Assert.Catch<AssertException>(() =>
            {
                UnsafeSortedDictionary.Add<int, float>(set, 4, 4.1f);
            });

            UnsafeSortedDictionary.Free(set);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
            {
                // Add in reverse order
                UnsafeSortedDictionary.Add<int, decimal>(set, i, i * i);
            }

            var enumerator = UnsafeSortedDictionary.GetEnumerator<int, decimal>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.CurrentKey);
                Assert.AreEqual(i * i, enumerator.CurrentValue);

                Assert.AreEqual(i, enumerator.Current.Key);
                Assert.AreEqual(i * i, enumerator.Current.Value);
            }

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void KeyIteratorTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
            {
                // Add in reverse order
                UnsafeSortedDictionary.Add<int, decimal>(set, i, i * i);
            }

            var enumerator = UnsafeSortedDictionary.GetKeyEnumerator<int>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.Current);
            }

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void ValueIteratorTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
            {
                // Add in reverse order
                UnsafeSortedDictionary.Add<int, decimal>(set, i, i * i);
            }

            var enumerator = UnsafeSortedDictionary.GetValueEnumerator<decimal>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i * i, enumerator.Current);
            }

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void ExpandFailedTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, float>(8, true);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeSortedDictionary.Add<int, float>(set, i + 1, i * i);

            Assert.AreEqual(8, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(8, UnsafeSortedDictionary.GetCapacity(set));

            Assert.Throws<InvalidOperationException>(() =>
            {
                UnsafeSortedDictionary.Add<int, float>(set, 42, 13.3f);
            });

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void ExpandTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, short>(8, fixedSize: false);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeSortedDictionary.Add<int, short>(set, i + 1, 2);

            Assert.AreEqual(8, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(8, UnsafeSortedDictionary.GetCapacity(set));

            UnsafeSortedDictionary.Add<int, short>(set, 42, 12);

            Assert.AreEqual(9, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedDictionary.GetCapacity(set));

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void ContainsTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeSortedDictionary.Contains<int>(set, 1));

            UnsafeSortedDictionary.Add(set, 1, 3);
            UnsafeSortedDictionary.Add(set, 7, 3);
            UnsafeSortedDictionary.Add(set, 51, 3);
            UnsafeSortedDictionary.Add(set, 13, 3);

            Assert.IsFalse(UnsafeSortedDictionary.Contains<int>(set, 3));

            Assert.IsTrue(UnsafeSortedDictionary.Contains<int>(set, 1));
            Assert.IsTrue(UnsafeSortedDictionary.Contains<int>(set, 7));
            Assert.IsTrue(UnsafeSortedDictionary.Contains<int>(set, 13));
            Assert.IsTrue(UnsafeSortedDictionary.Contains<int>(set, 51));

            Assert.IsFalse(UnsafeSortedDictionary.Contains<int>(set, 14));

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void RemoveTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeSortedDictionary.Remove<int>(set, 1));

            UnsafeSortedDictionary.Add(set, 1, 3);
            UnsafeSortedDictionary.Add(set, 7, 3);
            UnsafeSortedDictionary.Add(set, 51, 3);
            UnsafeSortedDictionary.Add(set, 13, 3);

            Assert.IsFalse(UnsafeSortedDictionary.Remove<int>(set, 3));

            Assert.IsTrue(UnsafeSortedDictionary.Remove<int>(set, 1));
            Assert.IsTrue(UnsafeSortedDictionary.Remove<int>(set, 7));
            Assert.IsTrue(UnsafeSortedDictionary.Remove<int>(set, 13));
            Assert.IsTrue(UnsafeSortedDictionary.Remove<int>(set, 51));

            Assert.IsFalse(UnsafeSortedDictionary.Remove<int>(set, 13));

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void AddRandomTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, double>(10);

            Random r = new Random();
            for (int i = 0; i < 10; i++)
                UnsafeSortedDictionary.Add(set, r.Next(), r.NextDouble());

            var arr = new KeyValuePair<int, double>[10];
            UnsafeSortedDictionary.CopyTo(set, arr, 0);

            // Validate values are from low to high
            var last = arr[0];
            for (int i = 1; i < 10; i++)
            {
                Assert.IsTrue(last.Key <= arr[i].Key);
                last = arr[i++];
            }

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void TryGetValueTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeSortedDictionary.TryGetValue(set, 1, out int value));

            UnsafeSortedDictionary.Add(set, 1, 1);
            UnsafeSortedDictionary.Add(set, 7, 2);
            UnsafeSortedDictionary.Add(set, 51, 3);
            UnsafeSortedDictionary.Add(set, 13, 4);

            Assert.IsFalse(UnsafeSortedDictionary.TryGetValue(set, 3, out value));

            Assert.IsTrue(UnsafeSortedDictionary.TryGetValue(set, 1, out value));
            Assert.AreEqual(1, value);
            Assert.IsTrue(UnsafeSortedDictionary.TryGetValue(set, 7, out value));
            Assert.AreEqual(2, value);
            Assert.IsTrue(UnsafeSortedDictionary.TryGetValue(set, 13, out value));
            Assert.AreEqual(4, value);
            Assert.IsTrue(UnsafeSortedDictionary.TryGetValue(set, 51, out value));
            Assert.AreEqual(3, value);

            Assert.IsFalse(UnsafeSortedDictionary.TryGetValue(set, 14, out value));

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void GetterTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, int>(4);

            UnsafeSortedDictionary.Add(set, 1, 1);
            UnsafeSortedDictionary.Add(set, 7, 2);
            UnsafeSortedDictionary.Add(set, 51, 3);
            UnsafeSortedDictionary.Add(set, 13, 4);

            // Get non-existent key
            Assert.Throws<ArgumentException>(() =>
            {
                UnsafeSortedDictionary.Get<int, int>(set, 2);
            });

            // Get existing key
            var value = UnsafeSortedDictionary.Get<int, int>(set, 51);
            Assert.AreEqual(3, value);
        }

        [Test]
        public void SetterTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, int>(4);

            UnsafeSortedDictionary.Add(set, 1, 1);
            UnsafeSortedDictionary.Add(set, 7, 2);

            // Add new key
            UnsafeSortedDictionary.Set(set, 2, 412);
            Assert.IsTrue(UnsafeSortedDictionary.TryGetValue<int, int>(set, 2, out int valNew));
            Assert.AreEqual(412, valNew);

            // Overwrite existing key
            UnsafeSortedDictionary.Set(set, 1, 333);
            Assert.IsTrue(UnsafeSortedDictionary.TryGetValue<int, int>(set, 1, out int valExist));
            Assert.AreEqual(333, valExist);
        }

        [Test]
        public void CopyToTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, bool>(10);

            // Fill set
            for (int i = 10; i >= 0; i--)
                UnsafeSortedDictionary.Add(set, i, i % 2 == 0);

            var count = UnsafeSortedDictionary.GetCount(set);
            var arr = new KeyValuePair<int, bool>[count];

            UnsafeSortedDictionary.CopyTo(set, arr, 0);

            // Check
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++].Key);

            UnsafeSortedDictionary.Free(set);
        }

        [Test]
        public void ClearTest()
        {
            var set = UnsafeSortedDictionary.Allocate<int, float>(16, fixedSize: false);

            // Add some random data
            Random r = new Random();
            for (int i = 0; i < 10; i++)
                UnsafeSortedDictionary.Add<int, float>(set, r.Next(), (float)r.NextDouble());

            // Verify data has been added
            Assert.AreEqual(10, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedDictionary.GetCapacity(set));

            // Clear set and verify it's cleared
            UnsafeSortedDictionary.Clear(set);

            Assert.AreEqual(0, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedDictionary.GetCapacity(set));


            // Validate we can still add data and have it be valid
            // Add data to cleared set
            for (int i = 10; i >= 0; i--)
                UnsafeSortedDictionary.Add(set, i, 41f);

            var count = UnsafeSortedDictionary.GetCount(set);
            var arr = new KeyValuePair<int, float>[count];

            UnsafeSortedDictionary.CopyTo(set, arr, 0);

            // Validate data has been written
            int num = 0;
            for (int i = 0; i < count; i++)
                Assert.AreEqual(i, arr[num++].Key);

            Assert.AreEqual(count, UnsafeSortedDictionary.GetCount(set));
            Assert.AreEqual(16, UnsafeSortedDictionary.GetCapacity(set));

            UnsafeSortedDictionary.Free(set);
        }
    }
}
