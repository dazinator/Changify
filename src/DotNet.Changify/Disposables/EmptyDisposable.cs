namespace System
{
    /// <summary>
    /// A disposable that does nothing.
    /// </summary>
    internal class EmptyDisposable : IDisposable
    {
        internal static EmptyDisposable Instance { get; } = new EmptyDisposable();

        private EmptyDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
