namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading.Tasks;
    using Changify;
    using static Changify.DelayChangeTokenProducer;

    public static class ResourceConsumingChangeTokenProducerExtensions
    {

        /// <summary>
        /// Include a producer that will filter out <see cref="IChangeToken"/> signals, if an `IDisposable` resource cannot successfully be acquired at the point a signal occurs.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ChangeTokenProducerBuilder FilterOnResourceAcquired(
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
