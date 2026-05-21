using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BibleStudyGenealogy.Infrastructure.Services;
using System.Text.Json;

namespace BibleStudyGenealogy.App;

public partial class MainWindow : Window
{
    private readonly IProjectService _projectService = new LocalProjectService();
    private readonly AppStateStore _appStateStore = new();

    public MainWindow()
    {
        InitializeComponent();
        RestoreLastProject();
    }

    private async void CreateProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        var projectName = string.IsNullOrWhiteSpace(ProjectNameTextBox.Text)
            ? "Mein Bibelprojekt"
            : ProjectNameTextBox.Text.Trim();

        var parentDirectory = await PickFolderAsync("Speicherort für das neue ScriptureLine-Projekt wählen");
        if (parentDirectory is null)
        {
            return;
        }

        try
        {
            SetBusyStatus("Projekt wird angelegt ...");
            var workspace = await _projectService.CreateProjectAsync(
                new ProjectCreationRequest(parentDirectory, projectName));

            await ShowProjectAsync(workspace, "Projekt wurde erfolgreich angelegt.");
        }
        catch (Exception exception)
        {
            ShowError($"Projekt konnte nicht angelegt werden: {exception.Message}");
        }
    }

    private async void OpenProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        var projectDirectory = await PickFolderAsync("ScriptureLine-Projektordner öffnen");
        if (projectDirectory is null)
        {
            return;
        }

        try
        {
            SetBusyStatus("Projekt wird geöffnet ...");
            var workspace = await _projectService.OpenProjectAsync(projectDirectory);
            await ShowProjectAsync(workspace, "Projekt wurde erfolgreich geöffnet.");
        }
        catch (Exception exception)
        {
            ShowError($"Projekt konnte nicht geöffnet werden: {exception.Message}");
        }
    }

    private async Task<string?> PickFolderAsync(string title)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        return folders.Count > 0
            ? folders[0].TryGetLocalPath()
            : null;
    }

    private async Task ShowProjectAsync(ProjectWorkspace workspace, string status)
    {
        var statistics = await _projectService.ReadStatisticsAsync(workspace);

        SidebarProjectTitle.Text = workspace.Settings.ProjectName;
        SidebarProjectStatus.Text = "Projekt geöffnet";
        ProjectStatusText.Text = $"{status} Speicherort: {workspace.RootDirectory}";
        CurrentProjectTitle.Text = workspace.Settings.ProjectName;
        CurrentProjectDetails.Text =
            $"Lokales Projekt mit SQLite-Datenbank. Bevorzugte Übersetzung: {workspace.Settings.PreferredBibleTranslation}.";

        PersonCountText.Text = statistics.PersonCount.ToString();
        RelationshipCountText.Text = statistics.RelationshipCount.ToString();
        PlaceCountText.Text = statistics.PlaceCount.ToString();
        ResearchQuestionCountText.Text = statistics.ResearchQuestionCount.ToString();

        await _appStateStore.SaveAsync(new AppState(workspace.RootDirectory));
    }

    private void SetBusyStatus(string status)
    {
        ProjectStatusText.Text = status;
        SidebarProjectStatus.Text = status;
    }

    private void ShowError(string message)
    {
        ProjectStatusText.Text = message;
        SidebarProjectStatus.Text = "Aktion fehlgeschlagen";
    }

    private async void RestoreLastProject()
    {
        try
        {
            var state = await _appStateStore.LoadAsync();
            if (state?.LastProjectDirectory is null || !Directory.Exists(state.LastProjectDirectory))
            {
                return;
            }

            var workspace = await _projectService.OpenProjectAsync(state.LastProjectDirectory);
            await ShowProjectAsync(workspace, "Zuletzt geöffnetes Projekt wurde geladen.");
        }
        catch
        {
            SidebarProjectStatus.Text = "Kein Projekt geladen";
        }
    }

    private sealed record AppState(string LastProjectDirectory);

    private sealed class AppStateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        private readonly string _stateFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ScriptureLine",
            "app-state.json");

        public async Task<AppState?> LoadAsync()
        {
            if (!File.Exists(_stateFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(_stateFilePath);
            return JsonSerializer.Deserialize<AppState>(json, JsonOptions);
        }

        public async Task SaveAsync(AppState state)
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(state, JsonOptions);
            await File.WriteAllTextAsync(_stateFilePath, json);
        }
    }
}
