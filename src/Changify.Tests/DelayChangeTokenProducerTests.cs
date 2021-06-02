namespace Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Changify;
    using Xunit;
    using static Changify.DelayChangeTokenProducer;

    public class DelayChangeTokenProducerTests
    {
        [Fact]
        public async Task Can_Signal_After_Delay()
        {

            int counter = 0;
            var producer = new DelayChangeTokenProducer(async () =>
            {
                counter = counter + 1;
                var delayInfo = new DelayInfo(TimeSpan.FromMilliseconds(200), CancellationToken.None);
                return delayInfo;
            });

            bool signalled = false;
            var token = producer.Produce();
            var listening = token.RegisterChangeCallback((s) =>
            {
                signalled = true;
            }, null);

            await Task.Delay(300);
            Assert.True(signalled);

            listening.Dispose();


            signalled = false;
            token = producer.Produce();
            listening = token.RegisterChangeCallback((s) =>
            {
                signalled = true;
            }, null);

            await Task.Delay(300);
            Assert.True(signalled);

            Assert.Equal(2, counter);

            listening.Dispose();
        }

    }
}
