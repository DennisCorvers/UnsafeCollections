using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollectionsTests.Native
{
    public class NativeListTests
    {
        [Test]
        public void AddRangeTest()
        {
            NativeList<int> list = new NativeList<int>(5);
            for (int i = 0; i < 5; i++)
                list.Add(i);

            List<int> list2 = new List<int>();

            for (int i = 5; i < 10; i++)
                list2.Add(i);

            list.AddRange(list2);

            Assert.AreEqual(10, list.Capacity);
            Assert.AreEqual(10, list.Count);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, list[i]);

            list.Dispose();
        }

        [Test]
        public void IndexOfTest()
        {
            NativeList<int> list = new NativeList<int>(10);

            for (int i = 0; i < 10; i++)
                list.Add(i);

            Assert.AreEqual(7, list.IndexOf(7));

            list.Dispose();
        }

        [Test]
        public void ContainsExplicitTest()
        {
            ICollection<int> list = new NativeList<int>(10);

            for (int i = 0; i < 10; i++)
                list.Add(i);

            Assert.IsTrue(list.Contains(7));
            Assert.IsFalse(list.Contains(11));

            ((NativeList<int>)list).Dispose();
        }
    }
}
