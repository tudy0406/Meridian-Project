using System.Linq.Expressions;
using Meridian.Api.Common.Domain;

namespace Meridian.Api.Common.Persistence;

/// <summary>
/// Generic data-access abstraction (Repository pattern). Keeps business logic in
/// services free of EF Core details and makes the persistence layer swappable
/// and testable. Feature-specific repositories extend this with queries that are
/// meaningful to their aggregate.
/// </summary>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<List<TEntity>> ListAsync(CancellationToken ct = default);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);

    /// <summary>Query hook for feature repositories that need composed reads (includes, projections).</summary>
    IQueryable<TEntity> Query();
}
