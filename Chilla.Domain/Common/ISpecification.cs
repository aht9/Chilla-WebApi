using System.Linq.Expressions;

namespace Chilla.Domain.Common;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>> OrderBy { get; }
    Expression<Func<T, object>> OrderByDescending { get; }
}

public abstract class BaseSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>> OrderBy { get; private set; }
    public Expression<Func<T, object>> OrderByDescending { get; private set; }

    protected BaseSpecification(Expression<Func<T, bool>> criteria)
    {
        Criteria = criteria;
    }

    public void And(Expression<Func<T, bool>> otherCriteria)
    {
        Criteria = Criteria.And(otherCriteria);
    }

    public void Or(Expression<Func<T, bool>> otherCriteria)
    {
        Criteria = Criteria.Or(otherCriteria);
    }

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);
}