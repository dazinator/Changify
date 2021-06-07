namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading.Tasks;
    using Changify;

    public static class ResourceConsumingChangeTokenProducerExtensions
    {

        /// <summary>
        /// A producer that wraps an inner producer, and will try to acquire an <see cref="IDisposable"/> resource when the inner producer signals. If it can't obtain the resource it won't signal a change token. If it can then it will. The resource is held until the token becomes obsolete.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ChangeTokenProducerBuilder AndResourceAcquired(
          this IChangeTokenProducer inner,
          Func<Task<IDisposable>> acquireResourceAsync,
          Action onAcquireFailed
          )
        {
            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            //  var inner = builder.Build();
            var newProducer = new ResourceConsumingChangeTokenProducer(inner, acquire: acquireResourceAsync, onAcquireFailed);
            var newBuilder = new ChangeTokenProducerBuilder().Include(newProducer);
            return newBuilder;
        }
    }
}
