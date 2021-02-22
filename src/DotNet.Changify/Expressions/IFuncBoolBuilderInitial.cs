
namespace System.Linq.Expressions
{
    public interface IFuncBoolBuilderInitial
    {
        IFuncBoolBuilder Initial(Expression<Func<bool>> expression);
        IFuncBoolBuilder Initial(bool value);
    }
}
