/*
The MIT License (MIT)

Copyright (c) 2021 Dennis Corvers

This software is based on, a modification of and/or an extention 
of "UnsafeCollections" originally authored by:

The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnsafeCollections.Debug;
#if UNITY
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeArray
    {
#if UNITY
        [NativeDisableUnsafePtrRestriction]
#endif
        void* _buffer;

        int _length;
        IntPtr _typeHandle;

        public static UnsafeArray* Allocate<T>(int size) where T : unmanaged
        {
            if (size < 0)
                throw new ArgumentOutOfRangeException(nameof(size), string.Format(ThrowHelper.ArgumentOutOfRange_MustBeNonNegNum, nameof(size)));

            var alignment = Memory.GetAlignment(sizeof(T));

            // pad the alignment of the array header
            var arrayStructSize = Memory.RoundToAlignment(sizeof(UnsafeArray), alignment);
            var arrayMemorySize = size * sizeof(T);

            // allocate memory for header + elements, aligned to 'alignment'
            var ptr = Memory.MallocAndZero(arrayStructSize + arrayMemorySize, alignment);

            UnsafeArray* array;
            array = (UnsafeArray*)ptr;
            array->_buffer = ((byte*)ptr) + arrayStructSize;
            array->_length = size;
            array->_typeHandle = typeof(T).TypeHandle.Value;

            return array;
        }

        public static void Free(UnsafeArray* array)
        {
            if (array == null)
                return;

            *array = default;

            Memory.Free(array);
        }

        internal static IntPtr GetTypeHandle(UnsafeArray* array)
        {
            return array->_typeHandle;
        }

        internal static void* GetBuffer(UnsafeArray* array)
        {
            return array->_buffer;
        }

        public static void Clear<T>(UnsafeArray* array) where T : unmanaged
        {
            Memory.ZeroMem(array->_buffer, array->_length * sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLength(UnsafeArray* array)
        {
            UDebug.Assert(array != null);
            return array->_length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPtr<T>(UnsafeArray* array, int index) where T : unmanaged
        {
            return GetPtr<T>(array, (long)index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetPtr<T>(UnsafeArray* array, long index) where T : unmanaged
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)array->_length)
                throw new IndexOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);

            return (T*)array->_buffer + index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(UnsafeArray* array, int index) where T : unmanaged
        {
            return *GetPtr<T>(array, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(UnsafeArray* array, long index) where T : unmanaged
        {
            return *GetPtr<T>(array, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetRef<T>(UnsafeArray* array, int index) where T : unmanaged
        {
            return ref *GetPtr<T>(array, index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T GetRef<T>(UnsafeArray* array, long index) where T : unmanaged
        {
            return ref *GetPtr<T>(array, index);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(UnsafeArray* array, int index, T value) where T : unmanaged
        {
            Set(array, (long)index, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(UnsafeArray* array, long index, T value) where T : unmanaged
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)array->_length)
                throw new IndexOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);

            *((T*)array->_buffer + index) = value;
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeArray* array) where T : unmanaged
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            return new Enumerator<T>(array);
        }

        public static void Copy<T>(UnsafeArray* source, int sourceIndex, UnsafeArray* destination, int destinationIndex, int count) where T : unmanaged
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (GetLength(source) - sourceIndex < count)
                throw new ArgumentException(ThrowHelper.Arg_ArrayPlusOffTooSmall);

            if (GetLength(destination) - destinationIndex < count)
                throw new ArgumentException(ThrowHelper.Arg_ArrayPlusOffTooSmall);

            UDebug.Assert(typeof(T).TypeHandle.Value == source->_typeHandle);
            UDebug.Assert(typeof(T).TypeHandle.Value == destination->_typeHandle);

            Memory.MemCpy((T*)destination->_buffer + destinationIndex, (T*)source->_buffer + sourceIndex, count * sizeof(T));
        }

        public void CopyTo<T>(void* destination, int destinationIndex) where T : unmanaged
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destinationIndex < 0)
                throw new ArgumentOutOfRangeException(ThrowHelper.ArgumentOutOfRange_Index);

            UDebug.Assert(typeof(T).TypeHandle.Value == _typeHandle);

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            Memory.MemCpy((T*)destination + destinationIndex, _buffer, _length * sizeof(T));
        }

        public void CopyFrom<T>(void* source, int sourceIndex, int count) where T : unmanaged
        {
            UDebug.Assert(typeof(T).TypeHandle.Value == _typeHandle);

            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if ((uint)sourceIndex + (uint)count > _length)
                throw new ArgumentException(ThrowHelper.Arg_ArrayPlusOffTooSmall);

            Memory.MemCpy(_buffer, (T*)source + sourceIndex, count * sizeof(T));
        }

        public static bool Contains<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            return IndexOf(array, item) > -1;
        }

        public static int IndexOf<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = 0; i < GetLength(array); ++i)
            {
                if (Get<T>(array, i).Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int LastIndexOf<T>(UnsafeArray* array, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = GetLength(array) - 1; i >= 0; --i)
            {
                if (Get<T>(array, i).Equals(item))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindIndex<T>(UnsafeArray* array, Func<T, bool> predicate) where T : unmanaged
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = 0; i < GetLength(array); ++i)
            {
                if (predicate(Get<T>(array, i)))
                {
                    return i;
                }
            }

            return -1;
        }

        public static int FindLastIndex<T>(UnsafeArray* array, Func<T, bool> predicate) where T : unmanaged
        {
            UDebug.Assert(array != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == array->_typeHandle);

            for (int i = GetLength(array) - 1; i >= 0; --i)
            {
                if (predicate(Get<T>(array, i)))
                {
                    return i;
                }
            }

            return -1;
        }


        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            T* _current;
            int _index;
            UnsafeArray* _array;

            internal Enumerator(UnsafeArray* array)
            {
                _index = 0;
                _array = array;
                _current = null;
            }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_array->_length)
                {
                    _current = (T*)_array->_buffer + _index;
                    _index++;
                    return true;
                }

                _current = default;
                return false;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_current != null);
                    return *_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _array->_length + 1)
                        throw new InvalidOperationException(ThrowHelper.InvalidOperation_EnumOpCantHappen);

                    return Current;
                }
            }

            public void Dispose()
            {
            }

            public Enumerator<T> GetEnumerator()
            {
                return this;
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
}