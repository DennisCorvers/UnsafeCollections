using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnsafeCollections.Collections.Native;

namespace Benchmark
{
    public class DictionaryBench
    {
        const int COUNT = 32;

        NativeDictionary<int, long> ndict = new NativeDictionary<int, long>(COUNT);
        Dictionary<int, long> dict = new Dictionary<int, long>(COUNT);

        public DictionaryBench()
        {

        }

        ~DictionaryBench()
        {
            ndict.Dispose();
        }

        [Benchmark]
        public void SortedSetAdd()
        {
            for (int i = 0; i < COUNT; i++)
            {
                dict.Add(i, i * i);
            }

            for (int i = 0; i < COUNT; i++)
            {
                dict.Remove(i);
            }
        }

        [Benchmark]
        public void NativeSortedSetAdd()
        {
            for (int i = 0; i < COUNT; i++)
            {
                ndict.Add(i, i * i);
            }

            for (int i = 0; i < COUNT; i++)
            {
                ndict.Remove(i);
            }
        }
    }
}
