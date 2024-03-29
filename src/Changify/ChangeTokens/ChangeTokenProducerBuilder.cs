namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class ChangeTokenProducerBuilder
    {

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public ChangeTokenProducerBuilder()
        {

        }

        public Func<IChangeToken> Build(out IDisposable factoryLifetime)
        {
            var disposables = _disposables.ToArray(); // capture snapshot.
            factoryLifetime = new InvokeOnDispose(() =>
            {
                foreach (var item in disposables)
                {
                    item?.Dispose();
                }
            });

            if (Factories == null || Factories.Count == 0)
            {
                return () => EmptyChangeToken.Instance;
            }

            if (Factories.Count == 1)
            {
                return Factories[0]; // pass through - no need to build composite for single producer.
            }

            var factories = Factories.ToArray(); // capture snapshot
            Reset(); // so builder is empty again to build another.
            return () =>
            {
                var tokens = new IChangeToken[factories.Length];
                for (var i = 0; i < factories.Length; i++)
                {
                    tokens[i] = factories[i].Invoke();
                }
                return new CompositeChangeToken(tokens);
            };


        }

        public IDisposableChangeTokenProducer Build()
        {
            var built = Build(out var lifetime);
            return new DisposableChangeTokenProducer(built, lifetime);
        }


        private void Reset()
        {
            Factories.Clear();
            _disposables.Clear();
        }

        public List<Func<IChangeToken>> Factories { get; } = new List<Func<IChangeToken>>();


        /// <summary>
        /// Inlcude your own change token's in the composite. 
        /// If your <see cref="Func{IChangeToken}"/> at any point returns null, 
        /// then an <see cref="EmptyChangeToken"/> will be returned to the consumer,
        /// which is a Noop token to avoid null ref exceptions.
        /// </summary>
        /// <param name="trigger"></param>
        /// <param name="disposePreviousChangeTokens">If true (default) then when issuing a new token, will dispose of the previous token if its IDisposable.</param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder Include(Func<IChangeToken> changeToken, bool disposePreviousChangeTokens = true)
        {
            IChangeToken currentToken = null;
            IChangeToken result()
            {
                // consumer is asking for a new token, any previous token is dead.                 
                var previous = Interlocked.Exchange(ref currentToken, changeToken() ?? EmptyChangeToken.Instance);
                if (disposePreviousChangeTokens && previous is IDisposable disposable)
                {
                    disposable?.Dispose();
                }
                return currentToken;
            }

            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Inlcude your own change token producer in the composite. If it implements IDisposable then ownership is transferred to the composite producer, unless you supply isExternallyOwned = true.
        /// </summary>
        /// <param name="producer">Producer to include in this composite.</param>
        /// <param name="isExternallyOwned">Whether you want be in charge of disposing this producer should it implement IDisposable. Defaults to false.</param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder Include(IChangeTokenProducer producer, bool isExternallyOwned = false)
        {
            // IChangeToken currentToken = null;
            if (!isExternallyOwned && producer is IDisposable disposable)
            {
                _disposables.Add(disposable);
            }

            IChangeToken result() => producer.Produce() ?? EmptyChangeToken.Instance;

            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Inlcude your own change token's in the composite that are generated
        /// from the supplied <see cref="Func{CancellationToken}"/> and signalled when the cancellation tokens are signalled.
        /// If your <see cref="Func{CancellationToken}"/> at any point returns null, 
        /// then an <see cref="EmptyChangeToken"/> will be returned to the consumer,
        /// which is a Noop token to avoid null ref exceptions.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        /// <summary>
        /// Inlcude your own change token's in the composite that are generated
        /// from the supplied <see cref="Func{CancellationToken}"/> and signalled when the cancellation tokens are signalled.
        /// If your <see cref="Func{CancellationToken}"/> at any point returns null, 
        /// then an <see cref="EmptyChangeToken"/> will be returned to the consumer,
        /// which is a Noop token to avoid null ref exceptions.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeCancellationTokens(Func<CancellationToken> cancellationTokenFactory)
        {

            IChangeToken factory()
            {
                var cancelToken = cancellationTokenFactory();
                if (cancelToken == CancellationToken.None)
                {
                    return EmptyChangeToken.Instance;
                }
                else
                {
                    return new CancellationChangeToken(cancelToken);
                }
            }

            Factories.Add(factory);
            return this;


        }

        /// <summary>
        /// Inlcude a change token that can be manually triggered.
        /// </summary>
        /// <param name="trigger"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeTrigger(out Action trigger)
        {

            // The idea is the Action that we pass out, can always be used to trigger the latest token.
            // therefore as each token is requested, we need to keep track of the latest token, and dispose of the previous one.
            // the disposal fo the previous one is important, because trigger change tokens have a cancellation token source with a list of registered callbacks, that must be disposed.          

            TriggerChangeToken currentToken = null;
            IChangeToken result()
            {
                // consumer is asking for a new token, any previous token is dead.                
                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();
                return currentToken; // another thread could have just swapped in a new value to this reference
                                     // in that case its preferable that we return the newer one, but if we do return the older one, 
                                     // its ok because it will be disposed by the later thread?
            }

            Factories.Add(result);
            trigger = () => currentToken?.Trigger();

            return this;
        }

        /// <summary>
        /// Takes a callback that will be invoked when the first change token is requested,
        /// and will be provided an Action that can be invoked to invalidate the change token and all subsequent change tokens produced.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeDeferredTrigger(Action<Action> subscribeDelegate)
        {
            TriggerChangeToken currentToken = null;

            // lazy because we wait for a change token to be requested 
            // before we bother attaching the subscriber.
            var activeSubscription = new Lazy<Action>(() => () => subscribeDelegate(() => currentToken?.Trigger()));


            IChangeToken result()
            {

                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();

                var newToken = currentToken;
                // consumer is asking for a new token
                // Ensure they have a callback to signal change tokens now.
                if (!activeSubscription.IsValueCreated)
                {
                    activeSubscription.Value.Invoke();
                }

                return newToken;
            }

            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Takes a callback that will be invoked when the first change token is requested,
        /// and will be provided an Action that can be invoked to invalidate the change token and all subsequent change tokens produced.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeDeferredAsyncTrigger(Func<Action, Task> callback)
        {
            TriggerChangeToken currentToken = null;

            // lazy because we wait for a change token to be requested 
            // before we bother attaching the subscriber.
            var activeSubscription = new Lazy<Task>(async () => await callback(() => currentToken?.Trigger()));


            IChangeToken result()
            {

                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();

                var newToken = currentToken;
                // consumer is asking for a new token
                // Ensure we have supplied the trigger action to signal tokens.
                if (!activeSubscription.IsValueCreated)
                {
                    activeSubscription.Value.Forget();
                }

                return newToken;
            }

            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Takes a callback that will be invoked immediately with the trigger callback, and allows an IDisposable to be returned which will be Disposed when the token producer lifetime is disposed, to perform any cleanup necessary.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeSubscribingHandlerTrigger(Func<Action, IDisposable> subscribeDelegate)
        {
            TriggerChangeToken currentToken = null;

            // lazy because we wait for a change token to be requested 
            // before we bother attaching the subscriber.
            var activeSubscription = subscribeDelegate(() => currentToken?.Trigger());
            _disposables.Add(activeSubscription);

            IChangeToken result()
            {
                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();
                return currentToken;
            }

            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Takes a callback that will be lazily invoked when the first change token is requested, and allows an IDisposable to be returned which will be Disposed when the token producer lifetime is disposed.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeDeferredSubscribingHandlerTrigger(Func<Action, IDisposable> subscribeDelegate)
        {
            TriggerChangeToken currentToken = null;

            // lazy because we wait for a change token to be requested 
            // before we bother attaching the subscriber.
            var activeSubscription = new Lazy<IDisposable>(() => subscribeDelegate(() => currentToken?.Trigger()));


            IChangeToken result()
            {
                // consumer is asking for a new token
                // Ensure we are actively listening for callbacks.
                if (!activeSubscription.IsValueCreated)
                {
                    var disposable = activeSubscription.Value;
                    _disposables.Add(disposable);
                }

                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();
                return currentToken;
            }

            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Takes a callback that will be invoked immediately with the trigger action, and returns a disposable to represent any clean up.
        /// This is "resubscribing" because this process repeats for each token that is produced, with the IDisposable from the previous invocation being disposed prior to the next invocation.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeResubscribingHandlerTrigger(Func<Action, IDisposable> registerListener)
        {

            TriggerChangeToken currentToken = null;
            IDisposable registration = null;

            registration = registerListener(() => currentToken?.Trigger());
            _disposables.Add(new InvokeOnDispose(() => registration?.Dispose())); // ensure any current disposable is disposed when producer disposed.

            IChangeToken result()
            {
                // consumer is asking for a new token, initialise it first so that if the registerListener callback below happens immeditely it will trigger the new token
                // not the old. This does also mean there is a period of time in which if the current listener fires again it will trigger the new token but think thats ok.
                var previousToken = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previousToken?.Dispose();

                // Ensure we are actively listening for callbacks, adding the new subscription first,
                // before disposing any old subscription to ensure no gaps in listener coverage.
                var previousRegistration = Interlocked.Exchange(ref registration, registerListener(() => currentToken?.Trigger()));
                previousRegistration?.Dispose();

                return currentToken;
            }

            Factories.Add(result);
            return this;
        }


        /// <summary>
        /// Takes a callback that will be invoked once the first token is issued, and is passed the trigger action, and should return a disposable to represent any clean up.
        /// This is "resubscribing" because this process repeats for each subsequent token that is produced, with the IDisposable from the previous invocation being disposed prior to each next invocation.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>

        public ChangeTokenProducerBuilder IncludeDeferredResubscribingHandlerTrigger(Func<Action, IDisposable> registerListener)
        {

            TriggerChangeToken currentToken = null;
            IDisposable registration = null;
            this._disposables.Add(new InvokeOnDispose(() => registration?.Dispose()));

            IChangeToken result()
            {
                // consumer is asking for a new token, initialise it first so that if the registerListener callback below happens immeditely it will trigger the new token
                // not the old. This does also mean there is a period of time in which if the current listener fires again it will trigger the new token but think thats ok.
                var previousToken = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previousToken?.Dispose();

                // Ensure we are actively listening for callbacks, adding the new subscription first,
                // before disposing any old subscription to ensure no gaps in listener coverage.
                var previousRegistration = Interlocked.Exchange(ref registration, registerListener(() => currentToken?.Trigger()));
                previousRegistration?.Dispose();

                return currentToken;
            }


            Factories.Add(result);
            return this;
        }

        /// <summary>
        /// Takes a callback that will be invoked immediately and which should add the event handler to the event.
        /// Takes another callback which will be invoked to remove the handler when the token producer lifetime is over.
        /// Takes a further optional callback if you want to take control over when the handler is disposed yourself.
        /// </summary>
        /// <param name="registerListener"></param>
        /// <returns></returns>
        public ChangeTokenProducerBuilder IncludeEventHandlerTrigger<TEventArgs>(
            Action<EventHandler<TEventArgs>> addHandler,
            Action<EventHandler<TEventArgs>> removeHandler,
            Action<IDisposable> ownsHandlerLifetime = null)
        {
            TriggerChangeToken currentToken = null;
            void triggerChangeTokenHandler(object a, TEventArgs e) => currentToken?.Trigger();

            addHandler(triggerChangeTokenHandler);

            // ensure handler gets cleaned up if caller not taking responsibility for that.
            var handlerLifetime = new InvokeOnDispose(() => removeHandler(triggerChangeTokenHandler));
            if (ownsHandlerLifetime == null)
            {
                this._disposables.Add(handlerLifetime);
            }
            else
            {
                ownsHandlerLifetime.Invoke(handlerLifetime);
            }

            IChangeToken factory()
            {
                // consumer is asking for a new token, any previous token is dead.                
                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();
                return currentToken;
            }

            Factories.Add(factory);
            return this;
        }

        /// <summary>
        /// Takes a callback that will be invoked when the first token is requested, and which should add the event handler to the event.
        /// Takes another callback which will be invoked to remove the handler when the token producer lifetime is over.
        /// Takes a further optional callback if you want to take control over when the handler is disposed yourself.
        /// </summary>
        public ChangeTokenProducerBuilder IncludeDeferredEventHandlerTrigger<TEventArgs>(
            Action<EventHandler<TEventArgs>> addHandler,
            Action<EventHandler<TEventArgs>> removeHandler,
            Action<IDisposable> ownsHandlerLifetime = null)
        {
            TriggerChangeToken currentToken = null;
            void triggerChangeTokenHandler(object a, TEventArgs e) => currentToken?.Trigger();

            var subscription = new Lazy<IDisposable>(() =>
            {
                addHandler(triggerChangeTokenHandler);
                return new InvokeOnDispose(() => removeHandler(triggerChangeTokenHandler));
            });

            if (ownsHandlerLifetime == null)
            {
                this._disposables.Add(new InvokeOnDispose(() =>
                {
                    if (subscription.IsValueCreated)
                    {
                        subscription.Value.Dispose();
                    }
                }));
            }

            IChangeToken factory()
            {
                if (!subscription.IsValueCreated)
                {
                    var disposable = subscription.Value;
                    ownsHandlerLifetime?.Invoke(disposable);
                }
                // consumer is asking for a new token, any previous token is dead.                
                var previous = Interlocked.Exchange(ref currentToken, new TriggerChangeToken());
                previous?.Dispose();
                return currentToken;
            }

            Factories.Add(factory);
            return this;
        }
    }

}
