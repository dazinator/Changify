namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Reflection.Metadata.Ecma335;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ChangeTokenExtensions
    {
        /// <summary>
        /// Consumes a single <see cref="IChangeToken"/> from the producer, and asynchronously waits for it to be signalled before returning it.
        /// </summary>
        /// <param name="changeTokenProducer"></param>
        /// <returns></returns>
        public static Task WaitOneAsync(this Func<IChangeToken> changeTokenProducer, CancellationToken cancellationToken = default) => WaitOneAsync<object>(changeTokenProducer, null, cancellationToken);

        /// <summary>
        /// Consumes a single <see cref="IChangeToken"/> from the producer, and asynchronously waits for it to be signalled before returning it.
        /// </summary>
        /// <param name="changeTokenProducer"></param>
        /// <returns></returns>
        public static Task<TState> WaitOneAsync<TState>(this Func<IChangeToken> changeTokenProducer, TState state = null, CancellationToken cancellationToken = default)
            where TState : class
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }

            var token = changeTokenProducer.Invoke();
            return token.WaitOneAsync(state, cancellationToken);
        }

        public static Task WaitOneAsync(this IChangeTokenProducer changeTokenProducer, CancellationToken cancellationToken = default) => WaitOneAsync<object>(changeTokenProducer, null, cancellationToken);

        /// <summary>
        /// Consumes a single <see cref="IChangeToken"/> from the producer, and asynchronously waits for it to be signalled.
        /// </summary>
        /// <param name="changeTokenProducer"></param>
        /// <returns></returns>
        public static Task<TState> WaitOneAsync<TState>(this IChangeTokenProducer changeTokenProducer, TState state = null, CancellationToken cancellationToken = default)
              where TState : class
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }

            var token = changeTokenProducer.Produce();
            return token.WaitOneAsync(state, cancellationToken);
        }

        /// <summary>
        /// Waits for a single <see cref="IChangeToken"/> to be singalled.
        /// </summary>
        /// <param name="changeToken"></param>
        /// <returns></returns>
        public static Task WaitOneAsync(this IChangeToken changeToken, CancellationToken cancellationToken = default) => WaitOneAsync<object>(changeToken, null, cancellationToken);

        /// <summary>
        ///Waits for a single <see cref="IChangeToken"/> to be singalled.
        /// </summary>      
        /// <param name="changeToken"></param>
        /// <returns></returns>
        public static Task<TState> WaitOneAsync<TState>(this IChangeToken changeToken, TState state = null, CancellationToken cancellationToken = default)
            where TState : class
        {
            if (changeToken == null)
            {
                throw new ArgumentNullException(nameof(changeToken));
            }
            // consume token, and when signalled complete task completion source..
            // if its already signalled return immediately
            if (changeToken.HasChanged)
            {
                return Task.FromResult(state);
            }

            var tcs = new TaskCompletionSource<TState>();

            cancellationToken.Register(() =>
            {
                tcs.TrySetCanceled(cancellationToken);
            });

            // var token = changeTokenProducer.Invoke();
            var handlerLifetime = changeToken.RegisterChangeCallback((s) => {

                tcs.TrySetResult(s as TState);
            }, state);              

            var result = tcs.Task.ContinueWith<TState>(a =>
            {
                handlerLifetime?.Dispose();
                return a.Result;
            });

            //// check again, in case it was signalled between the check above and the registration being added
            // not certain this is necessary as it might be the callback is invoked immediately if the token is already signalled
            //if (changeToken.HasChanged)
            //{
            //    tcs.TrySetResult(state);
            //}           

            return result;
        }


        #region CancellationToken

        /// <summary>
        /// Waits asynchronously for a <see cref="CancellationToken"/> to be singalled.
        /// </summary>
        /// <param name="token">Cancellation token to wait to be cancelled.</param>
        /// <param name="cancellationToken">Cancellation token used to abort the wait operation</param>
        /// <returns></returns>
        public static Task WaitUntilCancelledAsync(this CancellationToken token, CancellationToken cancellationToken = default) => token.ToChangeToken().WaitOneAsync<object>(null, cancellationToken);

        public static CancellationChangeToken ToChangeToken(this CancellationToken cancellationToken) => new CancellationChangeToken(cancellationToken);


        #endregion


        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> async task to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Async task to be called when the token changes.</param>
        /// <returns>An <see cref="IDisposable"/> that should be disposed to unregister the callback.</returns>
        public static IDisposable OnChange(this Func<IChangeToken> changeTokenProducer, Func<Task> changeTokenConsumer)
        {
            var subscribed = true;
            var subscriptionLifetime = new InvokeOnDispose(() => subscribed = false);

            var task = Task.Run(async () =>
            {
                while (subscribed)
                {
                    var tcs = new TaskCompletionSource();
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    Task.Run(async () =>
                    {
                        if (subscribed) // unsubscribe could happen on another thread right up to the point we invoke the callback (and even during)
                        {
                            await changeTokenProducer.WaitOneAsync();
                            if (subscribed)
                            {
                                await changeTokenConsumer?.Invoke();
                            }
                        }

                    }).ContinueWith((t) => tcs.SetResult()).ConfigureAwait(false);

                    await tcs.Task;
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }).ConfigureAwait(false);

            return subscriptionLifetime;
        }


    }
}
