namespace Tests
{
    using System;
    using System.Threading.Tasks;

    public interface IResourceProvider
    {
        Task<IDisposable> TryAcquireAsync();
    }
}
