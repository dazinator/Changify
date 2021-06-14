namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A producer of <see cref="IChangeToken"/>'s that wraps an inner change token that will only pass through a signal if a give <see cref="Func{Task{bool}}"/> returns true.
    /// </summary>
    public class WhenTrueAsyncChangeTokenProducer : IDisposableChangeTokenProducer
    {
        private readonly IChangeTokenProducer _innerProducer;
        private readonly Func<Task<bool>> _check;
        private readonly Action _onCheckFailed;

        public WhenTrueAsyncChangeTokenProducer(
            IChangeTokenProducer innerProducer,
            Func<Task<bool>> check,
            Action onCheckFailed)
        {
            _innerProducer = innerProducer;
            _check = check;
            _onCheckFailed = onCheckFailed;
        }

        private bool _disposedValue;
        private IChangeToken _currentToken;

        /// <summary>
        /// Produce a change token that will filter an inner change token signal, so that a signal is only carried if a delegate check returns true.
        /// </summary>
        /// <returns></returns>
        public IChangeToken Produce()
        {
            TriggerChangeToken getNewToken()
            {
                // consumer is asking for a new token, any previous token is dead.
                //  var innerToken = _innerProducer.Produce();
                var newToken = new TriggerChangeToken();
                var previousToken = Interlocked.Exchange(ref _currentToken, newToken);
                // listen until inner signal produces a successful result, then trigger this token.
                _ = ListenOnInnerChangeAsync(() => newToken.Trigger(), _onCheckFailed);
                return newToken;
            }

            return getNewToken();
        }


        public async Task ListenOnInnerChangeAsync(Action onSuccess, Action onFailed)
        {

            while (!_disposedValue)
            {
                // WaitOne might miss changes that happen before after it returns and before we call the next WaitOneAsync call..
                // however for things like scheduled jobs, this is ok.
                await _innerProducer.WaitOneAsync();
                var task = _check();
                if (task == null)
                {
                    throw new InvalidOperationException();
                }

                var success = await task;
                if (success)
                {
                    // token consumed duccessfully.
                    onSuccess?.Invoke();
                    return;
                }

                // Keep consuming (change filtered out).
                onFailed?.Invoke();
                continue;
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    if (_innerProducer is IDisposable innerDisposable)
                    {
                        innerDisposable.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ResourceAquiringChangeTokenProducer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
