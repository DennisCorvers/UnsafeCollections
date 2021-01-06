/*
The MIT License (MIT)

Copyright (c) 2021 Dennis Corvers

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace UnsafeCollections.Collections.Unsafe.Concurrent
{
    // Used by all ConcurrentQueue implementations
    [StructLayout(LayoutKind.Explicit, Size = 3 * CACHE_LINE_SIZE)]
    [DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
    internal struct HeadAndTail
    {
        private const int CACHE_LINE_SIZE = 64;

        [FieldOffset(1 * CACHE_LINE_SIZE)]
        public int Head;

        [FieldOffset(2 * CACHE_LINE_SIZE)]
        public int Tail;
    }

    // This struct is only used to get the size in memory
    [StructLayout(LayoutKind.Sequential)]
    internal struct QueueSlot<T>
    {
#pragma warning disable IDE0044
        T Item;
        int SequenceNumber;
#pragma warning restore IDE0044
    }
}
