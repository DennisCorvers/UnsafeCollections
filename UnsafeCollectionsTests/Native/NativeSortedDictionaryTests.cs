using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollectionsTests.Native
{
    public class NativeSortedDictionaryTests
    {
        [Test]
        public void ValueCollectionTest()
        {
            var dict = PrepareDictionary();

            var values = dict.Values;

            Assert.AreEqual(4, values.Count);
            var itr = values.GetEnumerator();

            // Check values in expected order.
            itr.MoveNext();
            Assert.AreEqual(42, itr.Current);

            itr.MoveNext();
            Assert.AreEqual(254, itr.Current);

            itr.MoveNext();
            Assert.AreEqual(999, itr.Current);

            itr.MoveNext();
            Assert.AreEqual(11, itr.Current);

        }

        [Test]
        public void ValuesCopyToTest()
        {
            var dict = PrepareDictionary();
            var values = dict.Values;

            var arr = new double[4];
            values.CopyTo(arr, 0);

            Assert.AreEqual(42, arr[0]);
            Assert.AreEqual(254, arr[1]);
            Assert.AreEqual(999, arr[2]);
            Assert.AreEqual(11, arr[3]);
        }

        [Test]
        public void KeyCollectionTest()
        {
            var dict = PrepareDictionary();

            var keys = dict.Keys;

            Assert.AreEqual(4, keys.Count);

            var itr = keys.GetEnumerator();

            // Check values in expected order.
            itr.MoveNext();
            Assert.AreEqual(-20, itr.Current);

            itr.MoveNext();
            Assert.AreEqual(1, itr.Current);

            itr.MoveNext();
            Assert.AreEqual(3, itr.Current);

            itr.MoveNext();
            Assert.AreEqual(5, itr.Current);
        }

        [Test]
        public void KeysCopyToTest()
        {
            var dict = PrepareDictionary();
            var keys = dict.Keys;

            var arr = new int[4];
            keys.CopyTo(arr, 0);

            Assert.AreEqual(-20, arr[0]);
            Assert.AreEqual(1, arr[1]);
            Assert.AreEqual(3, arr[2]);
            Assert.AreEqual(5, arr[3]);
        }


        private NativeSortedDictionary<int, double> PrepareDictionary()
        {
            var dict = new NativeSortedDictionary<int, double>(8);

            dict.Add(1, 254);   // Index 2
            dict.Add(5, 11);    // Index 4
            dict.Add(3, 999);   // Index 3
            dict.Add(-20, 42);  // Index 1

            return dict;
        }
    }
}
