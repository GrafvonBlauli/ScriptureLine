using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public interface IRelationshipRepository
{
    Task<IReadOnlyList<Relationship>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<Relationship?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(Relationship relationship, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
