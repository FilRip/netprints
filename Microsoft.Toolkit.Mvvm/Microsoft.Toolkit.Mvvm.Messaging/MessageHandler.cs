namespace Microsoft.Toolkit.Mvvm.Messaging
{
    public delegate void MessageHandler<in TRecipient, in TMessage>(TRecipient recipient, TMessage message) where TRecipient : class where TMessage : class;
}
