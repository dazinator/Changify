namespace Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Changify;
    using Microsoft.Extensions.Primitives;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Xunit;
    using static Changify.DelayChangeTokenProducer;

    public class ResourceAcquiringChangeTokenProducerTests
    {

        public class TestResourceLock : IDisposable
        {
            private bool _disposedValue;
            private readonly Action _onDispose;

            public TestResourceLock(Action onDispose)
            {
                _onDispose = onDispose;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!_disposedValue)
                {
                    if (disposing)
                    {
                        _onDispose?.Invoke();
                        // TODO: dispose managed state (managed objects)
                    }

                    // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                    // TODO: set large fields to null
                    _disposedValue = true;
                }
            }

            // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
            // ~TestResourceLock()
            // {
            //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //     Dispose(disposing: false);
            // }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }

        [Fact]
        public async Task Signals_When_Lock_Aquired()
        {

            var innerBuilder = new ChangeTokenProducerBuilder();
            innerBuilder.IncludeTrigger(out var triggerA);
            var innerProducer = innerBuilder.Build();

            var distributedLock = new TestResourceLock(onDispose: () =>
            {

            });

            TestResourceLock currentDistributedLock = null;

            var sut = new ResourceAquiringChangeTokenProducer(innerProducer, acquire: async () =>
            {
                var replaced = Interlocked.CompareExchange(ref currentDistributedLock, distributedLock, null);
                if (replaced == null)
                {
                    // successfully obtained the distributed lock.
                    return distributedLock;
                }
                // couldn't acquire the lock, some other process / thread already grabbed it..
                return null;
            });


            var signalCount = 0;
            var waitForSignalTask = sut.WaitOneAsync().ContinueWith((t) =>
            {
                Interlocked.Increment(ref signalCount);               
            });

            await Task.Delay(100);
            triggerA();

            await waitForSignalTask;
            Assert.Equal(signalCount, 1);



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
