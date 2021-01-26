using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using UnsafeCollections.Collections.Unsafe;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(typeof(NativeDictionaryDebugView<,>))]
    public unsafe struct NativeDictionary<K, V> : INativeDictionary<K, V>, IReadOnlyDictionary<K, V>
        where K : unmanaged, IEquatable<K>
        where V : unmanaged
    {
        private UnsafeDictionary* m_inner;

        public bool IsCreated
        {
            get
            {
                return m_inner != null;
            }
        }
        public int Count
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeDictionary.GetCount(m_inner);
            }
        }
        public int Capacity
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeDictionary.GetCapacity(m_inner);
            }
        }
        public bool IsFixedSize
        {
            get
            {
                if (m_inner == null)
                    throw new NullReferenceException();
                return UnsafeDictionary.IsFixedSize(m_inner);
            }
        }
        public bool IsReadOnly
        {
            get { return false; }
        }

        public V this[K key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return UnsafeDictionary.Get<K, V>(m_inner, key); }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set { UnsafeDictionary.Set<K, V>(m_inner, key, value); }
        }

        public KeyCollection Keys
        {
            get => new KeyCollection(this);
        }
        public ValueCollection Values
        {
            get => new ValueCollection(this);
        }

        ICollection<K> IDictionary<K, V>.Keys
        {
            get => Keys;
        }
        ICollection<V> IDictionary<K, V>.Values
        {
            get => Values;
        }

        IEnumerable<K> IReadOnlyDictionary<K, V>.Keys
        {
            get => Keys;
        }
        IEnumerable<V> IReadOnlyDictionary<K, V>.Values
        {
            get => Values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UnsafeDictionary* GetInnerCollection()
        {
            return m_inner;
        }

        public NativeDictionary(int capacity)
        {
            m_inner = UnsafeDictionary.Allocate<K, V>(capacity, false);
        }

        public NativeDictionary(int capacity, bool fixedSize)
        {
            m_inner = UnsafeDictionary.Allocate<K, V>(capacity, fixedSize);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(K key, V value)
        {
            UnsafeDictionary.Add<K, V>(m_inner, key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(K key, V value)
        {
            return UnsafeDictionary.TryAdd<K, V>(m_inner, key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(KeyValuePair<K, V> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            UnsafeDictionary.Clear(m_inner);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(KeyValuePair<K, V> item)
        {
            if (TryGetValue(item.Key, out V value))
            {
                var eq = EqualityComparer<V>.Default;
                return eq.Equals(item.Value, value);
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(K key)
        {
            return UnsafeDictionary.ContainsKey<K>(m_inner, key);
        }

        public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
        {
            UnsafeDictionary.CopyTo<K, V>(m_inner, array, arrayIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(K key)
        {
            return UnsafeDictionary.Remove<K>(m_inner, key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(KeyValuePair<K, V> item)
        {
            return Remove(item.Key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(K key, out V value)
        {
            return UnsafeDictionary.TryGetValue<K, V>(m_inner, key, out value);
        }

        public UnsafeDictionary.Enumerator<K, V> GetEnumerator()
        {
            return UnsafeDictionary.GetEnumerator<K, V>(m_inner);
        }
        IEnumerator<KeyValuePair<K, V>> IEnumerable<KeyValuePair<K, V>>.GetEnumerator()
        {
            return UnsafeDictionary.GetEnumerator<K, V>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeDictionary.GetEnumerator<K, V>(m_inner);
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeDictionary.Free(m_inner);
            m_inner = null;
        }


        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(NativeDictionaryKeyCollectionDebugView<,>))]
        public unsafe struct KeyCollection : INativeCollection<K>, INativeReadOnlyCollection<K>
        {
            private const string NOT_SUPPORTED_MUTATION = "Mutating a key collection derived from a dictionary is not allowed.";
            private readonly NativeDictionary<K, V> _dictionary;

            public KeyCollection(NativeDictionary<K, V> dictionary)
            {
                if (!dictionary.IsCreated)
                    throw new ArgumentNullException(nameof(dictionary));

                _dictionary = dictionary;
            }

            public int Count
            {
                get { return _dictionary.Count; }
            }

            bool ICollection<K>.IsReadOnly
            {
                get => true;
            }
            bool INativeCollection<K>.IsCreated
            {
                get => _dictionary.IsCreated;
            }
            bool INativeReadOnlyCollection<K>.IsCreated
            {
                get => _dictionary.IsCreated;
            }

            public UnsafeDictionary.KeyEnumerator<K> GetEnumerator()
            {
                return new UnsafeDictionary.KeyEnumerator<K>(_dictionary.m_inner);
            }
            IEnumerator<K> IEnumerable<K>.GetEnumerator()
            {
                return new UnsafeDictionary.KeyEnumerator<K>(_dictionary.m_inner);
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new UnsafeDictionary.KeyEnumerator<K>(_dictionary.m_inner);
            }

            public void CopyTo(K[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if ((uint)index > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (array.Length - index < Count)
                    throw new ArgumentException("Insufficient space in the target location to copy the information.");

                if (array.Length == 0)
                    return;

                int i = index;
                var enumerator = _dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                    array[i++] = enumerator.CurrentKey;
            }

            void ICollection<K>.Add(K item)
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }

            void ICollection<K>.Clear()
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }

            bool ICollection<K>.Contains(K item)
            {
                return _dictionary.ContainsKey(item);
            }

            bool ICollection<K>.Remove(K item)
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }

            public NativeArray<K> ToNativeArray()
            {
                var arr = new NativeArray<K>(Count);
                int i = 0;
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                    arr[i++] = enumerator.Current;

                return arr;
            }

            void ICollection<K>.CopyTo(K[] array, int arrayIndex)
            {
                CopyTo(array, arrayIndex);
            }

            void IDisposable.Dispose()
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }
        }

        [DebuggerDisplay("Count = {Count}")]
        [DebuggerTypeProxy(typeof(NativeDictionaryValueCollectionDebugView<,>))]
        public unsafe struct ValueCollection : INativeCollection<V>, INativeReadOnlyCollection<V>
        {
            private const string NOT_SUPPORTED_MUTATION = "Mutating a key collection derived from a dictionary is not allowed.";
            private readonly NativeDictionary<K, V> _dictionary;

            public ValueCollection(NativeDictionary<K, V> dictionary)
            {
                if (!dictionary.IsCreated)
                    throw new ArgumentNullException(nameof(dictionary));

                _dictionary = dictionary;
            }

            public int Count
            {
                get { return _dictionary.Count; }
            }

            bool ICollection<V>.IsReadOnly
            {
                get => true;
            }
            bool INativeCollection<V>.IsCreated
            {
                get => _dictionary.IsCreated;
            }
            bool INativeReadOnlyCollection<V>.IsCreated
            {
                get => _dictionary.IsCreated;
            }

            public UnsafeDictionary.ValueEnumerator<V> GetEnumerator()
            {
                return new UnsafeDictionary.ValueEnumerator<V>(_dictionary.m_inner);
            }
            IEnumerator<V> IEnumerable<V>.GetEnumerator()
            {
                return new UnsafeDictionary.ValueEnumerator<V>(_dictionary.m_inner);
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return new UnsafeDictionary.ValueEnumerator<V>(_dictionary.m_inner);
            }

            public void CopyTo(V[] array, int index)
            {
                if (array == null)
                    throw new ArgumentNullException(nameof(array));

                if ((uint)index > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (array.Length - index < Count)
                    throw new ArgumentException("Insufficient space in the target location to copy the information.");

                if (array.Length == 0)
                    return;

                int i = index;
                var enumerator = _dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                    array[i++] = enumerator.CurrentValue;
            }

            void ICollection<V>.Add(V item)
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }

            void ICollection<V>.Clear()
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }

            bool ICollection<V>.Contains(V item)
            {
                var comparer = EqualityComparer<V>.Default;
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (comparer.Equals(item, enumerator.Current))
                        return true;
                }

                return false;
            }

            bool ICollection<V>.Remove(V item)
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }

            public NativeArray<V> ToNativeArray()
            {
                var arr = new NativeArray<V>(Count);
                int i = 0;
                var enumerator = GetEnumerator();
                while (enumerator.MoveNext())
                    arr[i++] = enumerator.Current;

                return arr;
            }

            void ICollection<V>.CopyTo(V[] array, int arrayIndex)
            {
                CopyTo(array, arrayIndex);
            }

            void IDisposable.Dispose()
            {
                throw new NotSupportedException(NOT_SUPPORTED_MUTATION);
            }
        }
    }

    public unsafe static class NativeDictionaryExtensions
    {
        public static bool ContainsValue<K, V>(this NativeDictionary<K, V> dict, V value)
            where K : unmanaged, IEquatable<K>
            where V : unmanaged, IEquatable<V>
        {
            return UnsafeDictionary.ContainsValue<V>(dict.GetInnerCollection(), value);
        }
    }
}
