using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using System.Text.Json;

namespace BibleStudyGenealogy.App;

public partial class MainWindow : Window
{
    private readonly IProjectService _projectService = new LocalProjectService();
    private readonly AppStateStore _appStateStore = new();
    private IPersonRepository? _personRepository;
    private ProjectWorkspace? _currentWorkspace;
    private Person? _currentPerson;

    public MainWindow()
    {
        InitializeComponent();
        InitializePersonForm();
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

    private void CreatePersonButton_Click(object? sender, RoutedEventArgs e)
    {
        _currentPerson = new Person();
        ClearPersonForm();
        PersonFormStatusText.Text = "Neue Person vorbereiten.";
        MainNameTextBox.Focus();
    }

    private async void SavePersonButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_personRepository is null || _currentWorkspace is null)
        {
            PersonFormStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
            return;
        }

        if (string.IsNullOrWhiteSpace(MainNameTextBox.Text))
        {
            PersonFormStatusText.Text = "Bitte gib mindestens einen Hauptnamen ein.";
            return;
        }

        _currentPerson ??= new Person();
        FillPersonFromForm(_currentPerson);

        try
        {
            await _personRepository.SaveAsync(_currentPerson);
            PersonFormStatusText.Text = "Person wurde gespeichert.";
            await RefreshPeopleAsync();
            await RefreshStatisticsAsync();
        }
        catch (Exception exception)
        {
            PersonFormStatusText.Text = $"Person konnte nicht gespeichert werden: {exception.Message}";
        }
    }

    private async void PersonSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        await RefreshPeopleAsync();
    }

    private async void PeopleListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (PeopleListBox.SelectedItem is not PersonListItem selectedPerson || _personRepository is null)
        {
            return;
        }

        _currentPerson = await _personRepository.GetByIdAsync(selectedPerson.Id);
        if (_currentPerson is null)
        {
            PersonFormStatusText.Text = "Person wurde nicht gefunden.";
            return;
        }

        FillFormFromPerson(_currentPerson);
        PersonFormStatusText.Text = "Person geladen.";
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
        _currentWorkspace = workspace;
        _personRepository = new PersonRepository(workspace.DatabasePath);
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
        ResearchQuestionCountText.Text = $"{statistics.ResearchQuestionCount} offene Fragen";
        CreatePersonButton.IsEnabled = true;
        QuickAddPersonButton.IsEnabled = true;
        SavePersonButton.IsEnabled = true;
        PersonFormStatusText.Text = "Bereit für die erste Person.";

        await RefreshPeopleAsync();
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

    private void InitializePersonForm()
    {
        GenderComboBox.ItemsSource = new[]
        {
            new EnumDisplay<Gender>(Gender.Unknown, "unbekannt"),
            new EnumDisplay<Gender>(Gender.Male, "männlich"),
            new EnumDisplay<Gender>(Gender.Female, "weiblich"),
            new EnumDisplay<Gender>(Gender.Other, "andere Angabe")
        };

        PersonStatusComboBox.ItemsSource = new[]
        {
            new EnumDisplay<PersonStatus>(PersonStatus.Active, "aktiv"),
            new EnumDisplay<PersonStatus>(PersonStatus.Uncertain, "unsicher"),
            new EnumDisplay<PersonStatus>(PersonStatus.Archived, "archiviert"),
            new EnumDisplay<PersonStatus>(PersonStatus.Rejected, "verworfen"),
            new EnumDisplay<PersonStatus>(PersonStatus.DuplicateCandidate, "mögliches Duplikat")
        };

        GenderComboBox.SelectedIndex = 0;
        PersonStatusComboBox.SelectedIndex = 0;
    }

    private async Task RefreshPeopleAsync()
    {
        if (_personRepository is null)
        {
            PeopleListBox.ItemsSource = Array.Empty<PersonListItem>();
            PeopleEmptyText.Text = "Noch kein Projekt geöffnet.";
            return;
        }

        var people = await _personRepository.SearchAsync(PersonSearchTextBox.Text ?? string.Empty);
        PeopleListBox.ItemsSource = people
            .Select(person => new PersonListItem(person.Id, person.MainName, person.PrimaryRole))
            .ToList();
        PeopleEmptyText.Text = people.Count == 0
            ? "Noch keine passenden Personen gefunden."
            : $"{people.Count} Person(en) gefunden.";
    }

    private async Task RefreshStatisticsAsync()
    {
        if (_currentWorkspace is null)
        {
            return;
        }

        var statistics = await _projectService.ReadStatisticsAsync(_currentWorkspace);
        PersonCountText.Text = statistics.PersonCount.ToString();
        LastEditedText.Text = _currentPerson is null
            ? "Noch keine Person bearbeitet"
            : $"{_currentPerson.MainName} wurde zuletzt gespeichert.";
    }

    private void FillPersonFromForm(Person person)
    {
        person.MainName = MainNameTextBox.Text?.Trim() ?? string.Empty;
        person.AlternativeNames = AlternativeNamesTextBox.Text?.Trim() ?? string.Empty;
        person.HebrewName = HebrewNameTextBox.Text?.Trim() ?? string.Empty;
        person.GreekName = GreekNameTextBox.Text?.Trim() ?? string.Empty;
        person.NameMeaning = NameMeaningTextBox.Text?.Trim() ?? string.Empty;
        person.PrimaryRole = PrimaryRoleTextBox.Text?.Trim() ?? string.Empty;
        person.Occupation = OccupationTextBox.Text?.Trim() ?? string.Empty;
        person.ShortDescription = ShortDescriptionTextBox.Text?.Trim() ?? string.Empty;
        person.LongDescription = LongDescriptionTextBox.Text?.Trim() ?? string.Empty;
        person.Gender = (GenderComboBox.SelectedItem as EnumDisplay<Gender>)?.Value ?? Gender.Unknown;
        person.Status = (PersonStatusComboBox.SelectedItem as EnumDisplay<PersonStatus>)?.Value ?? PersonStatus.Active;
    }

    private void FillFormFromPerson(Person person)
    {
        MainNameTextBox.Text = person.MainName;
        AlternativeNamesTextBox.Text = person.AlternativeNames;
        HebrewNameTextBox.Text = person.HebrewName;
        GreekNameTextBox.Text = person.GreekName;
        NameMeaningTextBox.Text = person.NameMeaning;
        PrimaryRoleTextBox.Text = person.PrimaryRole;
        OccupationTextBox.Text = person.Occupation;
        ShortDescriptionTextBox.Text = person.ShortDescription;
        LongDescriptionTextBox.Text = person.LongDescription;
        SelectEnumValue(GenderComboBox, person.Gender);
        SelectEnumValue(PersonStatusComboBox, person.Status);
    }

    private void ClearPersonForm()
    {
        MainNameTextBox.Text = string.Empty;
        AlternativeNamesTextBox.Text = string.Empty;
        HebrewNameTextBox.Text = string.Empty;
        GreekNameTextBox.Text = string.Empty;
        NameMeaningTextBox.Text = string.Empty;
        PrimaryRoleTextBox.Text = string.Empty;
        OccupationTextBox.Text = string.Empty;
        ShortDescriptionTextBox.Text = string.Empty;
        LongDescriptionTextBox.Text = string.Empty;
        GenderComboBox.SelectedIndex = 0;
        PersonStatusComboBox.SelectedIndex = 0;
    }

    private static void SelectEnumValue<T>(ComboBox comboBox, T value)
        where T : struct, Enum
    {
        foreach (var item in comboBox.Items)
        {
            if (item is EnumDisplay<T> enumDisplay && EqualityComparer<T>.Default.Equals(enumDisplay.Value, value))
            {
                comboBox.SelectedItem = enumDisplay;
                return;
            }
        }
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

    private sealed record EnumDisplay<T>(T Value, string DisplayName)
        where T : struct, Enum
    {
        public override string ToString()
        {
            return DisplayName;
        }
    }

    private sealed record PersonListItem(Guid Id, string MainName, string PrimaryRole)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(PrimaryRole)
                ? MainName
                : $"{MainName} - {PrimaryRole}";
        }
    }

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
