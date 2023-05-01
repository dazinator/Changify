namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public static class ChangeTokenExtensions
    {
        /// <summary>
        /// Consumes a single <see cref="IChangeToken"/> from the producer, and asynchronously waits for it to be signalled before returning it.
        /// </summary>
        /// <param name="changeTokenProducer"></param>
        /// <returns></returns>
        public static Task<TState> WaitOneAsync<TState>(this Func<IChangeToken> changeTokenProducer, TState state = null)
            where TState : class
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }
            // consume token, and when signalled complete task completion source..
            var tcs = new TaskCompletionSource<TState>();

            var token = changeTokenProducer.Invoke();
            var handlerLifetime = token.RegisterChangeCallback((s) => tcs.SetResult(s as TState), state);

            var result = tcs.Task.ContinueWith<TState>(a =>
            {
                handlerLifetime?.Dispose();
                return a.Result;
            });

            return result;
        }

        /// <summary>
        /// Consumes a single <see cref="IChangeToken"/> from the producer, and asynchronously waits for it to be signalled before returning it.
        /// </summary>
        /// <param name="changeTokenProducer"></param>
        /// <returns></returns>
        public static Task WaitOneAsync(this Func<IChangeToken> changeTokenProducer) => WaitOneAsync<object>(changeTokenProducer, null);

        /// <summary>
        /// Consumes a single <see cref="IChangeToken"/> from the producer, and asynchronously waits for it to be signalled.
        /// </summary>
        /// <param name="changeTokenProducer"></param>
        /// <returns></returns>
        public static Task<TState> WaitOneAsync<TState>(this IChangeTokenProducer changeTokenProducer, TState state = null)
              where TState : class
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }
            // consume token, and when signalled complete task completion source..
            var tcs = new TaskCompletionSource<TState>();

            var token = changeTokenProducer.Produce();
            var handlerLifetime = token.RegisterChangeCallback((s) => tcs.SetResult(s as TState), state);


            var result = tcs.Task.ContinueWith<TState>(a =>
            {
                handlerLifetime?.Dispose();
                return a.Result;
            });
            return result;
        }

        public static Task WaitOneAsync(this IChangeTokenProducer changeTokenProducer) => WaitOneAsync<object>(changeTokenProducer, null);

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
