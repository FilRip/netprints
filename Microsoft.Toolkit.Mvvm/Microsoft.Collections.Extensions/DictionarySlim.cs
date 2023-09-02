using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#nullable enable

namespace Microsoft.Collections.Extensions
{
    [DebuggerDisplay("Count = {Count}")]
    internal class DictionarySlim<TKey, TValue> : IDictionarySlim<TKey, TValue>, IDictionarySlim<TKey>, IDictionarySlim where TKey : IEquatable<TKey> where TValue : class
    {
        private struct Entry
        {
            public TKey Key;

            public TValue? Value;

            public int Next;
        }

        public ref struct Enumerator
        {
            private readonly Entry[] entries;

            private int index;

            private int count;

            public TKey Key
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return entries[index - 1].Key;
                }
            }

            public TValue? Value
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return entries[index - 1].Value;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(DictionarySlim<TKey, TValue> dictionary)
            {
                entries = dictionary.entries;
                index = 0;
                count = dictionary.count;
            }

            public bool MoveNext()
            {
                if (count == 0)
                {
                    return false;
                }
                count--;
                Entry[] array = entries;
                while (array[index].Next < -1)
                {
                    index++;
                }
                index++;
                return true;
            }
        }

        private static readonly Entry[] InitialEntries = new Entry[1];

        private int count;

        private int freeList = -1;

        private int[] buckets;

        private Entry[] entries;

        public int Count => count;

        public TValue? this[TKey key]
        {
            get
            {
                Entry[] array = entries;
                int num = buckets[key.GetHashCode() & (buckets.Length - 1)] - 1;
                while ((uint)num < (uint)array.Length)
                {
                    if (key.Equals(array[num].Key))
                    {
                        return array[num].Value;
                    }
                    num = array[num].Next;
                }
                ThrowArgumentExceptionForKeyNotFound(key);
                return null;
            }
        }

        public DictionarySlim()
        {
            buckets = HashHelpers.SizeOneIntArray;
            entries = InitialEntries;
        }

        public void Clear()
        {
            count = 0;
            freeList = -1;
            buckets = HashHelpers.SizeOneIntArray;
            entries = InitialEntries;
        }

        public bool ContainsKey(TKey key)
        {
            Entry[] array = entries;
            int num = buckets[key.GetHashCode() & (buckets.Length - 1)] - 1;
            while ((uint)num < (uint)array.Length)
            {
                if (key.Equals(array[num].Key))
                {
                    return true;
                }
                num = array[num].Next;
            }
            return false;
        }

        public bool TryGetValue(TKey key, [NotNullWhen(true)] out TValue? value)
        {
            Entry[] array = entries;
            int num = buckets[key.GetHashCode() & (buckets.Length - 1)] - 1;
            while ((uint)num < (uint)array.Length)
            {
                if (key.Equals(array[num].Key))
                {
                    value = array[num].Value;
                    return true;
                }
                num = array[num].Next;
            }
            value = null;
            return false;
        }

        public bool TryRemove(TKey key)
        {
            Entry[] array = entries;
            int num = key.GetHashCode() & (buckets.Length - 1);
            int num2 = buckets[num] - 1;
            int num3 = -1;
            while (num2 != -1)
            {
                Entry entry = array[num2];
                if (entry.Key.Equals(key))
                {
                    if (num3 != -1)
                    {
                        array[num3].Next = entry.Next;
                    }
                    else
                    {
                        buckets[num] = entry.Next + 1;
                    }
                    array[num2] = default;
                    array[num2].Next = -3 - freeList;
                    freeList = num2;
                    count--;
                    return true;
                }
                num3 = num2;
                num2 = entry.Next;
            }
            return false;
        }

        public ref TValue? GetOrAddValueRef(TKey key)
        {
            Entry[] array = entries;
            int num = key.GetHashCode() & (buckets.Length - 1);
            int num2 = buckets[num] - 1;
            while ((uint)num2 < (uint)array.Length)
            {
                if (key.Equals(array[num2].Key))
                {
                    return ref array[num2].Value;
                }
                num2 = array[num2].Next;
            }
            return ref AddKey(key, num);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ref TValue? AddKey(TKey key, int bucketIndex)
        {
            Entry[] array = entries;
            int num;
            if (freeList != -1)
            {
                num = freeList;
                freeList = -3 - array[freeList].Next;
            }
            else
            {
                if (count == array.Length || array.Length == 1)
                {
                    array = Resize();
                    bucketIndex = key.GetHashCode() & (buckets.Length - 1);
                }
                num = count;
            }
            array[num].Key = key;
            array[num].Next = buckets[bucketIndex] - 1;
            buckets[bucketIndex] = num + 1;
            count++;
            return ref array[num].Value;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Entry[] Resize()
        {
            int num = count;
            int num2 = entries.Length * 2;
            if ((uint)num2 > 2147483647u)
            {
                ThrowInvalidOperationExceptionForMaxCapacityExceeded();
            }
            Entry[] array = new Entry[num2];
            Array.Copy(entries, 0, array, 0, num);
            int[] array2 = new int[array.Length];
            while (num-- > 0)
            {
                int num3 = array[num].Key.GetHashCode() & (array2.Length - 1);
                array[num].Next = array2[num3] - 1;
                array2[num3] = num + 1;
            }
            buckets = array2;
            entries = array;
            return array;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        private static void ThrowArgumentExceptionForKeyNotFound(TKey key)
        {
            throw new ArgumentException($"The target key {key} was not present in the dictionary");
        }

        private static void ThrowInvalidOperationExceptionForMaxCapacityExceeded()
        {
            throw new InvalidOperationException("Max capacity exceeded");
        }
    }
}
