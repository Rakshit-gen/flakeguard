using FlakeGuard.Core.Domain;
using FlakeGuard.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FlakeGuard.Core.Infrastructure.Postgres;

public class PostgresRepositoryRepository(FlakeGuardDbContext db) : IRepositoryRepository
{
    public Task<Repository?> GetByIdAsync(Guid id) =>
        db.Repositories.FirstOrDefaultAsync(r => r.Id == id);

    public Task<Repository?> GetByOwnerAndNameAsync(string owner, string name) =>
        db.Repositories.FirstOrDefaultAsync(r => r.Owner == owner && r.Name == name);

    public Task<List<Repository>> GetAllAsync() =>
        db.Repositories.OrderBy(r => r.Owner).ThenBy(r => r.Name).ToListAsync();

    public async Task AddAsync(Repository repository)
    {
        db.Repositories.Add(repository);
        await db.SaveChangesAsync();
    }
}
