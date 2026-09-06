using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Repository.IntegrationTests;

/// <inheritdoc />
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

    public async Task<List<TEntity>> GetAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
    {
        return await context
            .Set<TEntity>()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<TEntity>> GetAsync<TEntity>(IQuery<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return await query
            .Apply(context.Set<TEntity>())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<TResultItem>> GetAsync<TEntity, TResultItem>(IQuery<TEntity, TResultItem> query, CancellationToken cancellationToken = default) where TEntity : class
    {
        return await query
            .Apply(context.Set<TEntity>())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<TResultItem>> GetAsync<TResultItem>(IContextQuery<TResultItem> query, CancellationToken cancellationToken = default)
    {
        return await query
            .Apply(context)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TResult> GetAsync<TEntity, TResult>(IQueryExecutor<TEntity, TResult> query, CancellationToken cancellationToken = default) where TEntity : class
    {
        return await query
            .ApplyAsync(context.Set<TEntity>(), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<TResult> GetAsync<TResult>(IContextQueryExecutor<TResult> query, CancellationToken cancellationToken = default)
    {
        return await query
            .ApplyAsync(context, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> ExecuteDeleteAsync<TEntity>(IQuery<TEntity> query, CancellationToken cancellationToken = default)
        where TEntity : class
    {
        return await query
            .Apply(context.Set<TEntity>())
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> ExecuteUpdateAsync<TEntity>(IQuery<TEntity> query,
        Action<UpdateSettersBuilder<TEntity>> setPropertyCalls,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        return await query
            .Apply(context.Set<TEntity>())
            .ExecuteUpdateAsync(setPropertyCalls, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        return await context.Database
            .BeginTransactionAsync(isolationLevel, cancellationToken)
            .ConfigureAwait(false);
    }
}