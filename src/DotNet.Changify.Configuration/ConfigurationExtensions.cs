namespace Microsoft.Extensions.Primitives
{
    using Microsoft.Extensions.Configuration;

    public static class ChangeTokenFactoryUtils
    {
        public static ChangeTokenProducerBuilder IncludeConfigurationChange(this ChangeTokenProducerBuilder builder, IConfiguration config)
        {
            IChangeToken factory() => config.GetReloadToken();
            builder.Factories.Add(factory);
            return builder;
        }
    }
}
