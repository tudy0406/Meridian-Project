namespace Meridian.Api.Common.Persistence;

/// <summary>
/// Commits a coordinated set of changes as a single transaction. Repositories
/// stage changes; the service calls <see cref="SaveChangesAsync"/> once per use
/// case so that a business operation either fully succeeds or fully rolls back.
/// Dispatching of domain events happens as part of the same commit.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
