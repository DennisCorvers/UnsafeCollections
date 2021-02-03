using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnsafeCollections.Collections.Native;
using UnsafeCollections.Collections.Native.Concurrent;
using UnsafeCollections.Collections.Unsafe;

namespace Benchmark
{
    public class QueueBench
    {
        const int COUNT = 32;

        NativeQueue<long> nq = new NativeQueue<long>(COUNT);
        Queue<long> q = new Queue<long>(COUNT);
        NativeSPSCQueue<long> cq = new NativeSPSCQueue<long>(COUNT);

        public QueueBench()
        {

        }

        ~QueueBench()
        {
            nq.Dispose();
        }

        [Benchmark]
        public void QueueAddPeekRemove()
        {
            for (int i = 0; i < COUNT; i++)
            {
                q.Enqueue(i);
            }

            var peek = q.Peek();

            for (int i = 0; i < COUNT; i++)
            {
                var res = q.Dequeue();
            }
        }

        [Benchmark]
        public void NativeQueueAddPeekRemove()
        {
            for (int i = 0; i < COUNT; i++)
            {
                nq.Enqueue(i);
            }

            var peek = nq.Peek();

            for (int i = 0; i < COUNT; i++)
            {
                var res = nq.Dequeue();
            }
        }

        [Benchmark]
        public void SPSCQueueAddPeekRemove()
        {
            for (int i = 0; i < COUNT; i++)
            {
                cq.Enqueue(i);
            }

            var peek = cq.Peek();

            for (int i = 0; i < COUNT; i++)
            {
                var res = cq.Dequeue();
            }
        }
    }
}
