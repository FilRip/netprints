#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging.Messages
{
    public class PropertyChangedMessage<T>
    {
        public object Sender { get; }

        public string? PropertyName { get; }

        public T OldValue { get; }

        public T NewValue { get; }

        public PropertyChangedMessage(object sender, string? propertyName, T oldValue, T newValue)
        {
            Sender = sender;
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
