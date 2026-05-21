using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Services;

namespace BibleStudyGenealogy.Tests;

public sealed class LocalProjectServiceTests
{
    [Fact]
    public async Task CreateProjectAsync_CreatesProjectFilesAndCanOpenThemAgain()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ScriptureLineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var service = new LocalProjectService();
            var workspace = await service.CreateProjectAsync(
                new ProjectCreationRequest(tempRoot, "Mein Testprojekt", "Testbeschreibung"));

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
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                DeleteDirectoryWithRetry(tempRoot);
            }
        }
    }

    private static void DeleteDirectoryWithRetry(string path)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                Directory.Delete(path, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 5)
            {
                Thread.Sleep(100);
            }
        }

        Directory.Delete(path, recursive: true);
    }
}
