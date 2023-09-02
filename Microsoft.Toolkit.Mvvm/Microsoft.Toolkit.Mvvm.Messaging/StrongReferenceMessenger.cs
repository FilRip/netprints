using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Collections.Extensions;
using Microsoft.Toolkit.Mvvm.Messaging.Internals;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging
{
    public sealed class StrongReferenceMessenger : IMessenger
    {
        private sealed class Mapping<TMessage, TToken> : DictionarySlim<Recipient, DictionarySlim<TToken, object>>, IMapping, IDictionarySlim<Recipient>, IDictionarySlim where TMessage : class where TToken : IEquatable<TToken>
        {
            public Type2 TypeArguments { get; }

            public Mapping()
            {
                TypeArguments = new Type2(typeof(TMessage), typeof(TToken));
            }
        }

        private interface IMapping : IDictionarySlim<Recipient>, IDictionarySlim
        {
            Type2 TypeArguments { get; }
        }

        private readonly struct Recipient : IEquatable<Recipient>
        {
            public readonly object Target;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Recipient(object target)
            {
                Target = target;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Recipient other)
            {
                return Target == other.Target;
            }

            public override bool Equals(object? obj)
            {
                if (obj is Recipient other)
                {
                    return Equals(other);
                }
                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(Target);
            }
        }

        private readonly DictionarySlim<Recipient, HashSet<IMapping>> recipientsMap = new();

        private readonly DictionarySlim<Type2, IMapping> typesMap = new();

        public static StrongReferenceMessenger Default { get; } = new();


        public bool IsRegistered<TMessage, TToken>(object recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                if (!TryGetMapping(out Mapping<TMessage, TToken> mapping))
                {
                    return false;
                }
                Recipient key = new(recipient);
                return mapping.ContainsKey(key);
            }
        }

        public void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler) where TRecipient : class where TMessage : class where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                Mapping<TMessage, TToken> orAddMapping = GetOrAddMapping<TMessage, TToken>();
                Recipient key = new(recipient);
                ref DictionarySlim<TToken, object> orAddValueRef = ref orAddMapping.GetOrAddValueRef(key);
                if (orAddValueRef == null)
                {
                    orAddValueRef = new DictionarySlim<TToken, object>();
                }
                ref object? orAddValueRef2 = ref orAddValueRef.GetOrAddValueRef(token);
                if (orAddValueRef2 != null)
                {
                    ThrowInvalidOperationExceptionForDuplicateRegistration();
                }
                orAddValueRef2 = handler;
                ref HashSet<IMapping> orAddValueRef3 = ref recipientsMap.GetOrAddValueRef(key);
                if (orAddValueRef3 == null)
                {
                    orAddValueRef3 = new HashSet<IMapping>();
                }
                orAddValueRef3.Add(orAddMapping);
            }
        }

        public void UnregisterAll(object recipient)
        {
            lock (recipientsMap)
            {
                Recipient key = new(recipient);
                if (!recipientsMap.TryGetValue(key, out var value))
                {
                    return;
                }
                foreach (IMapping item in value)
                {
                    if (item.TryRemove(key) && item.Count == 0)
                    {
                        typesMap.TryRemove(item.TypeArguments);
                    }
                }
                recipientsMap.TryRemove(key);
            }
        }

        public void UnregisterAll<TToken>(object recipient, TToken token) where TToken : IEquatable<TToken>
        {
            bool lockTaken = false;
            object[] array = null;
            int length = 0;
            try
            {
                Monitor.Enter(recipientsMap, ref lockTaken);
                Recipient key = new(recipient);
                if (!recipientsMap.TryGetValue(key, out var value))
                {
                    return;
                }
                array = ArrayPool<object>.Shared.Rent(value.Count);
                foreach (IMapping item in value)
                {
                    if (item is IDictionarySlim<Recipient, IDictionarySlim<TToken>> dictionarySlim)
                    {
                        array[length++] = dictionarySlim;
                    }
                }
                Span<object> span = array.AsSpan(0, length);
                for (int i = 0; i < span.Length; i++)
                {
                    IDictionarySlim<Recipient, IDictionarySlim<TToken>> dictionarySlim2 = Unsafe.As<IDictionarySlim<Recipient, IDictionarySlim<TToken>>>(span[i]);
                    IDictionarySlim<TToken> dictionarySlim3 = dictionarySlim2[key];
                    if (dictionarySlim3.TryRemove(token) && dictionarySlim3.Count == 0)
                    {
                        dictionarySlim2.TryRemove(key);
                        if (dictionarySlim2.Count == 0 && value.Remove(Unsafe.As<IMapping>(dictionarySlim2)) && value.Count == 0)
                        {
                            recipientsMap.TryRemove(key);
                        }
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(recipientsMap);
                }
                if (array != null)
                {
                    array.AsSpan(0, length).Clear();
                    ArrayPool<object>.Shared.Return(array);
                }
            }
        }

        public void Unregister<TMessage, TToken>(object recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                if (!TryGetMapping(out Mapping<TMessage, TToken> mapping))
                {
                    return;
                }
                Recipient key = new(recipient);
                if (mapping.TryGetValue(key, out var value) && value.TryRemove(token) && value.Count == 0)
                {
                    mapping.TryRemove(key);
                    HashSet<IMapping> hashSet = recipientsMap[key];
                    if (hashSet.Remove(mapping) && hashSet.Count == 0)
                    {
                        recipientsMap.TryRemove(key);
                    }
                }
            }
        }

        public TMessage Send<TMessage, TToken>(TMessage message, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            int num = 0;
            Span<object> span;
            object[] array;
            lock (recipientsMap)
            {
                TryGetMapping(out Mapping<TMessage, TToken> mapping);
                int num2 = mapping?.Count ?? 0;
                if (num2 == 0)
                {
                    return message;
                }
                span = (array = ArrayPool<object>.Shared.Rent(2 * num2));
                DictionarySlim<Recipient, DictionarySlim<TToken, object>>.Enumerator enumerator = mapping.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Value.TryGetValue(token, out object? value))
                    {
                        span[2 * num] = value;
                        span[2 * num + 1] = enumerator.Key.Target;
                        num++;
                    }
                }
            }
            try
            {
                for (int i = 0; i < num; i++)
                {
                    Unsafe.As<MessageHandler<object, TMessage>>(span[2 * i])(span[2 * i + 1], message);
                }
                return message;
            }
            finally
            {
                Array.Clear(array, 0, 2 * num);
                ArrayPool<object>.Shared.Return(array);
            }
        }

        void IMessenger.Cleanup()
        {
        }

        public void Reset()
        {
            lock (recipientsMap)
            {
                recipientsMap.Clear();
                typesMap.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryGetMapping<TMessage, TToken>([NotNullWhen(true)] out Mapping<TMessage, TToken>? mapping) where TMessage : class where TToken : IEquatable<TToken>
        {
            Type2 key = new(typeof(TMessage), typeof(TToken));
            if (typesMap.TryGetValue(key, out var value))
            {
                mapping = Unsafe.As<Mapping<TMessage, TToken>>(value);
                return true;
            }
            mapping = null;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Mapping<TMessage, TToken> GetOrAddMapping<TMessage, TToken>() where TMessage : class where TToken : IEquatable<TToken>
        {
            Type2 key = new(typeof(TMessage), typeof(TToken));
            ref IMapping orAddValueRef = ref typesMap.GetOrAddValueRef(key);
            if (orAddValueRef == null)
            {
                orAddValueRef = new Mapping<TMessage, TToken>();
            }
            return Unsafe.As<Mapping<TMessage, TToken>>(orAddValueRef);
        }

        private static void ThrowInvalidOperationExceptionForDuplicateRegistration()
        {
            throw new InvalidOperationException("The target recipient has already subscribed to the target message");
        }
    }
}
