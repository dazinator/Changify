[![Build Status](https://darrelltunnell.visualstudio.com/Public%20Projects/_apis/build/status/dazinator.Changify?branchName=develop)](https://darrelltunnell.visualstudio.com/Public%20Projects/_build/latest?definitionId=18&branchName=develop)
## The Problem

Change Token's are a primitive in the modern `dotnet` stack, that are used to signal changes to consumers. 
`Microsoft` provides the convenient `ChangeToken.OnChange()` api to easily subscribe to some producer of these `IChangeToken`'s 
and to have your callback invoked whenever changes are signalled - which is great.

However in order to create the change token producer, it can be more tricky, especially if you want to signal changes based on a variety of events.
This library can help you with that.

### Basic Usage

Let's build a simple `IChangeToken` producer with a couple of triggers.
You can invoke these from anywhere in your application to signal a change to the consumer.

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

As you can see, suppose in this case you needed to signal the consumer if either the application config is updated,
or an api call is received - it would be fairly simple to have that code invoke a trigger like this, and your job is done. 
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
However in more advanced scenarios like the ones shown below,
some sources of change that you tilise with the builder, may come with their own `IDisposable`s that need to be disposed of when you are no longer interested in them.
For example, if you include an event handler, then the handler should be removed from the event when it is no longer required. 
Disposing of this `producerLifetime` will ensure any necessary cleanup like this is done.

## What else can I do

The builder has other methods to incorporate signals from other sources common in applications.

Skip to the bottom to see an example of the entire api surface so far.

1. Include your own custom change tokens from somewhere.

```csharp
 .Include(()=>new MyCustomChangeToken())

```

2. Include a producer of cancellation tokens:

```csharp
.IncludeCancellationTokens(()=>new CancellationToken())
```

When such `CancellationToken`s are signalled.. you guessed it.

3. Include a deferred trigger. 

A deffered trigger is similar to a normal trigger - i.e a callback that you use to invalidate change tokens, 
however it isn't supplied to you until the first change token is consumed by some consumer.
This lets you defer any logic that excercises the trigger until a change token is actually in play.

```csharp
 .IncludeDeferredTrigger((trigger) => trigger.Invoke()) // callback invoked to supply you a trigger once the first token is consumed. Note: logic here is synchronous and so will blocks the caller requesting the very first token so be snappy.
```

4. Include a deffered asynchronouse trigger. 

Similar to a deferred trigger above, except you are given a chance to run non blocking asynchronous logic with the trigger as a seperate fire and forget async task that won't block the consumer.

Consider the following:

```csharp
 .IncludeDeferredAsyncTrigger(async (trigger) => {
                                        await Task.Delay(200);
                                        trigger();
                                        await Task.Delay(500);
                                        trigger();
                                    })
 ```

 When the first token is consumed, the above async task is fired as a "fire and forget" task.
 In this example, we simulate some work with some delays, and trigger the change tokens a few times to signal any consumer.
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




