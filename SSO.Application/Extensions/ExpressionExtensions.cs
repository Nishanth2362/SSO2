using System.Linq.Expressions;

namespace SSO.Application.Extensions;

public static class PredicateBuilder
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        ParameterExpression p = left.Parameters.First();
        SubstExpressionVisitor visitor = new()
        {
            Subst = { [right.Parameters.First()] = p }
        };

        Expression body = Expression.AndAlso(left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }

    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {

        ParameterExpression p = left.Parameters.First();
        SubstExpressionVisitor visitor = new()
        {
            Subst = { [right.Parameters.First()] = p }
        };

        Expression body = Expression.OrElse(left.Body, visitor.Visit(right.Body));
        return Expression.Lambda<Func<T, bool>>(body, p);
    }
}

internal class SubstExpressionVisitor : ExpressionVisitor
{
    public Dictionary<Expression, Expression> Subst = new();

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return Subst.TryGetValue(node, out Expression? newValue) ? newValue : node;
    }
}
