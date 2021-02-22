namespace Microsoft.Extensions.Primitives
{
    using System;

    public class DisposableChangeTokenProducer : IDisposableChangeTokenProducer
    {
        private bool _disposedValue;
        private readonly Func<IChangeToken> _producer;
        private readonly IDisposable _lifetime;

        public DisposableChangeTokenProducer(
            Func<IChangeToken> producer,
            IDisposable lifetime)
        {
            _producer = producer;
            _lifetime = lifetime;
        }

        public IChangeToken Produce() => _producer?.Invoke() ?? EmptyChangeToken.Instance;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _lifetime?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~DisposableChangeTokenProducer()
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
