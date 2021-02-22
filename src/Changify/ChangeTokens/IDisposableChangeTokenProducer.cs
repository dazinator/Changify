using System;

namespace Microsoft.Extensions.Primitives
{
    public interface IDisposableChangeTokenProducer : IChangeTokenProducer, IDisposable
    {
    }
}
