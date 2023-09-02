using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Microsoft.Toolkit.Mvvm.Messaging.Messages
{
    public class CollectionRequestMessage<T> : IEnumerable<T>, IEnumerable
    {
        private readonly List<T> responses = new();

        public IReadOnlyCollection<T> Responses => responses;

        public void Reply(T response)
        {
            responses.Add(response);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerator<T> GetEnumerator()
        {
            return responses.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
