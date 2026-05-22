using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public interface IBibleReferenceRepository
{
    Task<IReadOnlyList<BibleReference>> SearchAsync(string searchText, CancellationToken cancellationToken = default);

    Task<BibleReference?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(BibleReference bibleReference, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task LinkEventAsync(Guid eventId, Guid bibleReferenceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BibleReference>> GetForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
