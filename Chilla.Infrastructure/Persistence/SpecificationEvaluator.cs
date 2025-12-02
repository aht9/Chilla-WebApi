using Chilla.Domain.Common;

namespace Chilla.Infrastructure.Persistence;

public static class SpecificationEvaluator
{
    public static IQueryable<TEntity> GetQuery<TEntity>(IQueryable<TEntity> inputQuery, ISpecification<TEntity> specification) where TEntity : class
    {
        var query = inputQuery;
        if (specification.Criteria != null) query = query.Where(specification.Criteria);
        
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        
        if (specification.OrderBy != null) query = query.OrderBy(specification.OrderBy);
        else if (specification.OrderByDescending != null) query = query.OrderByDescending(specification.OrderByDescending);
        
        return query;
    }
}