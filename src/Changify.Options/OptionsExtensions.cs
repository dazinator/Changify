namespace Microsoft.Extensions.Primitives
{
    using System;
    using Microsoft.Extensions.Options;

    public static class ChangeTokenFactoryUtils
    {
        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
            this ChangeTokenProducerBuilder builder,
            IOptionsMonitor<TOptions> monitor,
            string optionsName = "",
            Func<TOptions, string, bool> shouldTrigger = null) => builder.IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) =>
            {
                if (optionsName != null)
                {
                    if (!n.Equals(optionsName, StringComparison.InvariantCulture))
                    {
                        return;
                    }
                }
                if (shouldTrigger == null)
                {
                    trigger?.Invoke();
                    return;
                }
                if (shouldTrigger(o, n))
                {
                    trigger?.Invoke();
                }
            }));

        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
    this ChangeTokenProducerBuilder builder,
    IOptionsMonitor<TOptions> monitor,
    Func<TOptions, string, bool> shouldTrigger = null) => builder.IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) =>
    {
        if (shouldTrigger == null)
        {
            trigger?.Invoke();
            return;
        }
        if (shouldTrigger(o, n))
        {
            trigger?.Invoke();
        }
    }));


        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
            this ChangeTokenProducerBuilder builder,
            IOptionsMonitor<TOptions> monitor,
            string optionsName = "",
            Action<TOptions, string, Action> onOptionsChange = null) => builder.IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) =>
    {
        if (optionsName != null)
        {
            if (!n.Equals(optionsName, StringComparison.InvariantCulture))
            {
                return;
            }
        }
        if (onOptionsChange == null)
        {
            trigger?.Invoke();
            return;
        }
        onOptionsChange.Invoke(o, n, trigger);
    }));

        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
           this ChangeTokenProducerBuilder builder,
           IOptionsMonitor<TOptions> monitor,
           Action<TOptions, string, Action> onOptionsChange = null) => builder.IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) =>
           {
               if (onOptionsChange == null)
               {
                   trigger?.Invoke();
                   return;
               }
               onOptionsChange.Invoke(o, n, trigger);
           }));




    }
}
