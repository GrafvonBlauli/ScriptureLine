using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Services;

namespace BibleStudyGenealogy.Tests;

public sealed class LocalProjectServiceTests
{
    [Fact]
    public async Task CreateProjectAsync_CreatesProjectFilesAndCanOpenThemAgain()
    {
        await using var project = await TestProject.CreateAsync();
        var service = new LocalProjectService();
        var workspace = project.Workspace;

        Assert.True(File.Exists(workspace.ManifestPath));
        Assert.True(File.Exists(workspace.DatabasePath));
        Assert.True(Directory.Exists(Path.Combine(workspace.RootDirectory, "Media", "Persons")));
        Assert.True(Directory.Exists(Path.Combine(workspace.RootDirectory, "Media", "PDFs")));
        Assert.True(Directory.Exists(Path.Combine(workspace.RootDirectory, "Thumbnails")));
        Assert.True(Directory.Exists(Path.Combine(workspace.RootDirectory, "Backups")));
        Assert.Equal(ProjectDefaults.PreferredBibleTranslation, workspace.Settings.PreferredBibleTranslation);

        var openedWorkspace = await service.OpenProjectAsync(workspace.RootDirectory);

        Assert.Equal(workspace.Metadata.ProjectId, openedWorkspace.Metadata.ProjectId);
        Assert.Equal("Mein Testprojekt", openedWorkspace.Settings.ProjectName);
        Assert.Equal("Testbeschreibung", openedWorkspace.Settings.Description);

        var statistics = await service.ReadStatisticsAsync(openedWorkspace);

        Assert.Equal(0, statistics.PersonCount);
    }
}
