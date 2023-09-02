using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Microsoft.Toolkit.Mvvm.Messaging.Internals
{
    internal ref struct ArrayPoolBufferWriter<T>
    {
        private const int DefaultInitialBufferSize = 128;

        private T[] array;

        private int index;

        public ReadOnlySpan<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return array.AsSpan(0, index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ArrayPoolBufferWriter<T> Create()
        {
            ArrayPoolBufferWriter<T> result = default;
            result.array = ArrayPool<T>.Shared.Rent(DefaultInitialBufferSize);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T item)
        {
            if (index == array.Length)
            {
                ResizeBuffer();
            }
            array[index++] = item;
        }

        public void Reset()
        {
            Array.Clear(array, 0, index);
            index = 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ResizeBuffer()
        {
            T[] destinationArray = ArrayPool<T>.Shared.Rent(index << 2);
            Array.Copy(array, 0, destinationArray, 0, index);
            Array.Clear(array, 0, index);
            ArrayPool<T>.Shared.Return(array);
            array = destinationArray;
        }

        public void Dispose()
        {
            Array.Clear(array, 0, index);
            ArrayPool<T>.Shared.Return(array);
        }
    }
}
