namespace Tests
{
    using System;
    using System.Collections.Generic;
    using System.Formats.Asn1;
    using System.Linq.Expressions;
    using System.Reflection.Metadata.Ecma335;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Primitives;
    using Xunit;
    using static Changify.DelayChangeTokenProducer;

    public class ChangeTokenFactoryBuilderTests
    {
        [Fact]
        public void EmptyBuilder_Builds_EmptyChangeTokenFactory()
        {
            var sut = new ChangeTokenProducerBuilder();
            var factory = sut.Build(out var lifetime);

            Assert.NotNull(factory);
            var tokenA = factory();
            var tokenB = factory();
            Assert.Same(tokenB, tokenA);
            Assert.True(tokenA.ActiveChangeCallbacks);
            Assert.False(tokenA.HasChanged);
            tokenA.RegisterChangeCallback((a) => throw new Exception("this should never fire."), null);
        }

        [Fact]
        public void Include_ChangeToken()
        {
            var sut = new ChangeTokenProducerBuilder();
            TriggerChangeToken token = null;
            var factory = sut.Include(() =>
            {
                token = new TriggerChangeToken();
                return token;
            }).Build(out var lifetime);

            var consumed = factory();
            Assert.Same(token, consumed);

            // When we trigger the token and request a new one,
            //we get a new one thats different from the previous one.
            IChangeToken newToken = null;
            IChangeToken original = token;
            token.RegisterChangeCallback(a => newToken = factory(), null);

            token.Trigger();
            // await Task.Delay(200);

            Assert.NotNull(newToken);
            Assert.NotSame(newToken, original);
        }

        [Fact]
        public void Include_ManyChangeTokens_SignallingAny_SignalsConsumedToken()
        {
            var sut = new ChangeTokenProducerBuilder();

            var tokensProduced = new List<TriggerChangeToken>();

            var factory = sut.Include(() =>
            {
                var token = new TriggerChangeToken();
                tokensProduced.Add(token);
                return token;
            }).Include(() =>
            {
                var token = new TriggerChangeToken();
                tokensProduced.Add(token);
                return token;
            }).Build(out var lifetime);

            var consumedCompositeToken = factory();

            Assert.False(consumedCompositeToken.HasChanged);
            Assert.Equal(2, tokensProduced.Count);

            // When we trigger either token, it signales the composite token 
            var tokenOne = tokensProduced[0];
            tokenOne.Trigger();
            Assert.True(consumedCompositeToken.HasChanged);

            tokensProduced.Clear();
            consumedCompositeToken = factory();
            Assert.False(consumedCompositeToken.HasChanged);
            Assert.Equal(2, tokensProduced.Count);

            var tokenTwo = tokensProduced[1];
            tokenTwo.Trigger();
            Assert.True(consumedCompositeToken.HasChanged);

            // we can tell which individual token in the composite changed.
            var innerChangedTokenDetected = false;
            if (consumedCompositeToken is CompositeChangeToken composite)
            {
                // determine which token caused the signalling.
                foreach (var individualToken in composite.ChangeTokens)
                {
                    if (individualToken.HasChanged)
                    {
                        innerChangedTokenDetected = true;
                    }
                }
            }
            Assert.True(innerChangedTokenDetected);

        }


        [Fact]
        public void Readme_Sample()
        {

            var tokenProducer = new ChangeTokenProducerBuilder()
                                    .IncludeTrigger(out var triggerX)
                                    .IncludeTrigger(out var triggerY)
                                    .Build(out var lifetime);

            var signalled = false;
            ChangeToken.OnChange(tokenProducer, () => signalled = true);

            triggerX();
            Assert.True(signalled);

            signalled = false;
            triggerY();
            Assert.True(signalled);

            signalled = false;
            triggerX(); // Triggers remain good for the lifetime of token producer.
            Assert.True(signalled);
        }

        public event EventHandler<string> SomeEvent;

        public class FooOptions
        {

        }

        [Fact]
        public async Task Readme_Advanced_Compiles()
        {
            IDisposable subscription = null;

            var monitor = new ServiceCollection()
                                                        .AddOptions()
                                                        .Configure<FooOptions>((o) => { })
                                                        .BuildServiceProvider()
                                                        .GetRequiredService<IOptionsMonitor<FooOptions>>();

            var manualTrigger = new CountdownEvent(2);

            var tokenProducer = new ChangeTokenProducerBuilder()
                                    .IncludeTrigger(out var triggerX)
                                    .IncludeTrigger(out var triggerY)
                                    .Include(() => new TriggerChangeToken())
                                    .IncludeCancellationTokens(() => new CancellationToken())
                                    .IncludeDeferredTrigger((trigger) => trigger.Invoke())
                                    .IncludeDeferredAsyncTrigger(async (trigger) =>
                                    {
                                        await Task.Delay(200);
                                        trigger();
                                        manualTrigger.Signal();
                                        await Task.Delay(500);
                                        trigger();
                                        manualTrigger.Signal();
                                    })
                                    .IncludeDeferredEventHandlerTrigger<string>(
                                        addHandler: (handler) => SomeEvent += handler,
                                        removeHandler: (handler) => SomeEvent -= handler,
                                        (disposable) => subscription = disposable)
                                    .IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
                                    .IncludeDeferredResubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
                                    .IncludeDelayTokenProducer(async () =>                                         // configure a delay for the current change token to be signalled - this delegate fires each time a new token is produced.                                    
                                        new DelayInfo(TimeSpan.FromSeconds(10), CancellationToken.None))
                                    .IncludeDatetimeScheduledTokenProducer(async () =>
                                        // return a datetime for when the current change token is to be signalled - this delegate fires each time a new token is produced.     
                                        DateTime.UtcNow.AddSeconds(25), CancellationToken.None)
                                    .Build()
                                    .AndResourceAcquired(() => Task.FromResult<IDisposable>(EmptyDisposable.Instance), () =>
                                    {
                                        //_logger.Debug("Could not obtain resource so change ignored")
                                    })
                                    .Build()
                                    .AndTrueAsync(() => Task.FromResult(true))
                                    .Build(out var producerLifetime);


            var signalled = false;
            ChangeToken.OnChange(tokenProducer,
                () => signalled = true);

            manualTrigger.Wait(2000);
            //await Task.Delay(300);
            Assert.True(signalled);
        }


    }
}
