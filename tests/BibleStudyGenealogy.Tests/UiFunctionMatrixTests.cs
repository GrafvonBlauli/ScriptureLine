using System.Text.RegularExpressions;

namespace BibleStudyGenealogy.Tests;

public sealed partial class UiFunctionMatrixTests
{
    [Fact]
    public void MainWindow_ClickHandlers_AreImplementedInCodeBehind()
    {
        var rootDirectory = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "MainWindow.axaml"));
        var codeBehind = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "MainWindow.axaml.cs"));
        var clickHandlers = ClickHandlerRegex()
            .Matches(xaml)
            .Select(match => match.Groups[1].Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(clickHandlers);
        foreach (var handler in clickHandlers)
        {
            Assert.Contains($"void {handler}", codeBehind, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MainWindow_KnownPlaceholders_AreMarkedAsComingSoon()
    {
        var rootDirectory = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "MainWindow.axaml"));

        Assert.Contains("Karte (bald)", xaml, StringComparison.Ordinal);
        Assert.Contains("Orte (bald)", xaml, StringComparison.Ordinal);
        Assert.Contains("Forschungsfragen (bald)", xaml, StringComparison.Ordinal);
        Assert.Contains("Ort anlegen (bald)", xaml, StringComparison.Ordinal);
        Assert.Contains("noch nicht umgesetzt", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindow_SidebarButtons_HaveNavigationHandlers()
    {
        var rootDirectory = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "MainWindow.axaml"));

        var expectedHandlers = new[]
        {
            "NavigateDashboardButton_Click",
            "NavigatePeopleButton_Click",
            "NavigateFamilyTreeButton_Click",
            "NavigateTimelineButton_Click",
            "NavigateMapButton_Click",
            "NavigateEventsButton_Click",
            "NavigatePlacesButton_Click",
            "NavigateBibleReferencesButton_Click",
            "NavigateMediaButton_Click",
            "NavigateResearchQuestionsButton_Click"
        };

        foreach (var handler in expectedHandlers)
        {
            Assert.Contains($"Click=\"{handler}\"", xaml, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MainWindow_ProjectCloseButton_IsAvailable()
    {
        var rootDirectory = FindRepositoryRoot();
        var xaml = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "MainWindow.axaml"));
        var codeBehind = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "MainWindow.axaml.cs"));

        Assert.Contains("x:Name=\"CloseProjectButton\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Projekt schließen\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CloseProjectButton_Click\"", xaml, StringComparison.Ordinal);
        Assert.Contains("void CloseProjectButton_Click", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SaveOpenEditorsBeforeCloseAsync", codeBehind, StringComparison.Ordinal);
        Assert.Contains("ClearAsync", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void AppModule_DefinesEverySidebarTarget()
    {
        var rootDirectory = FindRepositoryRoot();
        var appModuleSource = File.ReadAllText(Path.Combine(rootDirectory, "src", "BibleStudyGenealogy.App", "AppModule.cs"));
        var expectedModules = new[]
        {
            "Dashboard",
            "People",
            "FamilyTree",
            "Timeline",
            "Map",
            "Events",
            "Places",
            "BibleReferences",
            "Media",
            "ResearchQuestions"
        };

        foreach (var module in expectedModules)
        {
            Assert.Contains(module, appModuleSource, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "BibleStudyGenealogy.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be found.");
    }

    [GeneratedRegex("Click=\"([^\"]+)\"")]
    private static partial Regex ClickHandlerRegex();
}
