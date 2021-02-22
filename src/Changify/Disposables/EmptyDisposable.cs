namespace System
{
    /// <summary>
    /// A disposable that does nothing.
    /// </summary>
    public class EmptyDisposable : IDisposable
    {
        public static EmptyDisposable Instance { get; } = new EmptyDisposable();

        private EmptyDisposable()
        {
        }

        public void Dispose()
        {
        }
    }
}
