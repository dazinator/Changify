namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading.Tasks;
    using Changify;
    using static Changify.DelayChangeTokenProducer;

    public static class DelayChangeTokenProducerExtensions
    {

        /// <summary>
        /// Include a producer that will signal change tokens after a supplied delay.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getNextDelayInfo"></param>
        /// <returns></returns>
        public static ChangeTokenProducerBuilder IncludeDelayTokenProducer(
          this ChangeTokenProducerBuilder builder,
         Func<Task<DelayInfo>> getNextDelayInfo)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            IChangeTokenProducer producer = new DelayChangeTokenProducer(async () => await getNextDelayInfo());
            return builder.Include(producer);
        }
    }
}
