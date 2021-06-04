namespace Microsoft.Extensions.Primitives
{
    using System;
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
            var handlerLifetime = token.RegisterChangeCallback((s) =>
            {
                tcs.SetResult(s as TState);
            }, state);

            return (Task<TState>)tcs.Task.ContinueWith(a => handlerLifetime.Dispose());
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
            var handlerLifetime = token.RegisterChangeCallback((s) =>
            {
                tcs.SetResult(s as TState);
            }, state);

            return (Task<TState>)tcs.Task.ContinueWith(a => handlerLifetime.Dispose());
        }

        public static Task WaitOneAsync(this IChangeTokenProducer changeTokenProducer) => WaitOneAsync<object>(changeTokenProducer, null);

    }
}
