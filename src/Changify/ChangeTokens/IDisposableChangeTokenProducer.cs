namespace Microsoft.Extensions.Primitives
{
    using System;

    public interface IDisposableChangeTokenProducer : IChangeTokenProducer, IDisposable
    {
    }
}
