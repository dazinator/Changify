using System;
using System.Threading.Tasks;

namespace Tests
{
    public interface IResourceProvider
    {
        Task<IDisposable> TryAcquireAsync();
    }
}
