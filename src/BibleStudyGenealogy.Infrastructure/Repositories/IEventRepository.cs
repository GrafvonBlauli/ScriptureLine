using BibleStudyGenealogy.Core.Models;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public interface IEventRepository
{
    Task<IReadOnlyList<ScriptureEvent>> SearchAsync(string searchText, CancellationToken cancellationToken = default);

    Task<ScriptureEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(ScriptureEvent scriptureEvent, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    Task LinkPersonAsync(Guid eventId, Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScriptureEvent>> GetForPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetPeopleForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
