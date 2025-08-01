using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Linq.Expressions;

namespace Repository.IntegrationTests;

/// <summary>
/// A read-write repository.
/// </summary>
public interface IRepository
{
    /// <summary>Adds a new entity to the context.</summary>
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Adds multiple new entities to the context.</summary>
    void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    /// <summary>Updates an existing entity in the context.</summary>
    EntityEntry<TEntity> Update<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Updates multiple existing entities in the context.</summary>
    void UpdateRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    /// <summary>Removes an entity from the context.</summary>
    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Removes multiple entities from the context.</summary>
    void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    /// <summary>Attaches an entity to the context without marking it as modified.</summary>
    EntityEntry<TEntity> Attach<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Attaches multiple entities to the context without marking them as modified.</summary>
    void AttachRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>Retrieves all entities of type TEntity.</summary>
    Task<List<TEntity>> GetAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Executes a query and returns the resultSet.</summary>
    Task<List<TEntity>> GetAsync<TEntity>(IQuery<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Executes a projection query and returns the resultSet.</summary>
    Task<List<TResultItem>> GetAsync<TEntity, TResultItem>(IQuery<TEntity, TResultItem> query, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Executes a context-wide query and returns the resultSet.</summary>
    Task<List<TResultItem>> GetAsync<TResultItem>(IContextQuery<TResultItem> query, CancellationToken cancellationToken = default);

    /// <summary>Executes a projection query and returns the result.</summary>
    Task<TResult> GetAsync<TEntity, TResult>(IQueryExecutor<TEntity, TResult> query, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Executes a context-wide query and returns the result.</summary>
    Task<TResult> GetAsync<TResult>(IContextQueryExecutor<TResult> query, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Asynchronously deletes database rows for the entity instances which match the LINQ query from the database.
    /// </summary>
    Task<int> ExecuteDeleteAsync<TEntity>(IQuery<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>
    ///     Asynchronously updates database rows for the entity instances which match the LINQ query from the database.
    /// </summary>
    Task<int> ExecuteUpdateAsync<TEntity>(IQuery<TEntity> query, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls, CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>
    /// Saves all changes made in this context to the database asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the started transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction asynchronously with a given System.Data.IsolationLevel.
    /// </summary>
    /// <param name="isolationLevel">A transaction locking behavior.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the started transaction.</returns>
    Task<IDbContextTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);
}