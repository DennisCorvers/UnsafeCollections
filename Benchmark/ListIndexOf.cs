using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnsafeCollections.Collections.Native;
using UnsafeCollections.Collections.Unsafe;

namespace Benchmark
{
    [MemoryDiagnoser]
    public unsafe class ListIndexOf
    {
        const int COUNT = 32;

        NativeList<long> nList = new NativeList<long>(COUNT);
        List<long> list = new List<long>(COUNT);
        UnsafeList* uList = UnsafeList.Allocate<long>(COUNT);


        public ListIndexOf()
        {
            for (int i = 0; i < COUNT; i++)
            {
                list.Add(i);
                nList.Add(i);
                UnsafeList.Add(uList, i);
            }
        }

        ~ListIndexOf()
        {
            nList.Dispose();
            UnsafeList.Free(uList);
        }

        [Benchmark]
        public void ListIndexof()
        {
            int num = list.IndexOf(COUNT / 3 * 2);
        }

        [Benchmark]
        public void NativeListIndexOf()
        {
            int num = nList.IndexOf(COUNT / 3 * 2);
        }

        [Benchmark]
        public void UnsafeListIndexOf()
        {
            int num = UnsafeList.IndexOf<long>(uList, COUNT / 3 * 2);
        }
    }
}
