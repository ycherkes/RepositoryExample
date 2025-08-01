using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Repository.IntegrationTests;

/// <inheritdoc />
[method: DebuggerStepThrough]
public class Repository(DbContext context) : IRepository
{
    public EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Add(entity);
    }

    public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        context.AddRange(entities);
    }

    public EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Update(entity);
    }

    public void UpdateRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        context.UpdateRange(entities);
    }

    public EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Remove(entity);
    }

    public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        context.RemoveRange(entities);
    }

    public EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Attach(entity);
    }

    public void AttachRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
    {
        context.AttachRange(entities);
    }

    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class
    {
        return context.Entry(entity);
    }

    public Task<List<TEntity>> GetAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
    {
        return context.Set<TEntity>().ToListAsync(cancellationToken);
    }

    public Task<List<TEntity>> GetAsync<TEntity>(IQuery<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return query.Invoke(context.Set<TEntity>()).ToListAsync(cancellationToken);
    }

    public Task<List<TResultItem>> GetAsync<TEntity, TResultItem>(IQuery<TEntity, TResultItem> query, CancellationToken cancellationToken = default) where TEntity : class
    {
        return query.Invoke(context.Set<TEntity>()).ToListAsync(cancellationToken);
    }

    public Task<List<TResultItem>> GetAsync<TResultItem>(IContextQuery<TResultItem> query, CancellationToken cancellationToken = default)
    {
        return query.Invoke(context).ToListAsync(cancellationToken);
    }

    public Task<TResult> GetAsync<TEntity, TResult>(IQueryExecutor<TEntity, TResult> query, CancellationToken cancellationToken = default) where TEntity : class
    {
        return query.InvokeAsync(context.Set<TEntity>(), cancellationToken);
    }

    public Task<TResult> GetAsync<TResult>(IContextQueryExecutor<TResult> query, CancellationToken cancellationToken = default)
    {
        return query.InvokeAsync(context, cancellationToken);
    }

    public Task<int> ExecuteDeleteAsync<TEntity>(IQuery<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return query.Invoke(context.Set<TEntity>()).ExecuteDeleteAsync(cancellationToken: cancellationToken);
    }

    public Task<int> ExecuteUpdateAsync<TEntity>(IQuery<TEntity> query,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        return query.Invoke(context.Set<TEntity>()).ExecuteUpdateAsync(setPropertyCalls, cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        return await context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }
}