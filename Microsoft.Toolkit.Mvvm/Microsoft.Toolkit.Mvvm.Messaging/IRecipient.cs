namespace Microsoft.Toolkit.Mvvm.Messaging
{
    public interface IRecipient<in TMessage> where TMessage : class
    {
        void Receive(TMessage message);
    }
}
