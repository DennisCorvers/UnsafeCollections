# UnsafeCollections

As this fork diverged too much from the [original](https://github.com/fholm/UnsafeCollections), it became its own repository.

This project contains various collections that perform no managed memory allocation. It alleviates GC (Garbage Collector) pressure useful for usecases such as Unity.

Project is targeted as a .Net 2.0 Standard library. Usable in Unity via dll.

## Usage
The NativeCollections (under UnsafeCollections/Collections/Native/) are usable just like the matching collections in C# would. The API matches as much as possible with the C# API of the same collection. All of the NativeCollection objects are safe to pass as value.

You **must** instantiate the collections with any non-default constructor. After you are done using it, you again **must** call the Dispose function. The matching UnsafeCollection objects work similarly, but instead you must call Allocate and Free respectively.

## Currently Implemented

- Array
- List
- LinkedList
- Stack
- Queue
- Bit Set
- Ring Buffer
- Min Heap
- Max Heap
- Dictionary
- HashSet
- SortedDictionary
- SortedSet
- Concurrent SPSC Lockfree Queue
- Concurrent MPSC Lockfree Queue
- Concurrent MPMC Queue (Lockfree with fixed size) 


## Build
Use Preprocessor directive UNITY to build the project using the Unity memory allocators instead of the .Net ones.

The library is usable in both .Net and Unity.

## Performance

Comparison is made for List between .Net List, UnsafeList and NativeList. This is done not only to show the difference between this and the .Net implementation, but also between the Native and Unsafe implementations.

### List

Adding x items to a list, followed by a clear:
|        Method |      Mean |    Error |   StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------- |----------:|---------:|---------:|------:|------:|------:|----------:|
|       ListAdd | 196.98 ns | 0.253 ns | 0.236 ns |     - |     - |     - |         - |
| NativeListAdd |  76.38 ns | 0.496 ns | 0.464 ns |     - |     - |     - |         - |
| UnsafeListAdd |  58.59 ns | 0.293 ns | 0.274 ns |     - |     - |     - |         - |

Adding x items to a list where the list has to resize (followed by a clear):
|        Method |     Mean |   Error |  StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------- |---------:|--------:|--------:|------:|------:|------:|----------:|
|       ListAdd | 399.7 ns | 4.60 ns | 4.30 ns |     - |     - |     - |         - |
| NativeListAdd | 161.2 ns | 0.13 ns | 0.12 ns |     - |     - |     - |         - |
| UnsafeListAdd | 124.9 ns | 0.30 ns | 0.28 ns |     - |     - |     - |         - |

Grabbing the index of a "random" item in the list
|            Method |     Mean |    Error |   StdDev | Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------------ |---------:|---------:|---------:|------:|------:|------:|----------:|
|       ListIndexOf | 28.76 ns | 0.206 ns | 0.193 ns |     - |     - |     - |         - |
| NativeListIndexOf | 23.69 ns | 0.038 ns | 0.036 ns |     - |     - |     - |         - |
| UnsafeListIndexOf | 23.94 ns | 0.190 ns | 0.178 ns |     - |     - |     - |         - |


Because the difference in performance between Unsafe and Native is minor, only a comparison between .Net and Native will be made from this point onwards. Do note that the timings for the Unsafe collections are usually slightly lower.

### Queue

Enqueue > Peek > Dequeue operations
|                   Method |     Mean |   Error |  StdDev |
|------------------------- |---------:|--------:|--------:|
|       QueueAddPeekRemove | 444.3 ns | 0.37 ns | 0.35 ns |
| NativeQueueAddPeekRemove | 194.4 ns | 1.47 ns | 1.23 ns |
|   SPSCQueueAddPeekRemove | 219.2 ns | 0.19 ns | 0.17 ns |

### Sorted Set

SortedSet Add in reverse order (worst case)
|             Method |     Mean |     Error |    StdDev |
|------------------- |---------:|----------:|----------:|
|       SortedSetAdd | 1.118 μs | 0.0042 μs | 0.0040 μs |
| NativeSortedSetAdd | 1.964 μs | 0.0104 μs | 0.0098 μs |

### Dictionary

Dictionary Add followed by Remove
|             Method |     Mean |   Error |  StdDev |
|------------------- |---------:|--------:|--------:|
|       SortedSetAdd | 580.0 ns | 0.82 ns | 0.73 ns |
| NativeSortedSetAdd | 461.8 ns | 2.82 ns | 2.64 ns |
