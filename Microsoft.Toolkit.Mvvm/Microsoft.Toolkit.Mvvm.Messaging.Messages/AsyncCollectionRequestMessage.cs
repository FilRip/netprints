using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Toolkit.Mvvm.Messaging.Messages
{
    public class AsyncCollectionRequestMessage<T> : IAsyncEnumerable<T>
    {
        private readonly List<(Task<T>?, Func<CancellationToken, Task<T>>?)> responses = new();

        private readonly CancellationTokenSource cancellationTokenSource = new();

        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public void Reply(T response)
        {
            Reply(Task.FromResult(response));
        }

        public void Reply(Task<T> response)
        {
            responses.Add((response, null));
        }

        public void Reply(Func<CancellationToken, Task<T>> response)
        {
            responses.Add((null, response));
        }

        public async Task<IReadOnlyCollection<T>> GetResponsesAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(cancellationTokenSource.Cancel);
            }
            List<T> results = new(responses.Count);
            await foreach (T item in this.WithCancellation(cancellationToken).ConfigureAwait(continueOnCapturedContext: false))
            {
                results.Add(item);
            }
            return results;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(cancellationTokenSource.Cancel);
            }
            foreach (var (task, func) in responses)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }
                if (task != null)
                {
                    yield return await task.ConfigureAwait(continueOnCapturedContext: false);
                }
                else if (func != null)
                {
                    yield return await func(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
                }
            }
        }
    }
}
