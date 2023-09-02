using System;

namespace Microsoft.Collections.Extensions
{
    internal interface IDictionarySlim
    {
        int Count { get; }

        void Clear();
    }
    internal interface IDictionarySlim<in TKey, out TValue> : IDictionarySlim<TKey>, IDictionarySlim where TKey : IEquatable<TKey> where TValue : class
    {
        TValue this[TKey key] { get; }
    }
    internal interface IDictionarySlim<in TKey> : IDictionarySlim where TKey : IEquatable<TKey>
    {
        bool TryRemove(TKey key);
    }
}
