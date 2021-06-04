namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Changify;
    using Microsoft.Extensions.Primitives;
    using Xunit;

    public partial class ResourceConsumingChangeTokenProducerTests
    {

        [Fact]
        public async Task Only_Signals_When_Resource_Acquired()
        {

            int concurrentCount = 3;
            var couldNotRunCounter = new CountdownEvent(concurrentCount - 1);
            var ranCounter = new CountdownEvent(1);

            bool lockDisposed = false;

            var suts = new List<ResourceConsumingChangeTokenProducer>();
            var triggers = new List<Action>();

            var testLockProvider = new TestLockProvider(() =>
            {
                lockDisposed = true;
            });

            var sutTasks = new List<Task>();
            ResourceConsumingChangeTokenProducer signalledSut = null;
            /// Create multiple token producers that will each try to acquire the lock when inner token signalled. Only the one that gets the lock
            /// should then signal.
            for (int i = 0; i < concurrentCount; i++)
            {
                var sut = CreateSut(couldNotRunCounter, testLockProvider, out var trigger);
                suts.Add(sut);
                triggers.Add(trigger);

                var sutTask = sut.WaitOneAsync().ContinueWith(async (t) =>
                 {
                     // We are expecting only one task to enter here, as this runs after a token has been signalled,
                     // // and the only one that should be signalled, is the one that can acquire the lock.
                     signalledSut = sut;
                     ranCounter.Signal();
                     await Task.Delay(3000); // simulate some work that we would do after being signalled.
                 });

                sutTasks.Add(sutTask);
            }


            /// Trigger all the inner token producers, this will cause our sut's to all try and acquire the lock at the same time.
            var triggerTasks = triggers.Select(t => Task.Run(
                () =>
                {
                    t();
                }));
            ;
            await Task.WhenAll(triggerTasks);
            var sutTasksWithTimeout = new List<Task>();
            sutTasksWithTimeout.AddRange(sutTasks);
            sutTasksWithTimeout.Add(Task.Delay(TimeSpan.FromSeconds(10)));

            await Task.WhenAny(sutTasksWithTimeout);


            // await Task.Delay(5000);

            Assert.True(ranCounter.IsSet);
            Assert.True(couldNotRunCounter.IsSet);

            // when we wait for the next token, it should release the lock.
            _ = signalledSut.WaitOneAsync();

            Assert.True(lockDisposed);
        }

        private static ResourceConsumingChangeTokenProducer CreateSut(CountdownEvent couldNotRunCounter, TestLockProvider lockProvider, out Action trigger)
        {
            var innerBuilder = new ChangeTokenProducerBuilder();
            innerBuilder.IncludeTrigger(out trigger);
            var innerProducer = innerBuilder.Build();

            var sut = new ResourceConsumingChangeTokenProducer(innerProducer, acquire: async () =>
            {
                var acquiredLock = await lockProvider.TryAcquireAsync();
                return acquiredLock;

            }, () =>
            {
                couldNotRunCounter.Signal();
            });
            return sut;
        }
    }
}
