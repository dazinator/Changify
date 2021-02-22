[![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Changify?branchName=develop)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=18&branchName=develop)
## The Problem

Change Token's are a primitive in the modern `dotnet` stack, that are used to signal changes to consumers. 
`Microsoft` provides the convenient `ChangeToken.OnChange()` api to easily subscribe to some producer of these `IChangeToken`'s 
and to have your callback invoked whenever changes are signalled - which is great.

However it can be tricky to create a reliable token producer, especially if you want to signal changes based on a variety of sources in your application.
This library can help you with that.

### Basic Usage

Let's build a simple `IChangeToken` producer with a couple of triggers.
You can invoke these to signal a change to the consumer.

Example:

```csharp
            Action triggerX = null;
            Action triggerY = null;

            Func<IChangeToken> tokenProducer = new ChangeTokenProducerBuilder()
                                    .IncludeTrigger(out triggerX)
                                    .IncludeTrigger(out triggerY)
                                    .Build(out var producerLifetime);

            var signalled = false;
            ChangeToken.OnChange(tokenProducer, () =>
            {
                signalled = true;
            });

            triggerX();
            Assert.True(signalled);

            signalled = false;
            triggerY();
            Assert.True(signalled);

            signalled = false;
            triggerX(); // Triggers remain good for the lifetime of token producer.
            Assert.True(signalled);       
```

As you can see, in this case it's relatively simple to keep a reference to the trigger somewhere, and signal the consumer when necessary.
Stay tuned however, because this is just one type of trigger and there are a lot more covered below.

## Producer Lifetime

When you `Build` the producer, you may notice you get an `out` parameter which is an `IDisposable`.

```csharp
 Func<IChangeToken> tokenProducer = new ChangeTokenProducerBuilder()
                                    .IncludeTrigger(out triggerX)
                                    .IncludeTrigger(out triggerY)
                                    .Build(out var producerLifetime);
```

You should keep a reference to this `IDisposable` alive somewhere in your application (probably alongside the token producer itself is best), 
as it represents the lifetime of the producer that you built.
In the example above, this `IDisposable` does precisely nothing when disposed..
However in more advanced scenarios like the ones shown below, disposing of this ensures that any necessary cleanup is done, for example detaching event handlers etc.

## Other types of triggers?

The builder has other methods to incorporate signals from other sources.

It's worth mentioning that most of the api's have a "deferred" flavour.

- Deferred: The callback you supply won't be executed until the very first token is consumed.

i.e the logic is deferred until first use of a token.

If you don't use the `deferred` version of the api, then the callback you supply is executed immediately instead of inline with consumption of the first token.

Skip to the bottom to see an example of the entire api surface so far.

1. Include a producer of your own custom change tokens.

```csharp
 .Include(()=>new MyCustomChangeToken())

```

2. Include a producer of cancellation tokens.
These are converted to change tokens and signalled when the cancellation token is cancelled.

```csharp
.IncludeCancellationTokens(()=>new CancellationToken())
```

3. Include a deferred trigger. 

```csharp
 .IncludeDeferredTrigger((trigger) => trigger.Invoke()) // callback invoked to supply you a trigger once the first token is consumed. Note: logic here is synchronous and so will blocks the caller requesting the very first token so be snappy.
```

4. Include a deffered asynchronouse trigger. 

Similar to a deferred trigger above, except you are given a chance to run non blocking asynchronous logic with the trigger - that won't block the consumer.

Consider the following:

```csharp
 .IncludeDeferredAsyncTrigger(async (trigger) => {
                                        await Task.Delay(200);
                                        trigger();
                                        await Task.Delay(500);
                                        trigger();
                                    })
 ```

 5. Include a trigger that gets fired when an `event` fires.

 ```csharp
 .IncludeEventHandlerTrigger<SomeEventArgs>(
                                     addHandler: (handler) => classWithEvent.SomeEvent += handler,
                                     removeHandler: (handler) => classWithEvent.SomeEvent -= handler);

```

In the above example, there is an object instance `classWithEvent` that has the following `event` that it raises:

```csharp
public event EventHandler<SomeEventArgs> SomeEvent;

```

Whenever the event is raised, the change token will be signalled. 
The `removeHandler` callback is invoked when the token producer lifetime is disposed.

6. Include a trigger that has its own `IDisposable` cleanup.

