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

namespace UnsafeCollections.Debug
{
    internal static class ThrowHelper
    {
        internal const string @Arg_ArrayLengthsDiffer = "Array lengths must be the same.";
        internal const string @Arg_ArrayPlusOffTooSmall = "Destination array is not long enough to copy all the items in the collection. Check array index and length.";

        internal const string @Arg_AddingDuplicateWithKey = "An item with the same key has already been added. Key: {0}";
        internal const string @Arg_KeyNotFoundWithKey = "Arg_KeyNotFoundWithKey";

        internal const string @ArgumentOutOfRange_Index = "Index was out of range. Must be non-negative and less than the size of the collection.";
        internal const string @ArgumentOutOfRange_MustBeNonNegInt32 = "Value must be non-negative and less than or equal to Int32.MaxValue.";
        internal const string @ArgumentOutOfRange_MustBeNonNegNum = "{0} must be non-negative.";
        internal const string @ArgumentOutOfRange_MustBePositive = "{0} must be greater than zero.";
        internal const string @ArgumentOutOfRange_NeedNonNegNum = "Non-negative number required.";

        internal const string @InvalidOperation_EnumOpCantHappen = "Enumeration has either not started or has already finished.";
        internal const string @InvalidOperation_EnumFailedVersion = "Collection was modified; enumeration operation may not execute.";

        internal const string @InvalidOperation_CollectionFull = "Fixed size collection is full.";
        internal const string @InvalidOperation_EmptyQueue = "Queue empty.";
        internal const string @InvalidOperation_EmptyStack = "Stack empty.";
        internal const string @InvalidOperation_EmptyHeap = "Heap empty.";
        internal const string @InvalidOperation_EmptyLinkedList = "The LinkedList is empty.";

        internal const string @Arg_BitSetLengthsDiffer = "BitSet lengths must be the same.";
    }
}
