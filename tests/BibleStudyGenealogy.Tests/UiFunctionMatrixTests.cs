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
