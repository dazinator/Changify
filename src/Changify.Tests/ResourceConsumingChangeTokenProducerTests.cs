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

            var suts = new List<IChangeTokenProducer>();
            var triggers = new List<Action>();

            var testLockProvider = new TestLockProvider(() =>
            {
                lockDisposed = true;
            });

            var sutTasks = new List<Task>();
            IChangeTokenProducer signalledSut = null;
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
            sutTasksWithTimeout.Add(Task.Delay(TimeSpan.FromSeconds(20)));

            await Task.WhenAny(sutTasksWithTimeout);


            // await Task.Delay(5000);

            Assert.True(ranCounter.IsSet);
            Assert.True(couldNotRunCounter.IsSet);

            // when we wait for the next token, it shoud make the previous token obsolete, which should release the lock / resource.
            Assert.False(lockDisposed);
            _ = signalledSut.WaitOneAsync(); // we don't need this task to finish, as simply by listening to next token it should trigger the dispose.

            await Task.Delay(100);
            Assert.True(lockDisposed);
        }

        private static IChangeTokenProducer CreateSut(CountdownEvent couldNotRunCounter, TestLockProvider lockProvider, out Action trigger)
        {
            var producer = new ChangeTokenProducerBuilder()
                .IncludeTrigger(out trigger)
                .Build()
                .FilterOnResourceAcquired(async () => await lockProvider.TryAcquireAsync(), () => couldNotRunCounter.Signal())
                .Build();

            return producer;
        }
    }
}
