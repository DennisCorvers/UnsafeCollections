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
