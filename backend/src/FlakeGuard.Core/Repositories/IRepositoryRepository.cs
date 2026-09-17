using FlakeGuard.Core.Domain;

namespace FlakeGuard.Core.Repositories;

public interface IRepositoryRepository
{
    Task<Repository?> GetByIdAsync(Guid id);
    Task<Repository?> GetByOwnerAndNameAsync(string owner, string name);
    Task<List<Repository>> GetAllAsync();
    Task AddAsync(Repository repository);
}
