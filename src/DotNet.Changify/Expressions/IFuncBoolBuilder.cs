
namespace System.Linq.Expressions
{
    public interface IFuncBoolBuilder
    {
        IFuncBoolBuilder AndAlso(Expression<Func<bool>> expression);
        IFuncBoolBuilder AndAlso(Action<IFuncBoolBuilderInitial> buildSubExpression);
        IFuncBoolBuilder AndAlso(bool value);
        IFuncBoolBuilder OrElse(Expression<Func<bool>> expression);
        IFuncBoolBuilder OrElse(Action<IFuncBoolBuilderInitial> buildSubExpression);
        IFuncBoolBuilder OrElse(bool value);
    }
}
