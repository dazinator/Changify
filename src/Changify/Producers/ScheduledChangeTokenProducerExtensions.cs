namespace Microsoft.Extensions.Primitives
{

    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Changify;

    public static class ScheduledChangeTokenProducerExtensions
    {
        /// <summary>
        ///  Include a producer that will signal it's change tokens at a specified datetime.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="getNextOccurrence">A func that can be called to obtain the Datetime that the current <see cref="IChangeToken"/> should be signalled.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort any asynchrnous delay that might be ongoing when waiting for the datetime to be reached to signal the current change token.</param>
        /// <returns></returns>
        public static ChangeTokenProducerBuilder IncludeDatetimeScheduledTokenProducer(
        this ChangeTokenProducerBuilder builder,
       Func<Task<DateTime?>> getNextOccurrence, CancellationToken cancellationToken)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var producer = new ScheduledChangeTokenProducer(getNextOccurrence, cancellationToken);
            return builder.Include(producer);
        }
    }
}
