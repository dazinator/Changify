namespace Changify.Utils
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Utility class used to asynchronously delay for potentially long periods of time.
    /// </summary>
    public static class LongDelay
    {
        // public static async Task For(long delay)
        // {
        //     while (delay > 0)
        //     {
        //         var currentDelay = delay > int.MaxValue ? int.MaxValue : (int)delay;
        //         await Task.Delay(currentDelay);
        //         delay -= currentDelay;
        //     }
        // }

        public static async Task For(long delay, CancellationToken token, Action<int> willDelayCallback = null)
        {
            while (delay > 0)
            {
                var currentDelay = delay > int.MaxValue ? int.MaxValue : (int)delay;
                willDelayCallback?.Invoke(currentDelay);
                await Task.Delay(currentDelay, token);
                delay -= currentDelay;
            }
        }
    }
}
