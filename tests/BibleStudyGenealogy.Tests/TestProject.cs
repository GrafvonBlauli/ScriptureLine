using BibleStudyGenealogy.Infrastructure.Services;

namespace BibleStudyGenealogy.Tests;

internal sealed class TestProject : IAsyncDisposable
{
    private readonly string _tempRoot;

    private TestProject(string tempRoot, ProjectWorkspace workspace)
    {
        _tempRoot = tempRoot;
        Workspace = workspace;
    }

    public ProjectWorkspace Workspace { get; }

    public static async Task<TestProject> CreateAsync()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "ScriptureLineTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var service = new LocalProjectService();
        var workspace = await service.CreateProjectAsync(
            new ProjectCreationRequest(tempRoot, "Mein Testprojekt", "Testbeschreibung"));

        return new TestProject(tempRoot, workspace);
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_tempRoot))
        {
            DeleteDirectoryWithRetry(_tempRoot);
        }

        return ValueTask.CompletedTask;
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
