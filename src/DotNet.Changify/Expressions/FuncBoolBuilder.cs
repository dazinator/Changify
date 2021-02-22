
namespace System.Linq.Expressions
{
    public class FuncBoolBuilder : IFuncBoolBuilderInitial, IFuncBoolBuilder
    {
        private Expression<Func<bool>> _expression = null;
        private readonly Lazy<FuncBoolBuilder> _subBuilder = new Lazy<FuncBoolBuilder>(() => new FuncBoolBuilder());

        private FuncBoolBuilder()
        {

        }

        public static Func<bool> Build(Action<IFuncBoolBuilderInitial> buildCallback)
        {
            var builder = new FuncBoolBuilder();
            buildCallback(builder);
            var built = builder.Build();
            return built;
        }

        public static Func<bool> Build(IServiceProvider serviceProvider, Action<IFuncBoolBuilderInitial> buildCallback)
        {
            var builder = new FuncBoolBuilder
            {
                ServiceProvider = serviceProvider
            };
            buildCallback(builder);
            var built = builder.Build();
            return built;
        }

        public IServiceProvider ServiceProvider { get; private set; }

        public IFuncBoolBuilder Initial(Expression<Func<bool>> expression)
        {
            _expression = expression;
            return this;
        }

        public IFuncBoolBuilder Initial(bool value) => Initial(() => value);

        public IFuncBoolBuilder AndAlso(Expression<Func<bool>> expression)
        {
            _expression = _expression.AndAlso(expression);
            return this;
        }

        public IFuncBoolBuilder AndAlso(Action<IFuncBoolBuilderInitial> buildSubExpression)
        {
            var subBuilder = _subBuilder.Value;
            buildSubExpression(subBuilder);
            var subExpression = subBuilder._expression;
            subBuilder.Reset();
            _expression = _expression.AndAlso(subExpression);
            return this;
        }

        public IFuncBoolBuilder AndAlso(bool value) => AndAlso(() => value);

        public IFuncBoolBuilder OrElse(Expression<Func<bool>> expression)
        {
            _expression = _expression.OrElse(expression);
            return this;
        }

        public IFuncBoolBuilder OrElse(Action<IFuncBoolBuilderInitial> buildSubExpression)
        {
            var subBuilder = _subBuilder.Value;
            buildSubExpression(subBuilder);
            var subExpression = subBuilder._expression;
            subBuilder.Reset();
            _expression = _expression.OrElse(subExpression);
            return this;
        }

        public IFuncBoolBuilder OrElse(bool value) => OrElse(() => value);

        public Func<bool> Build()
        {
            if (_expression == null)
            {
                throw new InvalidOperationException("expression not yet set, invalid for build.");
            }
            return _expression.Compile();
        }

        private void Reset() => _expression = null;

    }
}
