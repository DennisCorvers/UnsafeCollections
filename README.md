# UnsafeCollections

As this fork diverged too much from the [original](https://github.com/fholm/UnsafeCollections), it became its own repository.

This project contains various collections that perform no managed memory allocation. It alleviates GC (Garbage Collector) pressure useful for usecases such as Unity.

**This project is still a WIP**

Project is targeted as a .Net 2.0 Standard library. Usable in Unity via dll.

## Usage
The NativeCollections (under UnsafeCollections/Collections/Native/) are usable just like the matching collections in C# would. The API matches as much as possible with the C# API of the same collection. All of the NativeCollection objects are safe to pass as value.

You **must** instantiate the collections with any non-default constructor. After you are done using it, you again **must** call the Dispose function. The matching UnsafeCollection objects work similarly, but instead you must call Allocate and Free respectively.

## Currently Implemented

- Array
- List
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

## Planned Additions
- Concurrent Multi Producer Multi Consumer Dictionary (MPMC, mostly lockless)
- Single-Value heap
- Linked List

## Build
Use Preprocessor directive UNITY to build the project using the Unity memory allocators instead of the .Net ones.

The library is usable in both .Net and Unity.

## Performance
To be added...
