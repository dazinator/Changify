namespace Changify
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Changify.Utils;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    /// A producer of <see cref="IChangeToken"/>'s that is able to signal each one based on an asynchronous delay. As each token is produced, a delegate is asynchronosuly invoked to obtain the delay information used to signal that token.
    /// </summary>
    public class DelayChangeTokenProducer : IChangeTokenProducer
    {
        private readonly Func<Task<DelayInfo>> _getNextDelayInfo;

        public DelayChangeTokenProducer(Func<Task<DelayInfo>> getNextDelayInfo)
        {
            _getNextDelayInfo = getNextDelayInfo;
        }

        public IChangeToken Produce()
        {
            var delayTask = _getNextDelayInfo?.Invoke();
            if (delayTask == null)
            {
                return EmptyChangeToken.Instance;
            }

           // var delay = await delayTask;

            var token = new TriggerChangeToken();

            _ = Task.Run(async () =>
            {
                var delay = await delayTask;
                var totalMs = (long)delay.DelayFor.TotalMilliseconds;
                await LongDelay.For(totalMs, delay.DelayCancellationToken);
                if (!delay.DelayCancellationToken.IsCancellationRequested)
                {
                    token.Trigger();
                }
            });

            return token;
        }

        /// <summary>
        /// Information about a specific delay that will be used to signal an <see cref="Microsoft.Extensions.Primitives.IChangeToken"/>
        /// </summary>
        public class DelayInfo
        {
            public DelayInfo(TimeSpan delayFor, CancellationToken delayCancellationToken)
            {
                DelayFor = delayFor;
                DelayCancellationToken = delayCancellationToken;
            }

            /// <summary>
            /// A <see cref="CancellationToken"/> that can be used to abort the delay.
            /// </summary>
            public CancellationToken DelayCancellationToken { get; set; }
            public TimeSpan DelayFor { get; set; }
        }
    }
}
