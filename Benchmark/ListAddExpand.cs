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
    public unsafe class ListAddExpand
    {
        const int COUNT = 8;
        const int COUNTMAX = 65;

        NativeList<long> nList = new NativeList<long>(COUNT);
        List<long> list = new List<long>(COUNT);
        UnsafeList* uList = UnsafeList.Allocate<long>(COUNT);

        ~ListAddExpand()
        {
            nList.Dispose();
            UnsafeList.Free(uList);
        }

        [Benchmark]
        public void ListAdd()
        {
            for (int i = 0; i < COUNTMAX; i++)
            {
                list.Add(i);
            }

            list.Clear();
        }

        [Benchmark]
        public void NativeListAdd()
        {
            for (int i = 0; i < COUNTMAX; i++)
            {
                nList.Add(i);
            }

            nList.Clear();
        }

        [Benchmark]
        public void UnsafeListAdd()
        {
            for (int i = 0; i < COUNTMAX; i++)
            {
                UnsafeList.Add(uList, i);
            }

            UnsafeList.Clear(uList);
        }
    }
}
