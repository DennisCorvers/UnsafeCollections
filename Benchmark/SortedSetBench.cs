using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnsafeCollections.Collections.Native;

namespace Benchmark
{
    public class SortedSetBench
    {
        const int COUNT = 32;

        NativeSortedSet<long> nset = new NativeSortedSet<long>(COUNT);
        SortedSet<long> set = new SortedSet<long>();

        public SortedSetBench()
        {

        }

        ~SortedSetBench()
        {
            nset.Dispose();
        }

        [Benchmark]
        public void SortedSetAdd()
        {
            for (int i = COUNT; i > 0; i--)
            {
                set.Add(i);
            }

            set.Clear();
        }

        [Benchmark]
        public void NativeSortedSetAdd()
        {
            for (int i = COUNT; i > 0; i--)
            {
                nset.Add(i);
            }

            nset.Clear();
        }
    }
}
