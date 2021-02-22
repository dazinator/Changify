namespace Microsoft.Extensions.Primitives
{
    public interface IChangeTokenProducer
    {
        IChangeToken Produce();
    }
}
