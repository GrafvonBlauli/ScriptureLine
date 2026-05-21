using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Repositories;

public interface IPersonRepository
{
    Task<IReadOnlyList<Person>> SearchAsync(string searchText, CancellationToken cancellationToken = default);

    Task<Person?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task SaveAsync(Person person, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
