using NUnit.Framework;
using System;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeHashDictionaryTests
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
        public void ClearHashDictionary()
        {
            var map = Dictionary(1, 2, 3);
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
        public void DictionaryIteratorTest()
        {
            var map = Dictionary(0, 10, 20, 30, 40);

            int count = 0;
            foreach (var keypair in UnsafeDictionary.GetEnumerator<int, int>(map))
            {
                Assert.AreEqual(count * 10, keypair.Value);
                Assert.AreEqual(count, keypair.Key);
                count++;
            }
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
        public void ExpandFailTest()
        {
            var dict = UnsafeDictionary.Allocate<int, long>(3, true);

            Assert.AreEqual(3, UnsafeDictionary.GetCapacity(dict));

            UnsafeDictionary.Add<int, long>(dict, 1, 10);
            UnsafeDictionary.Add<int, long>(dict, 2, 20);
            UnsafeDictionary.Add<int, long>(dict, 3, 30);

            Assert.AreEqual(3, UnsafeDictionary.GetCount(dict));

            Assert.Catch<InvalidOperationException>(() =>
            {
                UnsafeDictionary.Add<int, long>(dict, 4, 40);
            });

            Assert.AreEqual(3, UnsafeDictionary.GetCount(dict));
        }
    }
}
