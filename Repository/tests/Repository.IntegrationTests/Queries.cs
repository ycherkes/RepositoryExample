using Microsoft.EntityFrameworkCore;

namespace Repository.IntegrationTests;

/// <summary>Represents a query that filters and returns entities.</summary>
public interface IQuery<TEntity> : IQuery<TEntity, TEntity>;

/// <summary>Represents a LINQ-based projection query.</summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
/// <typeparam name="TResultItem">The projected result type.</typeparam>
public interface IQuery<in TEntity, out TResultItem>
{
    /// <summary>Applies the projection to a queryable source of entities.</summary>
    IQueryable<TResultItem> Invoke(IQueryable<TEntity> queryable);
}

/// <summary>Represents a materializing query that returns the same type as the source entity.</summary>
public interface IQueryExecutor<TEntity> : IQueryExecutor<TEntity, TEntity>;

/// <summary>Represents a query that transforms and materializes a result asynchronously from a set of entities.</summary>
/// <typeparam name="TEntity">The source entity type.</typeparam>
/// <typeparam name="TResult">The result type after projection and materialization.</typeparam>
public interface IQueryExecutor<in TEntity, TResult>
{
    /// <summary>Applies the query asynchronously and materializes the result.</summary>
    Task<TResult> InvokeAsync(IQueryable<TEntity> queryable, CancellationToken cancellationToken);
}

/// <summary>Represents a projection query that uses the entire database context.</summary>
/// <typeparam name="TResult">The projected result type.</typeparam>
public interface IContextQuery<out TResult>
{
    /// <summary>Applies the query to the full <see cref="DbContext"/>.</summary>
    IQueryable<TResult> Invoke(DbContext context);
}

/// <summary>Represents a materializing query that uses the entire database context.</summary>
/// <typeparam name="TResult">The result type after materialization.</typeparam>
public interface IContextQueryExecutor<TResult>
{
    /// <summary>Applies the query asynchronously to the full <see cref="DbContext"/> and materializes the result.</summary>
    Task<TResult> InvokeAsync(DbContext context, CancellationToken cancellationToken);
}