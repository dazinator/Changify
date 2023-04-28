namespace Tests
{
    using System;
    using System.Threading.Tasks;

    public class TestLockProvider : IResourceProvider
    {
        private readonly Action _onLockDisposed;
        private IDisposable _acquiredLock = null;
        private readonly object _lock = new object();
        //  private bool _acquired = false;
        private readonly Task<IDisposable> _nullLock = Task.FromResult<IDisposable>(null);

        public TestLockProvider(Action onLockDisposed) => _onLockDisposed = onLockDisposed;
        public Task<IDisposable> TryAcquireAsync()
        {
            if (_acquiredLock != null)
            {
                // lock already taken by something.
                return _nullLock;
            }

            lock (_lock)
            {
                if (_acquiredLock != null)
                {
                    // lock already taken by something.
                    return _nullLock;
                }

                _acquiredLock = new InvokeOnDispose(() =>
                {
                    ReleaseLock();
                    _onLockDisposed();
                });

                return Task.FromResult(_acquiredLock);
            }
        }

        private void ReleaseLock()
        {
            lock (_lock)
            {
                _acquiredLock = null;
            }
        }
    }
}
