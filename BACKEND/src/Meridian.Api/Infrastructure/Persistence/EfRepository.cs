using System.Linq.Expressions;
using Meridian.Api.Common.Domain;
using Meridian.Api.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Meridian.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of the generic <see cref="IRepository{TEntity}"/>.
/// Registered open-generically so every entity gets a repository for free, while
/// feature repositories subclass it to add aggregate-specific queries.
/// </summary>
public class EfRepository<TEntity> : IRepository<TEntity> where TEntity : BaseEntity
{
    protected readonly MeridianDbContext Db;
    protected readonly DbSet<TEntity> Set;

    public EfRepository(MeridianDbContext db)
    {
        Db = db;
        Set = db.Set<TEntity>();
    }

    public Task<TEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(predicate, ct);

    public Task<List<TEntity>> ListAsync(CancellationToken ct = default) =>
        Set.ToListAsync(ct);

    public Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.Where(predicate).ToListAsync(ct);

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        Set.AnyAsync(predicate, ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default) =>
        await Set.AddAsync(entity, ct);

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) =>
        await Set.AddRangeAsync(entities, ct);

    public void Update(TEntity entity) => Set.Update(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);

    public IQueryable<TEntity> Query() => Set.AsQueryable();
}
