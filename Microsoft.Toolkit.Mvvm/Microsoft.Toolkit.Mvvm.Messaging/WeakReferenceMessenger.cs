using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.Collections.Extensions;
using Microsoft.Toolkit.Mvvm.Messaging.Internals;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging
{
    public sealed class WeakReferenceMessenger : IMessenger
    {
        private readonly DictionarySlim<Type2, ConditionalWeakTable<object, IDictionarySlim>> recipientsMap = new();

        public static WeakReferenceMessenger Default { get; } = new WeakReferenceMessenger();


        public WeakReferenceMessenger()
        {
            System.Gen2GcCallback.Register(Gen2GcCallbackProxy, this);
            static void Gen2GcCallbackProxy(object target)
            {
                ((WeakReferenceMessenger)target).CleanupWithNonBlockingLock();
            }
        }

        public bool IsRegistered<TMessage, TToken>(object recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                Type2 key = new(typeof(TMessage), typeof(TToken));
                return recipientsMap.TryGetValue(key, out ConditionalWeakTable<object, IDictionarySlim>? value) && value.TryGetValue(recipient, out IDictionarySlim? value2) && Unsafe.As<DictionarySlim<TToken, object>>(value2).ContainsKey(token);
            }
        }

        public void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler) where TRecipient : class where TMessage : class where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                Type2 key = new(typeof(TMessage), typeof(TToken));
                ref ConditionalWeakTable<object, IDictionarySlim>? orAddValueRef = ref recipientsMap.GetOrAddValueRef(key);
                if (orAddValueRef == null)
                {
                    orAddValueRef = new ConditionalWeakTable<object, IDictionarySlim>();
                }
                ref object? orAddValueRef2 = ref Unsafe.As<DictionarySlim<TToken, object>>(orAddValueRef.GetValue(recipient, (object _) => new DictionarySlim<TToken, object>())).GetOrAddValueRef(token);
                if (orAddValueRef2 != null)
                {
                    ThrowInvalidOperationExceptionForDuplicateRegistration();
                }
                orAddValueRef2 = handler;
            }
        }

        public void UnregisterAll(object recipient)
        {
            lock (recipientsMap)
            {
                DictionarySlim<Type2, ConditionalWeakTable<object, IDictionarySlim>>.Enumerator enumerator = recipientsMap.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Value != null)
                        enumerator.Value.Remove(recipient);
                }
            }
        }

        public void UnregisterAll<TToken>(object recipient, TToken token) where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                DictionarySlim<Type2, ConditionalWeakTable<object, IDictionarySlim>>.Enumerator enumerator = recipientsMap.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Key.TToken == typeof(TToken) && enumerator.Value != null && enumerator.Value.TryGetValue(recipient, out var value))
                    {
                        Unsafe.As<DictionarySlim<TToken, object>>(value).TryRemove(token);
                    }
                }
            }
        }

        public void Unregister<TMessage, TToken>(object recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            lock (recipientsMap)
            {
                Type2 key = new(typeof(TMessage), typeof(TToken));
                if (recipientsMap.TryGetValue(key, out var value) && value.TryGetValue(recipient, out var value2))
                {
                    Unsafe.As<DictionarySlim<TToken, object>>(value2).TryRemove(token);
                }
            }
        }

        public TMessage Send<TMessage, TToken>(TMessage message, TToken token) where TMessage : class where TToken : IEquatable<TToken>
        {
            int num = 0;
            ArrayPoolBufferWriter<object> arrayPoolBufferWriter;
            lock (recipientsMap)
            {
                Type2 key = new(typeof(TMessage), typeof(TToken));
                if (!recipientsMap.TryGetValue(key, out var value))
                {
                    return message;
                }
                arrayPoolBufferWriter = ArrayPoolBufferWriter<object>.Create();
                foreach (KeyValuePair<object, IDictionarySlim> item in value)
                {
                    if (Unsafe.As<DictionarySlim<TToken, object>>(item.Value).TryGetValue(token, out object? value2))
                    {
                        arrayPoolBufferWriter.Add(value2);
                        arrayPoolBufferWriter.Add(item.Key);
                        num++;
                    }
                }
            }
            try
            {
                ReadOnlySpan<object> span = arrayPoolBufferWriter.Span;
                for (int i = 0; i < num; i++)
                {
                    Unsafe.As<MessageHandler<object, TMessage>>(span[2 * i])(span[2 * i + 1], message);
                }
                return message;
            }
            finally
            {
                arrayPoolBufferWriter.Dispose();
            }
        }

        public void Cleanup()
        {
            lock (recipientsMap)
            {
                CleanupWithoutLock();
            }
        }

        public void Reset()
        {
            lock (recipientsMap)
            {
                recipientsMap.Clear();
            }
        }

        private void CleanupWithNonBlockingLock()
        {
            object obj = recipientsMap;
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(obj, ref lockTaken);
                if (lockTaken)
                {
                    CleanupWithoutLock();
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(obj);
                }
            }
        }

        private void CleanupWithoutLock()
        {
            ArrayPoolBufferWriter<Type2> arrayPoolBufferWriter = ArrayPoolBufferWriter<Type2>.Create();
            try
            {
                ArrayPoolBufferWriter<object> arrayPoolBufferWriter2 = ArrayPoolBufferWriter<object>.Create();
                try
                {
                    DictionarySlim<Type2, ConditionalWeakTable<object, IDictionarySlim>>.Enumerator enumerator = recipientsMap.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        arrayPoolBufferWriter2.Reset();
                        bool flag = false;
                        if (enumerator.Value != null)
                        {
                            foreach (KeyValuePair<object, IDictionarySlim> item in enumerator.Value)
                            {
                                if (item.Value.Count == 0)
                                {
                                    arrayPoolBufferWriter2.Add(item.Key);
                                }
                                else
                                {
                                    flag = true;
                                }
                            }
                            ReadOnlySpan<object> span = arrayPoolBufferWriter2.Span;
                            for (int i = 0; i < span.Length; i++)
                            {
                                object key = span[i];
                                enumerator.Value.Remove(key);
                            }
                            if (!flag)
                            {
                                arrayPoolBufferWriter.Add(enumerator.Key);
                            }
                        }
                    }
                    ReadOnlySpan<Type2> span2 = arrayPoolBufferWriter.Span;
                    for (int i = 0; i < span2.Length; i++)
                    {
                        Type2 key2 = span2[i];
                        recipientsMap.TryRemove(key2);
                    }
                }
                finally
                {
                    arrayPoolBufferWriter2.Dispose();
                }
            }
            finally
            {
                arrayPoolBufferWriter.Dispose();
            }
        }

        private static void ThrowInvalidOperationExceptionForDuplicateRegistration()
        {
            throw new InvalidOperationException("The target recipient has already subscribed to the target message");
        }
    }
}