namespace Microsoft.Extensions.Primitives
{
    using System;
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
        private Task innerListeningTask = null;
        private readonly object _lock = new object();

        /// <summary>
        /// Produce a change token that will filter an inner change token signal, so that a signal is only carried if a delegate check returns true.
        /// </summary>
        /// <returns></returns>
        public IChangeToken Produce()
        {
            var newToken = new TriggerChangeToken();
            if (innerListeningTask == null)
            {
                lock (_lock)
                {
                    if (innerListeningTask == null)
                    {
                        innerListeningTask = ListenOnInnerChangeAsync(() => newToken.Trigger(), _onCheckFailed);
                    }
                }
            }

            return newToken;
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
                    // culd not acquire resource, change filtered out.
                    onSuccess?.Invoke();
                    return;
                }

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
