namespace Microsoft.Extensions.Primitives
{
    using System;
    using System.Threading.Tasks;

    public static class WhenTrueAsyncChangeTokenProducerExtensions
    {

        /// <summary>
        /// Include a producer that will wrap an inner producer, and run an asynchronous check when the inner token producer signals. If the check returns true, this producers change token will signal, otherwise it won't.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ChangeTokenProducerBuilder AndTrueAsync(
          this IChangeTokenProducer inner,
          Func<Task<bool>> checkShouldSignal,
          Action onDidNotSignal = null
          )
        {
            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            //  var inner = builder.Build();
            var newProducer = new WhenTrueAsyncChangeTokenProducer(inner, checkShouldSignal, onDidNotSignal);
            var newBuilder = new ChangeTokenProducerBuilder().Include(newProducer);
            return newBuilder;
        }
    }
}
