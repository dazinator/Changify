namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading;

    /// <summary>
    /// An <see cref="IChangeToken"/> that is triggered in response to obtaining a disposable resource, which is then disposed of when the token is disposed.
    /// Generally the token is disposed when a callback is registered with the next token, rendering the previous token obsolete, and this this token disposes of previous token when first callback is registred. This causes any acquired disposable resource to be disposed at that point.
    /// </summary>
    public class ResourceConsumingChangeToken : IChangeToken, IDisposable
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposedValue;
        private readonly IDisposable _registration = null;
        private IDisposable _resource = null;
        private bool _hasCallbacksRegistered = false;

        // private Action  _onFirstCallbackRegistered = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ResourceConsumingChangeToken() => _registration = _cts.Token.Register((Action)(() => this.HasChanged = true)); // this allows HasChanged property to work even if this token gets disposed and something else keeps a reference for some reason.// _onFirstCallbackRegistered = onFirstCallbackRegistered;

        /// <summary>
        /// Indicates if this token will proactively raise callbacks. Callbacks are still guaranteed to be invoked, eventually.
        /// </summary>
        /// <returns>True if the token will proactively raise callbacks.</returns>
        public bool ActiveChangeCallbacks => true;

        public IDisposable PreviousToken { get; set; }

        /// <summary>
        /// Gets a value that indicates if a change has occurred.
        /// </summary>
        /// <returns>True if a change has occurred.</returns>
        public bool HasChanged { get; private set; } = false;

        /// <summary>
        /// Registers for a callback that will be invoked when the entry has changed. <see cref="Microsoft.Extensions.Primitives.IChangeToken.HasChanged"/>
        /// MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="state">State to be passed into the callback.</param>
        /// <returns>The <see cref="CancellationToken"/> registration.</returns>
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            // since we are not listening to this new token, we can dispose any resource used by old token.
            if (!_hasCallbacksRegistered)
            {
                _hasCallbacksRegistered = true;
                PreviousToken?.Dispose();
                PreviousToken = null;
                //_onFirstCallbackRegistered?.Invoke();
            }

            return _cts.Token.Register(callback, state);
        }

        /// <summary>
        /// Used to trigger the change token. Subsequent invocations do nothing, invocation after disposal does nothing.
        /// </summary>
        public void Trigger(IDisposable acquiredResource)
        {
            _resource = acquiredResource;
            _cts?.Cancel();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _registration.Dispose();
                    _cts.Dispose();
                    _cts = null;
                    _resource?.Dispose();
                    _resource = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~TriggerChangeToken()
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
