using System.Linq.Expressions;

namespace Chilla.Domain.Common;

public static class ExpressionCombiner
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var param = left.Parameters[0];
        if (ReferenceEquals(param, right.Parameters[0]))
        {
            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, right.Body), param);
        }
        var visitor = new ParameterReplaceVisitor(right.Parameters[0], param);
        var rightBody = visitor.Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, rightBody!), param);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var param = left.Parameters[0];
        if (ReferenceEquals(param, right.Parameters[0]))
        {
            return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, right.Body), param);
        }
        var visitor = new ParameterReplaceVisitor(right.Parameters[0], param);
        var rightBody = visitor.Visit(right.Body);
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, rightBody!), param);
    }
}

class ParameterReplaceVisitor : ExpressionVisitor
{
    private readonly ParameterExpression _from, _to;
    public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
    {
        _from = from;
        _to = to;
    }
    protected override Expression VisitParameter(ParameterExpression node) => node == _from ? _to : base.VisitParameter(node);
}