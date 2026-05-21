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
    private IRelationshipRepository? _relationshipRepository;
    private ProjectWorkspace? _currentWorkspace;
    private Person? _currentPerson;
    private IReadOnlyList<Person> _people = Array.Empty<Person>();

    public MainWindow()
    {
        InitializeComponent();
        InitializePersonForm();
        InitializeRelationshipForm();
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
        await RefreshRelationshipsAsync();
    }

    private async void SaveRelationshipButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_relationshipRepository is null || _currentPerson is null)
        {
            RelationshipFormStatusText.Text = "Wähle zuerst eine Person aus.";
            return;
        }

        if (RelationshipTargetComboBox.SelectedItem is not PersonListItem targetPerson)
        {
            RelationshipFormStatusText.Text = "Bitte wähle eine zweite Person aus.";
            return;
        }

        if (targetPerson.Id == _currentPerson.Id)
        {
            RelationshipFormStatusText.Text = "Eine Person kann nicht mit sich selbst verknüpft werden.";
            return;
        }

        var relationship = new Relationship
        {
            PersonAId = _currentPerson.Id,
            PersonBId = targetPerson.Id,
            RelationshipType = (RelationshipTypeComboBox.SelectedItem as EnumDisplay<RelationshipType>)?.Value
                ?? RelationshipType.UnknownRelated,
            Direction = (RelationshipDirectionComboBox.SelectedItem as EnumDisplay<RelationshipDirection>)?.Value
                ?? RelationshipDirection.Undirected,
            CertaintyLevel = (RelationshipCertaintyComboBox.SelectedItem as EnumDisplay<CertaintyLevel>)?.Value
                ?? CertaintyLevel.Unknown,
            SourceNote = RelationshipSourceNoteTextBox.Text?.Trim() ?? string.Empty,
            Comment = RelationshipCommentTextBox.Text?.Trim() ?? string.Empty
        };

        try
        {
            await _relationshipRepository.SaveAsync(relationship);
            RelationshipFormStatusText.Text = "Beziehung wurde gespeichert.";
            RelationshipSourceNoteTextBox.Text = string.Empty;
            RelationshipCommentTextBox.Text = string.Empty;
            await RefreshRelationshipsAsync();
            await RefreshStatisticsAsync();
        }
        catch (Exception exception)
        {
            RelationshipFormStatusText.Text = $"Beziehung konnte nicht gespeichert werden: {exception.Message}";
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
        _currentWorkspace = workspace;
        _personRepository = new PersonRepository(workspace.DatabasePath);
        _relationshipRepository = new RelationshipRepository(workspace.DatabasePath);
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
        SaveRelationshipButton.IsEnabled = true;
        PersonFormStatusText.Text = "Bereit für die erste Person.";

        await RefreshPeopleAsync();
        await RefreshRelationshipsAsync();
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

    private void InitializeRelationshipForm()
    {
        RelationshipTypeComboBox.ItemsSource = new[]
        {
            new EnumDisplay<RelationshipType>(RelationshipType.ParentChild, "Eltern-Kind"),
            new EnumDisplay<RelationshipType>(RelationshipType.Spouse, "Partner / Ehe"),
            new EnumDisplay<RelationshipType>(RelationshipType.Sibling, "Geschwister"),
            new EnumDisplay<RelationshipType>(RelationshipType.AdoptiveParent, "Adoptivbeziehung"),
            new EnumDisplay<RelationshipType>(RelationshipType.LegalParent, "rechtliche Elternschaft"),
            new EnumDisplay<RelationshipType>(RelationshipType.TribeMember, "Stammeszugehörigkeit"),
            new EnumDisplay<RelationshipType>(RelationshipType.UnknownRelated, "unbekannt verwandt"),
            new EnumDisplay<RelationshipType>(RelationshipType.Custom, "benutzerdefiniert")
        };

        RelationshipDirectionComboBox.ItemsSource = new[]
        {
            new EnumDisplay<RelationshipDirection>(RelationshipDirection.Undirected, "ungerichtet"),
            new EnumDisplay<RelationshipDirection>(RelationshipDirection.PersonAToPersonB, "aktuelle Person -> Zielperson"),
            new EnumDisplay<RelationshipDirection>(RelationshipDirection.PersonBToPersonA, "Zielperson -> aktuelle Person")
        };

        RelationshipCertaintyComboBox.ItemsSource = new[]
        {
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.ExplicitlyMentioned, "ausdrücklich erwähnt"),
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.Likely, "wahrscheinlich"),
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.Possible, "möglich"),
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.Traditional, "traditionell angenommen"),
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.Disputed, "umstritten"),
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.UserHypothesis, "eigene Arbeitshypothese"),
            new EnumDisplay<CertaintyLevel>(CertaintyLevel.Unknown, "unbekannt")
        };

        RelationshipTypeComboBox.SelectedIndex = 0;
        RelationshipDirectionComboBox.SelectedIndex = 0;
        RelationshipCertaintyComboBox.SelectedIndex = 6;
    }

    private async Task RefreshPeopleAsync()
    {
        if (_personRepository is null)
        {
            PeopleListBox.ItemsSource = Array.Empty<PersonListItem>();
            PeopleEmptyText.Text = "Noch kein Projekt geöffnet.";
            return;
        }

        _people = await _personRepository.SearchAsync(PersonSearchTextBox.Text ?? string.Empty);
        PeopleListBox.ItemsSource = _people
            .Select(person => new PersonListItem(person.Id, person.MainName, person.PrimaryRole))
            .ToList();
        RelationshipTargetComboBox.ItemsSource = _people
            .Where(person => _currentPerson is null || person.Id != _currentPerson.Id)
            .Select(person => new PersonListItem(person.Id, person.MainName, person.PrimaryRole))
            .ToList();
        PeopleEmptyText.Text = _people.Count == 0
            ? "Noch keine passenden Personen gefunden."
            : $"{_people.Count} Person(en) gefunden.";
    }

    private async Task RefreshRelationshipsAsync()
    {
        if (_relationshipRepository is null || _currentPerson is null)
        {
            RelationshipsListBox.ItemsSource = Array.Empty<RelationshipListItem>();
            RelationshipsEmptyText.Text = "Wähle eine Person aus, um Beziehungen zu sehen.";
            TreePreviewText.Text = "Wähle eine Person aus, um Eltern, Partner und Kinder als einfache Vorschau zu sehen.";
            return;
        }

        var relationships = await _relationshipRepository.GetForPersonAsync(_currentPerson.Id);
        var relationshipItems = relationships
            .Select(relationship => CreateRelationshipListItem(_currentPerson.Id, relationship))
            .ToList();

        RelationshipsListBox.ItemsSource = relationshipItems;
        RelationshipsEmptyText.Text = relationshipItems.Count == 0
            ? "Für diese Person sind noch keine Beziehungen erfasst."
            : $"{relationshipItems.Count} Beziehung(en) erfasst.";
        RelationshipTargetComboBox.ItemsSource = _people
            .Where(person => person.Id != _currentPerson.Id)
            .Select(person => new PersonListItem(person.Id, person.MainName, person.PrimaryRole))
            .ToList();
        TreePreviewText.Text = BuildTreePreviewText(_currentPerson.Id, relationships);
    }

    private async Task RefreshStatisticsAsync()
    {
        if (_currentWorkspace is null)
        {
            return;
        }

        var statistics = await _projectService.ReadStatisticsAsync(_currentWorkspace);
        PersonCountText.Text = statistics.PersonCount.ToString();
        RelationshipCountText.Text = $"{statistics.RelationshipCount} Beziehungen vorbereitet";
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

    private RelationshipListItem CreateRelationshipListItem(Guid currentPersonId, Relationship relationship)
    {
        var otherPersonId = relationship.PersonAId == currentPersonId
            ? relationship.PersonBId
            : relationship.PersonAId;
        var otherPersonName = FindPersonName(otherPersonId);
        var certainty = DisplayCertainty(relationship.CertaintyLevel);
        var type = DisplayRelationshipType(relationship.RelationshipType);

        return new RelationshipListItem(
            relationship.Id,
            $"{type}: {otherPersonName} ({certainty})",
            relationship.Comment);
    }

    private string BuildTreePreviewText(Guid currentPersonId, IReadOnlyList<Relationship> relationships)
    {
        var parents = new List<string>();
        var partners = new List<string>();
        var children = new List<string>();

        foreach (var relationship in relationships)
        {
            var otherPersonId = relationship.PersonAId == currentPersonId
                ? relationship.PersonBId
                : relationship.PersonAId;
            var name = FindPersonName(otherPersonId);

            if (relationship.RelationshipType == RelationshipType.Spouse)
            {
                partners.Add(name);
                continue;
            }

            if (relationship.RelationshipType != RelationshipType.ParentChild)
            {
                continue;
            }

            if (IsCurrentPersonParent(currentPersonId, relationship))
            {
                children.Add(name);
            }
            else
            {
                parents.Add(name);
            }
        }

        return $"Eltern: {FormatTreeGroup(parents)} | Partner: {FormatTreeGroup(partners)} | Kinder: {FormatTreeGroup(children)}";
    }

    private static bool IsCurrentPersonParent(Guid currentPersonId, Relationship relationship)
    {
        return relationship.Direction switch
        {
            RelationshipDirection.PersonAToPersonB => relationship.PersonAId == currentPersonId,
            RelationshipDirection.PersonBToPersonA => relationship.PersonBId == currentPersonId,
            _ => false
        };
    }

    private string FindPersonName(Guid personId)
    {
        return _people.FirstOrDefault(person => person.Id == personId)?.MainName ?? "Unbekannte Person";
    }

    private static string FormatTreeGroup(IReadOnlyCollection<string> names)
    {
        return names.Count == 0 ? "keine" : string.Join(", ", names);
    }

    private static string DisplayRelationshipType(RelationshipType relationshipType)
    {
        return relationshipType switch
        {
            RelationshipType.ParentChild => "Eltern-Kind",
            RelationshipType.Spouse => "Partner / Ehe",
            RelationshipType.Sibling => "Geschwister",
            RelationshipType.AdoptiveParent => "Adoptivbeziehung",
            RelationshipType.LegalParent => "rechtliche Elternschaft",
            RelationshipType.TribeMember => "Stammeszugehörigkeit",
            RelationshipType.Custom => "benutzerdefiniert",
            _ => "unbekannt verwandt"
        };
    }

    private static string DisplayCertainty(CertaintyLevel certaintyLevel)
    {
        return certaintyLevel switch
        {
            CertaintyLevel.ExplicitlyMentioned => "ausdrücklich erwähnt",
            CertaintyLevel.Likely => "wahrscheinlich",
            CertaintyLevel.Possible => "möglich",
            CertaintyLevel.Traditional => "traditionell angenommen",
            CertaintyLevel.Disputed => "umstritten",
            CertaintyLevel.UserHypothesis => "eigene Arbeitshypothese",
            _ => "unbekannt"
        };
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

    private sealed record RelationshipListItem(Guid Id, string Title, string Comment)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Comment)
                ? Title
                : $"{Title} - {Comment}";
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
