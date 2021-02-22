using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Implements <see cref="IChangeToken"/>
    /// </summary>
    public class TriggerChangeToken : IChangeToken, IDisposable
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _disposedValue;
        private IDisposable _registration = null;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TriggerChangeToken() => _registration = _cts.Token.Register((Action)(() => this.HasChanged = true)); // this allows HasChanged property to work even if this token gets disposed and something else keeps a reference for some reason.

        /// <summary>
        /// Indicates if this token will proactively raise callbacks. Callbacks are still guaranteed to be invoked, eventually.
        /// </summary>
        /// <returns>True if the token will proactively raise callbacks.</returns>
        public bool ActiveChangeCallbacks => true;

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
            => _cts.Token.Register(callback, state);

        /// <summary>
        /// Used to trigger the change token. Subsequent invocations do nothing, invocation after disposal does nothing.
        /// </summary>
        public void Trigger() => _cts?.Cancel();

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _registration.Dispose();
                    _cts.Dispose();
                    _cts = null;
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
