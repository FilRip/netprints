using System;

namespace Microsoft.Toolkit.Mvvm.Messaging.Messages
{
    public class RequestMessage<T>
    {
        private T response;

        public T Response
        {
            get
            {
                if (!HasReceivedResponse)
                {
                    ThrowInvalidOperationExceptionForNoResponseReceived();
                }
                return response;
            }
        }

        public bool HasReceivedResponse { get; private set; }

        public void Reply(T response)
        {
            if (HasReceivedResponse)
            {
                ThrowInvalidOperationExceptionForDuplicateReply();
            }
            HasReceivedResponse = true;
            this.response = response;
        }

        public static implicit operator T(RequestMessage<T> message)
        {
            return message.Response;
        }

        private static void ThrowInvalidOperationExceptionForNoResponseReceived()
        {
            throw new InvalidOperationException("No response was received for the given request message");
        }

        private static void ThrowInvalidOperationExceptionForDuplicateReply()
        {
            throw new InvalidOperationException("A response has already been issued for the current message");
        }
    }
}
