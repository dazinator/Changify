namespace Changify
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// A producer of <see cref="IChangeToken"/>'s that wrap an inner change token that will only pass through a signal if an <see cref="IDisposable"/> resource can successfully be aquired, such as a distributed lock etc.
    /// The disposable resource is disposed when the next token is requested.
    /// </summary>
    /// <remarks>
    /// Imagine multiple seperate processes that all have some <see cref="IChangeToken"/> triggered at the same time - perhaps they are monitoring the same file.
    /// You want one of the processes to start doing some processing, and not the other processes.
    /// You can use this producer to wrap the inner change tokens, so that the process that successfully aquires the distributed lock will have its change token signalled, where as the other processes that cannot aquire the lock, do not.
    /// This results in a token that should only get signalled in one of the processes and not the others.
    /// </remarks>
    public class ResourceAquiringChangeTokenProducer : IDisposableChangeTokenProducer
    {
        private readonly IChangeTokenProducer _innerProducer;
        private readonly Func<Task<IDisposable>> _acquire;

        public ResourceAquiringChangeTokenProducer(IChangeTokenProducer innerProducer, Func<Task<IDisposable>> acquire)
        {
            _innerProducer = innerProducer;
            _acquire = acquire;            
        }

        private IDisposable _lastResource = null;
        private bool _disposedValue;
        private Task innerListeningTask = null;


        private DisposableResourceChangeToken _lastToken;

        public IChangeToken Produce()
        {

            DisposableResourceChangeToken currentToken = null;
            DisposableResourceChangeToken result()
            {
                // consumer is asking for a new token, any previous token is dead.
              //  var innerToken = _innerProducer.Produce();
               
                var newToken = new DisposableResourceChangeToken();
                var previous = Interlocked.Exchange(ref currentToken, newToken);
                previous?.Dispose(); // ensures any previously acquired resource is disposed.
                return currentToken; 
            }
            var newToken = result();
            if(innerListeningTask == null)
            {
                // Listen to inner tokens one at a time, each time a change is detected, try acquire the resource and if acquired, trigger our token.
                // this will cause ChaneToken.OnChange() to obtain a new token by calling Produce() again, and we will dispose of the previous tokens resource.
                // whch is nto what we want:TODO: FIX
                // FIX BY: When new token has a callback registered, dispose of previous tokens resource.
                //    ChangeToken.OnChange registers callback on new tokens AFTER invoking the callback so this would work.

                innerListeningTask = ListenOnInnerChangeAsync((resource) => currentToken.Trigger(resource));
            }      
                      

            return newToken;
        }

        public async Task ListenOnInnerChangeAsync(Action<IDisposable> onResourceAcquired)
        {
            while(!_disposedValue)
            {
                // WaitOne might miss changes that happen before after it returns and before we call the next WaitOneAsync call..
                // however for things like scheduled jobs, this is ok.
                _ = _innerProducer.WaitOneAsync().ContinueWith(async (t) =>
                {
                    if (!t.IsCanceled)
                    {
                        var newResouce = await _acquire();
                      
                      //  _lastResource = newResouce;
                        if (newResouce != null)
                        {
                            onResourceAcquired?.Invoke(newResouce);                           
                        }
                        else
                        {
                            // culd not acquire resource, change filtered out.
                        }
                    }
                    else
                    {
                        // task to wait for inner change token to signal has been cancelled.

                    }
                });
            }
          
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (_lastResource != null)
                    {
                        _lastResource?.Dispose();
                        _lastResource = null;
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
