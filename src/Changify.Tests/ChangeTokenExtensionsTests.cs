namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Primitives;
    using Xunit;

    public class ChangeTokenExtensionsTests
    {
        [Fact]
        public async Task WaitOneAsync_WhenTokenNotSignalled_RemainsActive()
        {

            // arrange
            var manualToken = new TriggerChangeToken();
            var producer = new Func<IChangeToken>(() => manualToken);

            // act
            var task = producer.WaitOneAsync();
            await Task.Delay(100);

            // assert
            Assert.False(task.IsCompleted);
            Assert.False(task.IsCanceled);

        }

        [Fact]
        public async Task WaitOneAsync_WhenTokenSignalled_Completes()
        {

            // arrange
            var manualToken = new TriggerChangeToken();
            var producer = new Func<IChangeToken>(() => manualToken);

            // act
            var task = producer.WaitOneAsync();
            manualToken.Trigger();
            await task;

            Assert.True(task.IsCompleted);
            Assert.False(task.IsCanceled);
        }

        [Fact]
        public async Task OnChange_WhenTokenSignalled_InvokesAsyncCallback()
        {

            // arrange
            var tokens = new List<TriggerChangeToken>
            {
                new TriggerChangeToken(),
                new TriggerChangeToken()
            };

            var index = 0;
            var producer = new Func<IChangeToken>(() =>
            {
                var token = tokens[Interlocked.Increment(ref index) - 1];
                return token;
            });


            var signalled = new AutoResetEvent(false);
            // act
            producer.OnChange(async () => signalled.Set());

            tokens[0].Trigger();

            Assert.True(signalled.WaitOne(500));
        }

        [Fact]
        public async Task OnChange_WhenAsyncCallbackThrows_ExceptionIsNotSwallowed()
        {
            var signalled = new AutoResetEvent(false);

            TaskScheduler.UnobservedTaskException += (s, e) => signalled.Set();

            // arrange
            var tokens = new List<TriggerChangeToken>
            {
                new TriggerChangeToken(),
                new TriggerChangeToken()
            };

            var index = 0;
            var producer = new Func<IChangeToken>(() =>
            {
                var token = tokens[Interlocked.Increment(ref index) - 1];
                return token;
            });

            // act
            // SEE https://stackoverflow.com/questions/5734121/how-to-detect-unobservedtaskexception-errors-in-nunit-test-suites
            CauseATaskToThrowInTheSystemUnderTest(producer, tokens[0]);

            // This task delay appears to be necessary to get UnobservedTaskException to be raised above^^ - not sure why at present.
            await Task.Delay(10);

            for (var i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            Assert.True(signalled.WaitOne(200));
        }

        private void CauseATaskToThrowInTheSystemUnderTest(Func<IChangeToken> producer, TriggerChangeToken trigger)
        {
            producer.OnChange(async () => throw new Exception("boo"));
            trigger.Trigger();
        }

        [Fact]
        public async Task OnRegistrationDisposed_NoLongerInvokesCallbacks()
        {
            var signalled = new AutoResetEvent(false);
            // arrange
            var tokens = new List<TriggerChangeToken>
            {
                new TriggerChangeToken(),
                new TriggerChangeToken()
            };

            var index = 0;
            var producer = new Func<IChangeToken>(() =>
            {
                var token = tokens[Interlocked.Increment(ref index) - 1];
                return token;
            });

            // act
            var registration = producer.OnChange(async () => signalled.Set());

            tokens[0].Trigger();

            // manualToken.Trigger();
            signalled.WaitOne(500);

            //signalled = new AutoResetEvent(false);
            signalled.Reset();

            registration.Dispose();
            tokens[1].Trigger();

            Assert.False(signalled.WaitOne(500));

        }

    }
}
