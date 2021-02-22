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
            return IncludeOptionsChangeTrigger(builder, monitor, optionsName, shouldTrigger);
        }

        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
              this ChangeTokenProducerBuilder builder,
             IOptionsMonitor<TOptions> monitor,
              string optionsName = "",
              Func<TOptions, string, bool> shouldTrigger = null) => IncludeOptionsChangeTrigger(builder, monitor, optionsName, (a, b, c) =>
                                                                              {
                                                                                  if (shouldTrigger(a, b))
                                                                                  {
                                                                                      c?.Invoke();
                                                                                  }
                                                                              });




        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
            this ChangeTokenProducerBuilder builder,
            IServiceProvider serviceProvider,
            string optionsName,
            Action<TOptions, string, Action> onOptionsChange)
        {
            var monitor = serviceProvider.GetRequiredService<IOptionsMonitor<TOptions>>();
            return IncludeOptionsChangeTrigger(builder, monitor, optionsName, onOptionsChange);
        }

        public static ChangeTokenProducerBuilder IncludeOptionsChangeTrigger<TOptions>(
            this ChangeTokenProducerBuilder builder,
            IOptionsMonitor<TOptions> monitor,
            string optionsName,
            Action<TOptions, string, Action> onOptionsChange) => builder.IncludeDeferredSubscribingHandlerTrigger((trigger) => monitor.OnChange((o, n) =>
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

    }
}