Some api's - like the `IOptionsMonitor.OnChange()` api, adopt a pattern where you register a callback and keep hold of an `IDisposable` representing that registration.
It will then invoke the callback to notify you of some change, until you indicate you are no longer interested, by disposing of the subscription.

To use these style api's to trigger change tokens you can use this convenience api:

```csharp
IncludeSubscribingHandlerTrigger((trigger)=> monitor.OnChange((o,n)=> trigger()))

```

This API let's you return the `IDisposable' so that it will be disposed when the token producer lifetime is disposed.

7. Include a trigger that has its own `IDisposable` cleanup, and should be recycled per token consumed.

This behaves in a similar way to the `IncludeSubscribingHandlerTrigger` api, except it will be invoked for each new token, and the previously returned IDiposable will be disposed before each new invocation.
This might be useful if you prefer to do some brief logic per change token.

In the following scenario, any time the options monitor raises a change it will signal the consumer.
If the consumer then requests another token, then the IDipsosable previously returned from monitor.OnChange() will be disposed causing that OnChange handler to be removed. The callback itself will then be executed again to subscribe a new handler to monitor.OnChange() and get a new IDisposable.
This process repeats.

```csharp
.IncludeResubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
```

### Just showing the api surface..

Just showing the api surface of the builder, and showing the async task trigger in action too..

```csharp
        [Fact]
        public async Task Readme_Advanced_Compiles()
        {
            Action triggerX = null;
            Action triggerY = null;
            IDisposable subscription = null;

            IOptionsMonitor<FooOptions> monitor = new ServiceCollection()
                                                        .AddOptions()
                                                        .Configure<FooOptions>((o) => { })
                                                        .BuildServiceProvider()
                                                        .GetRequiredService<IOptionsMonitor<FooOptions>>();

            Func<IChangeToken> tokenProducer = new ChangeTokenProducerBuilder()
                                    .Include(() => new TriggerChangeToken())
                                    .IncludeCancellationTokens(() => new CancellationToken()) 
                                    .IncludeTrigger(out triggerX)
                                    .IncludeDeferredTrigger((trigger) => trigger.Invoke())       
                                    .IncludeDeferredAsyncTrigger(async (trigger) =>
                                    {
                                        await Task.Delay(200);
                                        trigger();
                                        await Task.Delay(500);
                                        trigger();
                                    })
                                    .IncludeSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
                                    .IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))   
                                    .IncludeEventHandlerTrigger<string>(
                                        addHandler: (handler) => SomeEvent += handler,
                                        removeHandler: (handler) => SomeEvent -= handler)
                                    .IncludeDeferredEventHandlerTrigger<string>(
                                        addHandler: (handler) => SomeEvent += handler,
                                        removeHandler: (handler) => SomeEvent -= handler)
                                    .IncludeResubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
                                    .IncludeDeferredResubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
                                    .Build(out var producerLifetime);

            var signalled = false;
            ChangeToken.OnChange(tokenProducer, () => signalled = true);

            await Task.Delay(200);
            Assert.True(signalled);
            signalled = false;

            await Task.Delay(500);
            Assert.True(signalled);        
        }
```

## Companion packages

Just for convenience an `Changify.Configuration` and `Changify.Options` nuget package is available.
These let you also include Configuration Reloads, or Options Monitor changes more easily.

## Changify.Options

```csharp
 .IncludeOptionsChangeTrigger<MyOptions>(monitor)  // fires whenever any change for MyOptions occurs
 .IncludeOptionsChangeTrigger<MyOptions>(monitor, "Foo") // only if the options named "Foo" changes.
 .IncludeOptionsChangeTrigger<MyOptions>(monitor, "") // only if the default named options "" changes.
 .IncludeOptionsChangeTrigger<MyOptions>(monitor, (opts, name)=>{ return true; }) // use a predicate to decide.
 .IncludeOptionsChangeTrigger<MyOptions>(monitor, (opts, name, trigger)=>{ trigger(); }) // call the trigger if you want.
```

If you don't have the `IOptionsMonitor' instance handy, but you have the `IServiceProvider` - as often might be the case in startup logic, you can use the convience overloads on all of the aboce that takes the IServiceProvider rather than the options monitor.

```csharp
.IncludeOptionsChangeTrigger<MyOptions>(sp)  // fires whenever any change for MyOptions occurs
```
## Changify.Configuration

.IncludeConfigurationReloads(config); // if the config reloads..you guessed it?
