## Features

Build composite `IChangeToken's using a convenient fluent / builder style api to easily incorporate various sources of change signalling, common in applications.

### Basic Usage

Use the fluent builder to build an `IChangeToken` producer that can be used in your application to supply `IChangeTokens` 
that are a composite of various sources. Should any of those sources signal, then the composite change token is signalled.

The simplest way to signal the change token is to include some `Trigger`s - which you can invoke from anywhere in your application.

Example:

```csharp
            Action triggerX = null;
            Action triggerY = null;

            Func<IChangeToken> tokenProducer = new CompositeChangeTokenFactoryBuilder()
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

## Producer Lifetime

When you `Build` the producer, you may notice you get an `out` parameter which is an `IDisposable`.

```
 Func<IChangeToken> tokenProducer = new CompositeChangeTokenFactoryBuilder()
                                    .IncludeTrigger(out triggerX)
                                    .IncludeTrigger(out triggerY)
                                    .Build(out var producerLifetime);
```

You should keep a reference to this `IDisposable` alive somewhere in your application (probably alongside the token producer itself), 
as it represents the lifetime of the producer that you built.
In the example above, this `IDisposable` does precisely nothing when disposed.
However in more advanced scenarios like the ones shown below,
some sources of change may come with `IDisposable`s that need to be disposed of when you are no longer interested in them.
For example, if you include an event handler as a source of change, then the handler needs to be removed from the event when it is no longer required. 
Disposing of the `producerLifetime` ensures any IDisposables that the token consumer no longer needs are disposed.

## What else can I do

The builder has other methods to incorporate signals from other sources and areas, common in applications.

The following will go through each one, skip to the bottom to see a code sample showing all of these methods used on a builder.

1. Include your own custom change tokens:

```csharp
 .Include(()=>new MyCustomChangeToken())

```

2. Include a producer of cancellation tokens:

```csharp
.IncludeCancellationTokens(()=>new CancellationToken())
```

3. Include a deferred trigger. A deffered trigger is similar to a normal trigger - i.e a callback that you use to invalidate change tokens, however it isn't supplied to you until the first change token is requested. This means there is no danger of using the trigger before any change tokens are in play. This also lets you defer logic until a change token is actually in play.

```csharp
 .IncludeDeferredTrigger((trigger) => trigger.Invoke()) // callback invoked to supply you a trigger once the first token is consumed. Note: logic here is synchronous and so will blocks the caller requesting the very first token.
```

4. Include a deffered asynchronouse trigger. 
Similar to a deferred trigger above, except you are given a chance to run non blocking asynchronous logic with the trigger as a seperate fire and forget async task that token consumer doesn't wait upon.
Consider the following:

```csharp
 .IncludeDeferredAsyncTrigger(async (trigger) => {
                                        await Task.Delay(200);
                                        trigger();
                                        await Task.Delay(500);
                                        trigger();
                                    })
 ```

 When the first token is consumed, the above async callback will also be fired as a "fire and forget" task by the token producer.
 You can run any async logic and use the trigger to signal appropriately.
 In this example, we wait 200 ms and then trigger the change token, then wait another 500ms and trigger it again.
 This could be quite powerful if you wanted to signal the consumer peridocally based on some work happening in tandem.

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

To include this event, note we must provide an `addHandler` callback to add / attach the event handler, as well as another to remove
the handler. Internally an IDisposable is created that will call the `removeHandler` callback when the token producer lifetime IDisposable is disposed.

6. Include a trigger that gets fired when a subscribed callback is fired.

Some api's - like the `IOptionsMonitor.OnChange()` api, adopt a pattern where you register a callback and keep hold of an `IDisposable` representing that registration.
It will then invoke the callback to notify you of some change, until you indicate you are no longer interested, by disposing of the subscription.

To use these style api's to trigger change tokens you can use this convenience api:

```csharp
IncludeSubscribingHandlerTrigger((trigger)=> monitor.OnChange((o,n)=> trigger()))

```

Internally the `IDisposable` representing the OnChange callback registration is incorporated into the token producers lifetime `IDisposable`.

7. Include a trigger that gets fired when a subscribed callback is fired, that then unsubscribes and re-subscribes the callback afresh.

In the following example, the `IOptionsMonitor.OnChange()` method is used to subscribe a callback. When this callback is invoked, the change token is signalled. The subscription is then disposed so options monitor removes the callback. The delegate is then invoked again to subscribe a new callback to the monitor.OnChange() method.
This might be useful if you prefer to do some brief logic here per change token, as opposed to setting up the callback subscription once.

```csharp
.IncludeResubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
```


### It passes

Just showing the api surface of the builder, and showing the async task trigger in action too..

```csharp

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

            Func<IChangeToken> tokenProducer = new CompositeChangeTokenFactoryBuilder()
                                    .IncludeTrigger(out triggerX)
                                    .IncludeTrigger(out triggerY)
                                    .Include(() => new TriggerChangeToken())
                                    .IncludeCancellationTokens(() => new CancellationToken())
                                    .IncludeDeferredTrigger((trigger) => trigger.Invoke())
                                    .IncludeDeferredAsyncTrigger(async (trigger) =>
                                    {
                                        await Task.Delay(200);
                                        trigger();
                                        await Task.Delay(500);
                                        trigger();
                                    })
                                    .IncludeEventHandlerTrigger<string>(
                                        addHandler: (handler) => SomeEvent += handler,
                                        removeHandler: (handler) => SomeEvent -= handler,
                                        (disposable) => subscription = disposable)
                                    .IncludeSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
                                    .IncludeResubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) => trigger()))
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




