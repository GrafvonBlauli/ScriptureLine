using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Services;

public interface IProjectService
{
    Task<ProjectWorkspace> CreateProjectAsync(ProjectCreationRequest request, CancellationToken cancellationToken = default);

    Task<ProjectWorkspace> OpenProjectAsync(string projectDirectory, CancellationToken cancellationToken = default);

    Task<ProjectStatistics> ReadStatisticsAsync(ProjectWorkspace workspace, CancellationToken cancellationToken = default);
}
