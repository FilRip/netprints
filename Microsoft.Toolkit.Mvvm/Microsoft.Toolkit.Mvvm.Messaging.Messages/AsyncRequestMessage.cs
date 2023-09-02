using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging.Messages
{
    public class AsyncRequestMessage<T>
    {
        private Task<T>? response;

        public Task<T>? Response
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
            Reply(Task.FromResult(response));
        }

        public void Reply(Task<T> response)
        {
            if (HasReceivedResponse)
            {
                ThrowInvalidOperationExceptionForDuplicateReply();
            }
            HasReceivedResponse = true;
            this.response = response;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public TaskAwaiter<T> GetAwaiter()
        {
            return Response.GetAwaiter();
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
