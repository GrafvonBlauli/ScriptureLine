using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public interface IMediaRepository
{
    Task<IReadOnlyList<MediaFile>> SearchAsync(string searchText, CancellationToken cancellationToken = default);

    Task<MediaFile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(MediaFile mediaFile, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task LinkAsync(Guid mediaFileId, LinkedEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MediaFile>> GetForEntityAsync(LinkedEntityType entityType, Guid entityId, CancellationToken cancellationToken = default);
}
