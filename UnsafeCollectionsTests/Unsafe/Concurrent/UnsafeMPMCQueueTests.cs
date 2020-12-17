using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe.Concurrent;

namespace UnsafeCollectionsTests.Unsafe.Concurrent
{
    public unsafe class UnsafeMPMCQueueTests
    {
        private static void SplitQueue(UnsafeMPMCQueue* q)
        {
            //Wrap tail back to 0
            for (int i = 0; i < 5; i++)
                UnsafeMPMCQueue.TryEnqueue(q, 111);

            //First half
            for (int i = 0; i < 5; i++)
                UnsafeMPMCQueue.TryEnqueue(q, i);

            //Move head by 5
            for (int i = 0; i < 5; i++)
                UnsafeMPMCQueue.TryDequeue<int>(q, out int num);

            //Second half (head and tail are now both 5)
            for (int i = 5; i < 10; i++)
                UnsafeMPMCQueue.TryEnqueue(q, i);

            //Circular buffer now "ends" in the middle of the underlying array
        }


        [Test]
        public void ConstructorTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeMPMCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeMPMCQueue.GetCapacity(q));

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void EnqueueTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(100);

            for (int i = 0; i < 100; i++)
            {
                UnsafeMPMCQueue.TryEnqueue(q, i * i);
            }

            Assert.AreEqual(100, UnsafeMPMCQueue.GetCount(q));

            UnsafeMPMCQueue.Clear(q);

            Assert.AreEqual(0, UnsafeMPMCQueue.GetCount(q));

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void DequeueTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeMPMCQueue.TryEnqueue(q, i * i);


            for (int i = 0; i < 10; i++)
            {
                UnsafeMPMCQueue.TryDequeue(q, out int num);
                Assert.AreEqual(i * i, num);
            }

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void PeekTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeMPMCQueue.TryEnqueue(q, (int)Math.Pow(i + 2, 2));

            for (int i = 0; i < 10; i++)
            {
                UnsafeMPMCQueue.TryPeek(q, out int result);
                Assert.AreEqual(4, result);
            }

            //Verify no items are dequeued
            Assert.AreEqual(10, UnsafeMPMCQueue.GetCount(q));

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void ExpandTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>();

            SplitQueue(q);

            // Fill buffer beyond capacity
            for (int i = 0; i < 100;)
            {
                if (UnsafeMPMCQueue.TryEnqueue(q, 999))
                    i++;
            }


            Assert.AreEqual(110, UnsafeMPMCQueue.GetCount(q));

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void ExpandTestFixed()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(64, true);

            // Fill buffer to capacity
            for (int i = 0; i < 64;)
            {
                if (UnsafeMPMCQueue.TryEnqueue(q, i))
                    i++;
            }


            // Buffer is full, can no longer insert.
            Assert.IsFalse(UnsafeMPMCQueue.TryEnqueue(q, 10));

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void TryActionTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);

            //Insert 6 more to fill the queue
            for (int i = 0; i < 6; i++)
                UnsafeMPMCQueue.TryEnqueue(q, 999);

            Assert.IsTrue(UnsafeMPMCQueue.TryPeek(q, out int result));
            Assert.AreEqual(0, result);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(UnsafeMPMCQueue.TryDequeue(q, out int val));
                Assert.AreEqual(i, val);
            }

            //Empty 6 last items
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(UnsafeMPMCQueue.TryDequeue(q, out int val));

            //Empty queue
            Assert.IsFalse(UnsafeMPMCQueue.TryPeek(q, out int res));

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        public void ClearTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);

            Assert.AreEqual(10, UnsafeMPMCQueue.GetCount(q));
            UnsafeMPMCQueue.Clear(q);
            Assert.AreEqual(0, UnsafeMPMCQueue.GetCount(q));

            Assert.IsTrue(UnsafeMPMCQueue.IsEmpty<int>(q));

            UnsafeMPMCQueue.Free(q);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeMPMCQueue.TryEnqueue<float>(q, 162); });

            UnsafeMPMCQueue.Free(q);
        }
#endif

        [Test]
        //Demonstration that this queue is SPSC
        public void SPSCConcurrencyTest()
        {
            var q = UnsafeMPMCQueue.Allocate<ComplexType>(16);
            int count = 10000;


            Thread reader = new Thread(() =>
            {
                for (int i = 0; i < count;)
                {
                    if (UnsafeMPMCQueue.TryDequeue(q, out ComplexType num))
                    {
                        Assert.IsTrue(num.Equals(new ComplexType((ushort)i)));
                        i++;
                    }
                }
            });

            reader.Start();

            for (int i = 0; i < count;)
            {
                if (UnsafeMPMCQueue.TryEnqueue(q, new ComplexType((ushort)i)))
                    i++;
            }

            reader.Join();

            UnsafeMPMCQueue.Free(q);
        }

        [Test]
        // Demonstration that this queue is MPSC
        public void MPSCConcurrencyTest()
        {
            var q = UnsafeMPMCQueue.Allocate<int>(16000);
            int count = 10000;

            Thread writer = new Thread(() =>
            {
                for (int i = 0; i < count / 2;)
                    if (UnsafeMPMCQueue.TryEnqueue(q, i))
                        i++;
            });

            Thread writer2 = new Thread(() =>
            {
                for (int i = 0; i < count / 2;)
                    if (UnsafeMPMCQueue.TryEnqueue(q, i))
                        i++;
            });

            writer.Start();
            writer2.Start();


            writer.Join();
            writer2.Join();

            Assert.AreEqual(count, UnsafeMPMCQueue.GetCount(q));

            UnsafeMPMCQueue.Free(q);
        }

        private struct ComplexType : IEquatable<ComplexType>
        {
            ushort num1;
            ushort num2;
            ushort num3;

            public ComplexType(ushort num)
            {
                num1 = num2 = num3 = num;
            }

            public bool Equals(ComplexType other)
            {
                return
                    num1 == other.num1 &&
                    num2 == other.num2 &&
                    num3 == other.num3;
            }
        }
    }
}
