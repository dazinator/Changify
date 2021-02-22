namespace Microsoft.Extensions.Primitives
{
    using System;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Options;

    public static class ChangeTokenFactoryUtils
    {
        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
                this ChangeTokenProducerBuilder builder,
                IServiceProvider serviceProvider,
                string optionsName = "",
                Func<TOptions, string, bool> shouldTrigger = null)
        {
            var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
            return IncludeOptionsChangeTrigger(builder, serviceProvider, optionsName, shouldTrigger);
        }

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
            IServiceProvider serviceProvider,
            Func<TOptions, string, bool> shouldTrigger = null)
        {
            var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
            return IncludeOptionsChangeTrigger(builder, serviceProvider, shouldTrigger);
        }

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
            IServiceProvider serviceProvider,
            string optionsName = "",
             Action<TOptions, string, Action> onOptionsChange = null)
        {
            var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
            return IncludeOptionsChangeTrigger(builder, serviceProvider, optionsName, onOptionsChange);
        }

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
            IServiceProvider serviceProvider,
            Action<TOptions, string, Action> onOptionsChange = null)
        {
            var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
            return IncludeOptionsChangeTrigger(builder, serviceProvider, onOptionsChange);
        }

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
