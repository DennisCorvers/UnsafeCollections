using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeDictionaryTests
    {
        private UnsafeDictionary* Dictionary(params int[] values)
        {
            var map = UnsafeDictionary.Allocate<int, int>(values.Length * 2);

            for (int i = 0; i < values.Length; ++i)
            {
                UnsafeDictionary.Add(map, i, values[i]);
            }

            return map;
        }

        [Test]
        public void FreeFixedDictionary()
        {
            var s = UnsafeDictionary.Allocate<int, int>(2, true);
            UnsafeDictionary.Free(s);
        }

        [Test]
        public void FreeDynamicDictionary()
        {
            var s = UnsafeDictionary.Allocate<int, byte>(2, false);
            UnsafeDictionary.Free(s);
        }

        [Test]
        public void ClearDictionary()
        {
            var map = UnsafeDictionary.Allocate<int, int>(16, fixedSize: false);

            for (int i = 0; i < 3; i++)
                UnsafeDictionary.Add(map, i, 3);

            Assert.IsTrue(UnsafeDictionary.ContainsKey(map, 2));
            Assert.AreEqual(3, UnsafeDictionary.GetCount(map));
            UnsafeDictionary.TryGetValue(map, 2, out int result);
            Assert.AreEqual(3, result);

            UnsafeDictionary.Add(map, 3, 1);
            Assert.AreEqual(4, UnsafeDictionary.GetCount(map));

            UnsafeDictionary.Clear(map);
            Assert.AreEqual(0, UnsafeDictionary.GetCount(map));
            Assert.IsFalse(UnsafeDictionary.ContainsKey(map, 2));

            UnsafeDictionary.Add(map, 3, 10);
            Assert.AreEqual(1, UnsafeDictionary.GetCount(map));
            Assert.IsTrue(UnsafeDictionary.ContainsKey(map, 3));
            UnsafeDictionary.TryGetValue(map, 3, out int result2);
            Assert.AreEqual(10, result2);

            UnsafeDictionary.Clear(map);
            Assert.AreEqual(0, UnsafeDictionary.GetCount(map));

            UnsafeDictionary.Free(map);
        }

        [Test]
        public void DuplicateKeyTest()
        {
            var dict = UnsafeDictionary.Allocate<int, long>(8);

            UnsafeDictionary.Add<int, long>(dict, 1, 10);

            Assert.Catch<ArgumentException>(() =>
            {
                UnsafeDictionary.Add<int, long>(dict, 1, 10);
            });
        }

        [Test]
        public void ConstructorTest()
        {
            var count = UnsafeHashCollection.GetNextPrime(10);
            var set = UnsafeDictionary.Allocate<int, bool>(10);

            Assert.AreEqual(0, UnsafeDictionary.GetCount(set));
            Assert.AreEqual(count, UnsafeDictionary.GetCapacity(set));

            UnsafeDictionary.Free(set);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var set = UnsafeDictionary.Allocate<int, bool>(10);

            // Test key
            Assert.Catch<AssertException>(() =>
            {
                UnsafeDictionary.Add<float, bool>(set, 4, true);
            });

            // Test value
            Assert.Catch<AssertException>(() =>
            {
                UnsafeDictionary.Add<int, float>(set, 4, 4.1f);
            });

            UnsafeDictionary.Free(set);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var set = UnsafeDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 0; i < 10; i++)
            {
                // Add in reverse order
                UnsafeDictionary.Add<int, decimal>(set, i, i * i);
            }

            var enumerator = UnsafeDictionary.GetEnumerator<int, decimal>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.CurrentKey);
                Assert.AreEqual(i * i, enumerator.CurrentValue);

                Assert.AreEqual(i, enumerator.Current.Key);
                Assert.AreEqual(i * i, enumerator.Current.Value);
            }

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void KeyIteratorTest()
        {
            var set = UnsafeDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 0; i < 10; i++)
            {
                // Add in reverse order
                UnsafeDictionary.Add<int, decimal>(set, i, i * i);
            }

            var enumerator = UnsafeDictionary.GetKeyEnumerator<int>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i, enumerator.Current);
            }

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void ValueIteratorTest()
        {
            var set = UnsafeDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 0; i < 10; i++)
            {
                // Add in reverse order
                UnsafeDictionary.Add<int, decimal>(set, i, i * i);
            }

            var enumerator = UnsafeDictionary.GetValueEnumerator<decimal>(set);

            for (int i = 0; i < 10; i++)
            {
                enumerator.MoveNext();
                Assert.AreEqual(i * i, enumerator.Current);
            }

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void TryAddTest()
        {
            var set = UnsafeDictionary.Allocate<int, float>(8, true);

            Assert.IsTrue(UnsafeDictionary.TryAdd<int, float>(set, 5, 10));
            Assert.IsTrue(UnsafeDictionary.TryAdd<int, float>(set, 2, 18));
            Assert.IsTrue(UnsafeDictionary.TryAdd<int, float>(set, 1, 1));
            Assert.IsFalse(UnsafeDictionary.TryAdd<int, float>(set, 2, 1));
        }

        [Test]
        public void ExpandFailedTest()
        {
            var count = UnsafeHashCollection.GetNextPrime(7);
            var set = UnsafeDictionary.Allocate<int, float>(7, true);

            // Valid adds
            for (int i = 0; i < 7; i++)
                UnsafeDictionary.Add<int, float>(set, i + 1, i * i);

            Assert.AreEqual(7, UnsafeDictionary.GetCount(set));
            Assert.AreEqual(7, UnsafeDictionary.GetCapacity(set));

            Assert.Throws<InvalidOperationException>(() =>
            {
                UnsafeDictionary.Add<int, float>(set, 42, 13.3f);
            });

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void ExpandTest()
        {
            int count = UnsafeHashCollection.GetNextPrime(8);
            var set = UnsafeDictionary.Allocate<int, short>(count, fixedSize: false);

            // Valid adds
            for (int i = 0; i < 8; i++)
                UnsafeDictionary.Add<int, short>(set, i + 1, 2);

            Assert.AreEqual(8, UnsafeDictionary.GetCount(set));
            Assert.AreEqual(count, UnsafeDictionary.GetCapacity(set));

            UnsafeDictionary.Add<int, short>(set, 42, 12);

            Assert.AreEqual(9, UnsafeDictionary.GetCount(set));
            Assert.AreEqual(count, UnsafeDictionary.GetCapacity(set));

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void ContainsTest()
        {
            var set = UnsafeDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeDictionary.ContainsKey<int>(set, 1));

            UnsafeDictionary.Add(set, 1, 3);
            UnsafeDictionary.Add(set, 7, 3);
            UnsafeDictionary.Add(set, 51, 3);
            UnsafeDictionary.Add(set, 13, 3);

            Assert.IsFalse(UnsafeDictionary.ContainsKey<int>(set, 3));

            Assert.IsTrue(UnsafeDictionary.ContainsKey<int>(set, 1));
            Assert.IsTrue(UnsafeDictionary.ContainsKey<int>(set, 7));
            Assert.IsTrue(UnsafeDictionary.ContainsKey<int>(set, 13));
            Assert.IsTrue(UnsafeDictionary.ContainsKey<int>(set, 51));

            Assert.IsFalse(UnsafeDictionary.ContainsKey<int>(set, 14));

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void ContainsValueTest()
        {
            var set = UnsafeDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeDictionary.ContainsKey<int>(set, 1));

            UnsafeDictionary.Add(set, 1, 5);
            UnsafeDictionary.Add(set, 2, 2);
            UnsafeDictionary.Add(set, 3, 38);

            Assert.IsTrue(UnsafeDictionary.ContainsValue<int>(set, 2));
            Assert.IsTrue(UnsafeDictionary.ContainsValue<int>(set, 38));
            Assert.IsFalse(UnsafeDictionary.ContainsValue<int>(set, 1));

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void RemoveTest()
        {
            var set = UnsafeDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeDictionary.Remove<int>(set, 1));

            UnsafeDictionary.Add(set, 1, 3);
            UnsafeDictionary.Add(set, 7, 3);
            UnsafeDictionary.Add(set, 51, 3);
            UnsafeDictionary.Add(set, 13, 3);

            Assert.IsFalse(UnsafeDictionary.Remove<int>(set, 3));

            Assert.IsTrue(UnsafeDictionary.Remove<int>(set, 1));
            Assert.IsTrue(UnsafeDictionary.Remove<int>(set, 7));
            Assert.IsTrue(UnsafeDictionary.Remove<int>(set, 13));
            Assert.IsTrue(UnsafeDictionary.Remove<int>(set, 51));

            Assert.IsFalse(UnsafeDictionary.Remove<int>(set, 13));

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void TryGetValueTest()
        {
            var set = UnsafeDictionary.Allocate<int, int>(10);

            Assert.IsFalse(UnsafeDictionary.TryGetValue(set, 1, out int value));

            UnsafeDictionary.Add(set, 1, 1);
            UnsafeDictionary.Add(set, 7, 2);
            UnsafeDictionary.Add(set, 51, 3);
            UnsafeDictionary.Add(set, 13, 4);

            Assert.IsFalse(UnsafeDictionary.TryGetValue(set, 3, out value));

            Assert.IsTrue(UnsafeDictionary.TryGetValue(set, 1, out value));
            Assert.AreEqual(1, value);
            Assert.IsTrue(UnsafeDictionary.TryGetValue(set, 7, out value));
            Assert.AreEqual(2, value);
            Assert.IsTrue(UnsafeDictionary.TryGetValue(set, 13, out value));
            Assert.AreEqual(4, value);
            Assert.IsTrue(UnsafeDictionary.TryGetValue(set, 51, out value));
            Assert.AreEqual(3, value);

            Assert.IsFalse(UnsafeDictionary.TryGetValue(set, 14, out value));

            UnsafeDictionary.Free(set);
        }

        [Test]
        public void GetterTest()
        {
            var set = UnsafeDictionary.Allocate<int, int>(4);

            UnsafeDictionary.Add(set, 1, 1);
            UnsafeDictionary.Add(set, 7, 2);
            UnsafeDictionary.Add(set, 51, 3);
            UnsafeDictionary.Add(set, 13, 4);

            // Get non-existent key
            Assert.Throws<ArgumentException>(() =>
            {
                UnsafeDictionary.Get<int, int>(set, 2);
            });

            // Get existing key
            var value = UnsafeDictionary.Get<int, int>(set, 51);
            Assert.AreEqual(3, value);
        }

        [Test]
        public void SetterTest()
        {
            var set = UnsafeDictionary.Allocate<int, int>(4);

            UnsafeDictionary.Add(set, 1, 1);
            UnsafeDictionary.Add(set, 7, 2);

            // Add new key
            UnsafeDictionary.Set(set, 2, 412);
            Assert.IsTrue(UnsafeDictionary.TryGetValue<int, int>(set, 2, out int valNew));
            Assert.AreEqual(412, valNew);

            // Overwrite existing key
            UnsafeDictionary.Set(set, 1, 333);
            Assert.IsTrue(UnsafeDictionary.TryGetValue<int, int>(set, 1, out int valExist));
            Assert.AreEqual(333, valExist);
        }

        [Test]
        public void CopyToTest()
        {
            var set = UnsafeDictionary.Allocate<int, decimal>(10);

            // Fill set
            for (int i = 0; i < 10; i++)
            {
                UnsafeDictionary.Add<int, decimal>(set, i, i * i * i);
            }

            var count = UnsafeDictionary.GetCount(set);
            var arr = new KeyValuePair<int, decimal>[count];

            UnsafeDictionary.CopyTo(set, arr, 0);

            // Check
            int num = 0;
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(i, arr[num].Key);
                Assert.AreEqual(i * i * i, arr[num++].Value);
            }

            UnsafeDictionary.Free(set);
        }
    }
}
