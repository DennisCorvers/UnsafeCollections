using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeRingBufferTests
    {
        private static void SplitRingBuffer(UnsafeRingBuffer* q)
        {
            //Wrap tail back to 0
            for (int i = 0; i < 5; i++)
                UnsafeRingBuffer.Push(q, 111);

            //First half
            for (int i = 0; i < 5; i++)
                UnsafeRingBuffer.Push(q, i);

            //Move head by 5
            for (int i = 0; i < 5; i++)
                UnsafeRingBuffer.Pop<int>(q, out _);

            //Second half (head and tail are now both 5)
            for (int i = 5; i < 10; i++)
                UnsafeRingBuffer.Push(q, i);

            //Circular buffer now "ends" in the middle of the underlying array
        }


        [Test]
        public void ConstructorTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeRingBuffer.GetCount(q));
            Assert.AreEqual(10, UnsafeRingBuffer.GetCapacity(q));

            UnsafeRingBuffer.Free(q);
        }

        [Test]
        public void PushTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeRingBuffer.Push(q, i * i);
            }

            Assert.AreEqual(10, UnsafeRingBuffer.GetCount(q));
            Assert.AreEqual(10, UnsafeRingBuffer.GetCapacity(q));

            UnsafeRingBuffer.Clear(q);

            Assert.AreEqual(0, UnsafeRingBuffer.GetCount(q));
            Assert.AreEqual(10, UnsafeRingBuffer.GetCapacity(q));

            UnsafeRingBuffer.Free(q);
        }

        [Test]
        public void DequeueTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeRingBuffer.Push(q, i * i);


            for (int i = 0; i < 10; i++)
            {
                UnsafeRingBuffer.Pop<int>(q, out int num);
                Assert.AreEqual(i * i, num);
            }

            UnsafeRingBuffer.Free(q);
        }

        [Test]
        public void PeekTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeRingBuffer.Push(q, (int)Math.Pow(i + 2, 2));

            for (int i = 0; i < 10; i++)
            {
                UnsafeRingBuffer.Peek<int>(q, out int num);
                Assert.AreEqual(4, num);
            }

            //Verify no items are dequeued
            Assert.AreEqual(10, UnsafeRingBuffer.GetCount(q));

            UnsafeRingBuffer.Free(q);
        }

        [Test]
        public void IteratorTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            //Wrap tail around
            SplitRingBuffer(q);

            //Iterator should start from the head.
            int num = 0;
            foreach (int i in UnsafeRingBuffer.GetEnumerator<int>(q))
            {
                Assert.AreEqual(num, i);
                num++;
            }

            Assert.AreEqual(num, UnsafeRingBuffer.GetCount(q));
            
            UnsafeRingBuffer.Free(q);
        }

        [Test]
        public void Contains()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            //Wrap tail around
            SplitRingBuffer(q);


            //Check tail and head end of the queue
            Assert.IsTrue(UnsafeRingBuffer.Contains(q, 1));
            Assert.IsTrue(UnsafeRingBuffer.Contains(q, 9));
            Assert.False(UnsafeRingBuffer.Contains(q, 11));

            UnsafeRingBuffer.Free(q);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeRingBuffer.Push<float>(q, 162); });

            UnsafeRingBuffer.Free(q);
        }
#endif

        [Test]
        public void CopyToTest()
        {
            var q = UnsafeRingBuffer.Allocate<int>(10);
            SplitRingBuffer(q);

            var arr = new int[10];
            fixed (void* ptr = arr)
            {
                UnsafeRingBuffer.CopyTo<int>(q, ptr, 0);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, arr[i]);
            }
        }
    }
}
