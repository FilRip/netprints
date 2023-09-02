using System;

namespace Microsoft.Toolkit.Mvvm.Messaging
{
    public interface IMessenger
    {
        bool IsRegistered<TMessage, TToken>(object recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>;

        void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler) where TRecipient : class where TMessage : class where TToken : IEquatable<TToken>;

        void UnregisterAll(object recipient);

        void UnregisterAll<TToken>(object recipient, TToken token) where TToken : IEquatable<TToken>;

        void Unregister<TMessage, TToken>(object recipient, TToken token) where TMessage : class where TToken : IEquatable<TToken>;

        TMessage Send<TMessage, TToken>(TMessage message, TToken token) where TMessage : class where TToken : IEquatable<TToken>;

        void Cleanup();

        void Reset();
    }
}
