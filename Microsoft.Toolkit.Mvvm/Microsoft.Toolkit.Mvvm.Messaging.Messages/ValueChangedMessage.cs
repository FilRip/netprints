namespace Microsoft.Toolkit.Mvvm.Messaging.Messages
{
    public class ValueChangedMessage<T>
    {
        public T Value { get; }

        public ValueChangedMessage(T value)
        {
            Value = value;
        }
    }
}
