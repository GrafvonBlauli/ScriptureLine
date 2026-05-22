using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using BibleStudyGenealogy.Rendering.TreeLayout;
using System.Text.Json;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.App;

public partial class MainWindow : Window
{
    private readonly IProjectService _projectService = new LocalProjectService();
    private readonly AppStateStore _appStateStore = new();
    private IPersonRepository? _personRepository;
    private IRelationshipRepository? _relationshipRepository;
    private IEventRepository? _eventRepository;
    private IBibleReferenceRepository? _bibleReferenceRepository;
    private IMediaRepository? _mediaRepository;
    private readonly FamilyTreeBuilder _familyTreeBuilder = new();
    private readonly MediaImportService _mediaImportService = new();
    private ProjectWorkspace? _currentWorkspace;
    private Person? _currentPerson;
    private bool _isCurrentPersonPersisted;
    private Relationship? _currentRelationship;
    private ScriptureEvent? _currentEvent;
    private BibleReference? _currentBibleReference;
    private MediaFile? _currentMediaFile;
    private IReadOnlyList<Person> _people = Array.Empty<Person>();
    private IReadOnlyList<Relationship> _currentRelationships = Array.Empty<Relationship>();
    private IReadOnlyList<ScriptureEvent> _currentEvents = Array.Empty<ScriptureEvent>();
    private IReadOnlyList<MediaFile> _mediaFiles = Array.Empty<MediaFile>();

    public MainWindow()
    {
        InitializeComponent();
        InitializePersonForm();
        InitializeRelationshipForm();
        InitializeEventForm();
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
        _isCurrentPersonPersisted = false;
        _currentRelationship = null;
        ClearPersonForm();
        PersonFormStatusText.Text = "Neue Person vorbereiten.";
        RelationshipFormStatusText.Text = "Speichere die Person, bevor du Beziehungen anlegst.";
        EventFormStatusText.Text = "Speichere die Person, bevor du ein Ereignis mit ihr verknüpfst.";
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
            _isCurrentPersonPersisted = true;
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
        _isCurrentPersonPersisted = _currentPerson is not null;
        if (_currentPerson is null)
        {
            PersonFormStatusText.Text = "Person wurde nicht gefunden.";
            return;
        }

        FillFormFromPerson(_currentPerson);
        PersonFormStatusText.Text = "Person geladen.";
        await RefreshRelationshipsAsync();
        await RefreshEventsAsync();
        await RefreshMediaFilesAsync();
    }

    private async void SaveRelationshipButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_relationshipRepository is null || _currentPerson is null || !_isCurrentPersonPersisted)
        {
            RelationshipFormStatusText.Text = "Wähle zuerst eine gespeicherte Person aus.";
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

        var relationship = _currentRelationship ?? new Relationship();
        FillRelationshipFromForm(relationship, _currentPerson.Id, targetPerson.Id);

        try
        {
            await _relationshipRepository.SaveAsync(relationship);
            _currentRelationship = relationship;
            RelationshipFormStatusText.Text = "Beziehung wurde gespeichert.";
            await RefreshRelationshipsAsync();
            await RefreshStatisticsAsync();
        }
        catch (Exception exception)
        {
            RelationshipFormStatusText.Text = $"Beziehung konnte nicht gespeichert werden: {exception.Message}";
        }
    }

    private async void RelationshipsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (RelationshipsListBox.SelectedItem is not RelationshipListItem selectedRelationship || _relationshipRepository is null)
        {
            return;
        }

        _currentRelationship = await _relationshipRepository.GetByIdAsync(selectedRelationship.Id);
        if (_currentRelationship is null)
        {
            RelationshipFormStatusText.Text = "Beziehung wurde nicht gefunden.";
            ArchiveRelationshipButton.IsEnabled = false;
            return;
        }

        FillRelationshipForm(_currentRelationship);
        RelationshipFormStatusText.Text = "Beziehung geladen und kann bearbeitet werden.";
        ArchiveRelationshipButton.IsEnabled = true;
    }

    private async void ArchiveRelationshipButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentRelationship is null || _relationshipRepository is null)
        {
            RelationshipFormStatusText.Text = "Wähle zuerst eine Beziehung aus.";
            return;
        }

        await _relationshipRepository.ArchiveAsync(_currentRelationship.Id);
        _currentRelationship = null;
        ClearRelationshipForm();
        RelationshipFormStatusText.Text = "Beziehung wurde archiviert.";
        ArchiveRelationshipButton.IsEnabled = false;
        await RefreshRelationshipsAsync();
        await RefreshStatisticsAsync();
    }

    private void CreateEventButton_Click(object? sender, RoutedEventArgs e)
    {
        _currentEvent = new ScriptureEvent();
        ClearEventForm();
        EventFormStatusText.Text = _currentPerson is null
            ? "Wähle eine Person aus, um ein Ereignis direkt zu verknüpfen."
            : $"Neues Ereignis für {_currentPerson.MainName} vorbereiten.";
        EventTitleTextBox.Focus();
    }

    private async void SaveEventButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_eventRepository is null || _currentWorkspace is null)
        {
            EventFormStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
            return;
        }

        if (string.IsNullOrWhiteSpace(EventTitleTextBox.Text))
        {
            EventFormStatusText.Text = "Bitte gib einen Ereignistitel ein.";
            return;
        }

        _currentEvent ??= new ScriptureEvent();
        FillEventFromForm(_currentEvent);

        if (_currentPerson is not null && !_isCurrentPersonPersisted)
        {
            EventFormStatusText.Text = "Speichere die Person zuerst, bevor du ein Ereignis mit ihr verknüpfst.";
            return;
        }

        try
        {
            await _eventRepository.SaveAsync(_currentEvent);
            if (_currentPerson is not null)
            {
                await _eventRepository.LinkPersonAsync(_currentEvent.Id, _currentPerson.Id);
            }

            EventFormStatusText.Text = _currentPerson is null
                ? "Ereignis wurde gespeichert."
                : "Ereignis wurde gespeichert und mit der ausgewählten Person verbunden.";
            await RefreshEventsAsync();
            await RefreshStatisticsAsync();
        }
        catch (Exception exception)
        {
            EventFormStatusText.Text = $"Ereignis konnte nicht gespeichert werden: {exception.Message}";
        }
    }

    private async void EventsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (EventsListBox.SelectedItem is not EventListItem selectedEvent || _eventRepository is null)
        {
            return;
        }

        _currentEvent = await _eventRepository.GetByIdAsync(selectedEvent.Id);
        if (_currentEvent is null)
        {
            EventFormStatusText.Text = "Ereignis wurde nicht gefunden.";
            return;
        }

        FillEventForm(_currentEvent);
        EventFormStatusText.Text = "Ereignis geladen und kann bearbeitet werden.";
        UpdateMediaActionButtons();
    }

    private async void SaveBibleReferenceButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_bibleReferenceRepository is null || _currentWorkspace is null)
        {
            BibleReferenceFormStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
            return;
        }

        if (string.IsNullOrWhiteSpace(BibleBookTextBox.Text) || !TryReadInt(BibleChapterStartTextBox.Text, out var chapterStart))
        {
            BibleReferenceFormStatusText.Text = "Bitte gib mindestens Buch und Startkapitel ein.";
            return;
        }

        _currentBibleReference ??= new BibleReference();
        FillBibleReferenceFromForm(_currentBibleReference, chapterStart);

        try
        {
            await _bibleReferenceRepository.SaveAsync(_currentBibleReference);
            BibleReferenceFormStatusText.Text = "Bibelstelle wurde gespeichert.";
            await RefreshBibleReferencesAsync();
            await RefreshStatisticsAsync();
        }
        catch (Exception exception)
        {
            BibleReferenceFormStatusText.Text = $"Bibelstelle konnte nicht gespeichert werden: {exception.Message}";
        }
    }

    private async void BibleReferencesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (BibleReferencesListBox.SelectedItem is not BibleReferenceListItem selectedReference || _bibleReferenceRepository is null)
        {
            return;
        }

        _currentBibleReference = await _bibleReferenceRepository.GetByIdAsync(selectedReference.Id);
        if (_currentBibleReference is null)
        {
            BibleReferenceFormStatusText.Text = "Bibelstelle wurde nicht gefunden.";
            return;
        }

        FillBibleReferenceForm(_currentBibleReference);
        BibleReferenceFormStatusText.Text = "Bibelstelle geladen und kann bearbeitet werden.";
    }

    private async void MediaSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        await RefreshMediaFilesAsync();
    }

    private async void ImportMediaButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkspace is null || _mediaRepository is null)
        {
            MediaFormStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Datei in die Mediathek importieren",
            AllowMultiple = false
        });

        var sourcePath = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (sourcePath is null)
        {
            return;
        }

        try
        {
            var mediaFile = await _mediaImportService.ImportAsync(
                _currentWorkspace,
                sourcePath,
                MediaDescriptionTextBox.Text ?? string.Empty);
            await _mediaRepository.SaveAsync(mediaFile);
            _currentMediaFile = mediaFile;
            MediaFormStatusText.Text = "Datei wurde in das Projekt importiert.";
            await RefreshMediaFilesAsync();
            await RefreshStatisticsAsync();
            FillMediaForm(mediaFile);
        }
        catch (Exception exception)
        {
            MediaFormStatusText.Text = $"Datei konnte nicht importiert werden: {exception.Message}";
        }
    }

    private async void SaveMediaButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_mediaRepository is null || _currentMediaFile is null)
        {
            MediaFormStatusText.Text = "Wähle zuerst ein Medium aus.";
            return;
        }

        _currentMediaFile.Description = MediaDescriptionTextBox.Text?.Trim() ?? string.Empty;
        await _mediaRepository.SaveAsync(_currentMediaFile);
        MediaFormStatusText.Text = "Medium wurde gespeichert.";
        await RefreshMediaFilesAsync();
    }

    private async void MediaFilesListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MediaFilesListBox.SelectedItem is not MediaListItem selectedMedia || _mediaRepository is null)
        {
            return;
        }

        _currentMediaFile = await _mediaRepository.GetByIdAsync(selectedMedia.Id);
        if (_currentMediaFile is null)
        {
            MediaFormStatusText.Text = "Medium wurde nicht gefunden.";
            UpdateMediaActionButtons();
            return;
        }

        FillMediaForm(_currentMediaFile);
        MediaFormStatusText.Text = "Medium geladen.";
        UpdateMediaActionButtons();
    }

    private async void LinkMediaToPersonButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_mediaRepository is null || _currentMediaFile is null || _currentPerson is null || !_isCurrentPersonPersisted)
        {
            MediaFormStatusText.Text = "Wähle ein Medium und eine gespeicherte Person aus.";
            return;
        }

        await _mediaRepository.LinkAsync(_currentMediaFile.Id, LinkedEntityType.Person, _currentPerson.Id);
        MediaFormStatusText.Text = $"Medium wurde mit {_currentPerson.MainName} verknüpft.";
    }

    private async void LinkMediaToEventButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_mediaRepository is null || _eventRepository is null || _currentMediaFile is null || _currentEvent is null)
        {
            MediaFormStatusText.Text = "Wähle ein Medium und ein gespeichertes Ereignis aus.";
            return;
        }

        var persistedEvent = await _eventRepository.GetByIdAsync(_currentEvent.Id);
        if (persistedEvent is null)
        {
            MediaFormStatusText.Text = "Speichere oder wähle zuerst ein Ereignis aus.";
            return;
        }

        await _mediaRepository.LinkAsync(_currentMediaFile.Id, LinkedEntityType.Event, _currentEvent.Id);
        MediaFormStatusText.Text = $"Medium wurde mit {persistedEvent.Title} verknüpft.";
    }

    private async void SetPortraitButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_personRepository is null || _currentPerson is null || !_isCurrentPersonPersisted || _currentMediaFile is null)
        {
            MediaFormStatusText.Text = "Wähle ein Bild und eine gespeicherte Person aus.";
            return;
        }

        if (_currentMediaFile.MediaType != MediaType.Image)
        {
            MediaFormStatusText.Text = "Nur Bilder können als Portrait gesetzt werden.";
            return;
        }

        _currentPerson.PortraitMediaFileId = _currentMediaFile.Id;
        await _personRepository.SaveAsync(_currentPerson);
        MediaFormStatusText.Text = $"Portrait für {_currentPerson.MainName} wurde gesetzt.";
        PersonFormStatusText.Text = "Portrait wurde gespeichert.";
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
        _eventRepository = new EventRepository(workspace.DatabasePath);
        _bibleReferenceRepository = new BibleReferenceRepository(workspace.DatabasePath);
        _mediaRepository = new MediaRepository(workspace.DatabasePath);
        _currentPerson = null;
        _isCurrentPersonPersisted = false;
        _currentRelationship = null;
        _currentEvent = null;
        _currentBibleReference = null;
        _currentMediaFile = null;
        var statistics = await _projectService.ReadStatisticsAsync(workspace);

        SidebarProjectTitle.Text = workspace.Settings.ProjectName;
        SidebarProjectStatus.Text = "Projekt geöffnet";
        ProjectStatusText.Text = $"{status} Speicherort: {workspace.RootDirectory}";
        CurrentProjectTitle.Text = workspace.Settings.ProjectName;
        CurrentProjectDetails.Text =
            $"Lokales Projekt mit SQLite-Datenbank. Bevorzugte Übersetzung: {workspace.Settings.PreferredBibleTranslation}.";

        PersonCountText.Text = statistics.PersonCount.ToString();
        RelationshipCountText.Text = statistics.RelationshipCount.ToString();
        EventCountText.Text = $"{statistics.EventCount} Ereignisse";
        BibleReferenceCountText.Text = $"{statistics.BibleReferenceCount} Bibelstellen";
        MediaFileCountText.Text = $"{statistics.MediaFileCount} Medien";
        PlaceCountText.Text = statistics.PlaceCount.ToString();
        ResearchQuestionCountText.Text = $"{statistics.ResearchQuestionCount} offene Fragen";
        CreatePersonButton.IsEnabled = true;
        QuickAddPersonButton.IsEnabled = true;
        QuickAddEventButton.IsEnabled = true;
        SavePersonButton.IsEnabled = true;
        SaveRelationshipButton.IsEnabled = true;
        SaveEventButton.IsEnabled = true;
        SaveBibleReferenceButton.IsEnabled = true;
        ImportMediaButton.IsEnabled = true;
        SaveMediaButton.IsEnabled = false;
        PersonFormStatusText.Text = "Bereit für die erste Person.";
        EventFormStatusText.Text = "Bereit für Ereignisse.";
        BibleReferenceFormStatusText.Text = "Bereit für Bibelstellen.";
        MediaFormStatusText.Text = "Bereit für Medien.";

        await RefreshPeopleAsync();
        await RefreshRelationshipsAsync();
        await RefreshEventsAsync();
        await RefreshBibleReferencesAsync();
        await RefreshMediaFilesAsync();
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
        GenderComboBox.ItemsSource = DisplayOptions.Genders();
        PersonStatusComboBox.ItemsSource = DisplayOptions.PersonStatuses();
        GenderComboBox.SelectedIndex = 0;
        PersonStatusComboBox.SelectedIndex = 0;
    }

    private void InitializeRelationshipForm()
    {
        RelationshipTypeComboBox.ItemsSource = DisplayOptions.RelationshipTypes();
        RelationshipDirectionComboBox.ItemsSource = DisplayOptions.RelationshipDirections();
        RelationshipCertaintyComboBox.ItemsSource = DisplayOptions.CertaintyLevels();
        RelationshipTypeComboBox.SelectedIndex = 0;
        RelationshipDirectionComboBox.SelectedIndex = 0;
        RelationshipCertaintyComboBox.SelectedIndex = 6;
    }

    private void InitializeEventForm()
    {
        EventTypeComboBox.ItemsSource = DisplayOptions.EventTypes();
        EventCertaintyComboBox.ItemsSource = DisplayOptions.CertaintyLevels();
        EventTypeComboBox.SelectedIndex = 10;
        EventCertaintyComboBox.SelectedIndex = 6;
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
            TreeParentsText.Text = "Eltern: keine";
            TreeFocusText.Text = "Fokusperson: keine ausgewählt";
            TreePartnersText.Text = "Partner: keine";
            TreeChildrenText.Text = "Kinder: keine";
            TreeOtherText.Text = "Weitere oder unsichere Beziehungen: keine";
            return;
        }

        _currentRelationships = await _relationshipRepository.GetForPersonAsync(_currentPerson.Id);
        var relationshipItems = _currentRelationships
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
        RefreshTreePreview();
    }

    private async Task RefreshEventsAsync()
    {
        if (_eventRepository is null)
        {
            EventsListBox.ItemsSource = Array.Empty<EventListItem>();
            EventsEmptyText.Text = "Noch kein Projekt geöffnet.";
            return;
        }

        _currentEvents = _currentPerson is null
            ? await _eventRepository.SearchAsync(string.Empty)
            : await _eventRepository.GetForPersonAsync(_currentPerson.Id);
        var eventItems = _currentEvents
            .Select(scriptureEvent => new EventListItem(
                scriptureEvent.Id,
                $"{DisplayText.For(scriptureEvent.EventType)}: {scriptureEvent.Title}",
                scriptureEvent.ShortDescription))
            .ToList();

        EventsListBox.ItemsSource = eventItems;
        EventsEmptyText.Text = eventItems.Count == 0
            ? _currentPerson is null
                ? "Noch keine Ereignisse erfasst."
                : "Für diese Person sind noch keine Ereignisse erfasst."
            : $"{eventItems.Count} Ereignis(se) erfasst.";
    }

    private async Task RefreshBibleReferencesAsync()
    {
        if (_bibleReferenceRepository is null)
        {
            BibleReferencesListBox.ItemsSource = Array.Empty<BibleReferenceListItem>();
            BibleReferencesEmptyText.Text = "Noch kein Projekt geöffnet.";
            return;
        }

        var references = await _bibleReferenceRepository.SearchAsync(string.Empty);
        var referenceItems = references
            .Select(reference => new BibleReferenceListItem(
                reference.Id,
                FormatBibleReferenceTitle(reference),
                reference.UserSummary))
            .ToList();

        BibleReferencesListBox.ItemsSource = referenceItems;
        BibleReferencesEmptyText.Text = referenceItems.Count == 0
            ? "Noch keine Bibelstellen erfasst."
            : $"{referenceItems.Count} Bibelstelle(n) erfasst.";
    }

    private async Task RefreshMediaFilesAsync()
    {
        if (_mediaRepository is null)
        {
            MediaFilesListBox.ItemsSource = Array.Empty<MediaListItem>();
            MediaFilesEmptyText.Text = "Noch kein Projekt geöffnet.";
            return;
        }

        _mediaFiles = await _mediaRepository.SearchAsync(MediaSearchTextBox.Text ?? string.Empty);
        var mediaItems = _mediaFiles
            .Select(mediaFile => new MediaListItem(
                mediaFile.Id,
                FormatMediaTitle(mediaFile),
                mediaFile.Description))
            .ToList();

        MediaFilesListBox.ItemsSource = mediaItems;
        MediaFilesEmptyText.Text = mediaItems.Count == 0
            ? "Noch keine Medien importiert."
            : $"{mediaItems.Count} Medium/Medien gefunden.";
        UpdateMediaActionButtons();
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
        EventCountText.Text = $"{statistics.EventCount} Ereignisse";
        BibleReferenceCountText.Text = $"{statistics.BibleReferenceCount} Bibelstellen";
        MediaFileCountText.Text = $"{statistics.MediaFileCount} Medien";
        LastEditedText.Text = _currentPerson is null
            ? "Noch keine Person bearbeitet"
            : $"{_currentPerson.MainName} wurde zuletzt gespeichert.";
    }

    private void FillRelationshipFromForm(Relationship relationship, Guid personAId, Guid personBId)
    {
        relationship.PersonAId = personAId;
        relationship.PersonBId = personBId;
        relationship.RelationshipType = (RelationshipTypeComboBox.SelectedItem as EnumDisplay<RelationshipType>)?.Value
            ?? RelationshipType.UnknownRelated;
        relationship.Direction = (RelationshipDirectionComboBox.SelectedItem as EnumDisplay<RelationshipDirection>)?.Value
            ?? RelationshipDirection.Undirected;
        relationship.CertaintyLevel = (RelationshipCertaintyComboBox.SelectedItem as EnumDisplay<CertaintyLevel>)?.Value
            ?? CertaintyLevel.Unknown;
        relationship.SourceNote = RelationshipSourceNoteTextBox.Text?.Trim() ?? string.Empty;
        relationship.Comment = RelationshipCommentTextBox.Text?.Trim() ?? string.Empty;
        relationship.Status = RelationshipStatus.Active;
    }

    private void FillRelationshipForm(Relationship relationship)
    {
        var targetPersonId = relationship.PersonAId == _currentPerson?.Id
            ? relationship.PersonBId
            : relationship.PersonAId;

        SelectComboItem(RelationshipTargetComboBox, (PersonListItem item) => item.Id == targetPersonId);
        SelectEnumValue(RelationshipTypeComboBox, relationship.RelationshipType);
        SelectEnumValue(RelationshipDirectionComboBox, relationship.Direction);
        SelectEnumValue(RelationshipCertaintyComboBox, relationship.CertaintyLevel);
        RelationshipSourceNoteTextBox.Text = relationship.SourceNote;
        RelationshipCommentTextBox.Text = relationship.Comment;
    }

    private void ClearRelationshipForm()
    {
        RelationshipTargetComboBox.SelectedItem = null;
        RelationshipTypeComboBox.SelectedIndex = 0;
        RelationshipDirectionComboBox.SelectedIndex = 0;
        RelationshipCertaintyComboBox.SelectedIndex = 6;
        RelationshipSourceNoteTextBox.Text = string.Empty;
        RelationshipCommentTextBox.Text = string.Empty;
    }

    private void FillEventFromForm(ScriptureEvent scriptureEvent)
    {
        scriptureEvent.Title = EventTitleTextBox.Text?.Trim() ?? string.Empty;
        scriptureEvent.EventType = (EventTypeComboBox.SelectedItem as EnumDisplay<EventType>)?.Value ?? EventType.Other;
        scriptureEvent.CertaintyLevel = (EventCertaintyComboBox.SelectedItem as EnumDisplay<CertaintyLevel>)?.Value ?? CertaintyLevel.Unknown;
        scriptureEvent.ShortDescription = EventShortDescriptionTextBox.Text?.Trim() ?? string.Empty;
        scriptureEvent.LongDescription = EventLongDescriptionTextBox.Text?.Trim() ?? string.Empty;
        var dateText = EventDateTextBox.Text?.Trim() ?? string.Empty;
        scriptureEvent.DateInfo = string.IsNullOrWhiteSpace(dateText)
            ? null
            : new DateInfo
            {
                ApproximationText = dateText,
                DateType = DateType.Unknown,
                CertaintyLevel = scriptureEvent.CertaintyLevel
            };
    }

    private void FillEventForm(ScriptureEvent scriptureEvent)
    {
        EventTitleTextBox.Text = scriptureEvent.Title;
        SelectEnumValue(EventTypeComboBox, scriptureEvent.EventType);
        SelectEnumValue(EventCertaintyComboBox, scriptureEvent.CertaintyLevel);
        EventDateTextBox.Text = scriptureEvent.DateInfo?.ApproximationText ?? string.Empty;
        EventShortDescriptionTextBox.Text = scriptureEvent.ShortDescription;
        EventLongDescriptionTextBox.Text = scriptureEvent.LongDescription;
    }

    private void ClearEventForm()
    {
        EventTitleTextBox.Text = string.Empty;
        EventTypeComboBox.SelectedIndex = 10;
        EventCertaintyComboBox.SelectedIndex = 6;
        EventDateTextBox.Text = string.Empty;
        EventShortDescriptionTextBox.Text = string.Empty;
        EventLongDescriptionTextBox.Text = string.Empty;
    }

    private void FillBibleReferenceFromForm(BibleReference bibleReference, int chapterStart)
    {
        bibleReference.Translation = BibleTranslationTextBox.Text?.Trim() ?? string.Empty;
        bibleReference.Book = BibleBookTextBox.Text?.Trim() ?? string.Empty;
        bibleReference.ChapterStart = chapterStart;
        bibleReference.VerseStart = ReadOptionalInt(BibleVerseStartTextBox.Text);
        bibleReference.ChapterEnd = ReadOptionalInt(BibleChapterEndTextBox.Text);
        bibleReference.VerseEnd = ReadOptionalInt(BibleVerseEndTextBox.Text);
        bibleReference.ReferenceText = BibleReferenceTextBox.Text?.Trim() ?? string.Empty;
        bibleReference.UserSummary = BibleSummaryTextBox.Text?.Trim() ?? string.Empty;
        bibleReference.UserComment = BibleCommentTextBox.Text?.Trim() ?? string.Empty;
    }

    private void FillBibleReferenceForm(BibleReference bibleReference)
    {
        BibleTranslationTextBox.Text = bibleReference.Translation;
        BibleBookTextBox.Text = bibleReference.Book;
        BibleChapterStartTextBox.Text = bibleReference.ChapterStart.ToString();
        BibleVerseStartTextBox.Text = bibleReference.VerseStart?.ToString() ?? string.Empty;
        BibleChapterEndTextBox.Text = bibleReference.ChapterEnd?.ToString() ?? string.Empty;
        BibleVerseEndTextBox.Text = bibleReference.VerseEnd?.ToString() ?? string.Empty;
        BibleReferenceTextBox.Text = bibleReference.ReferenceText;
        BibleSummaryTextBox.Text = bibleReference.UserSummary;
        BibleCommentTextBox.Text = bibleReference.UserComment;
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
        var certainty = DisplayText.For(relationship.CertaintyLevel);
        var type = DisplayText.For(relationship.RelationshipType);

        return new RelationshipListItem(
            relationship.Id,
            $"{type}: {otherPersonName} ({certainty})",
            relationship.Comment);
    }

    private void RefreshTreePreview()
    {
        if (_currentPerson is null)
        {
            return;
        }

        var snapshot = _familyTreeBuilder.Build(_currentPerson, _people, _currentRelationships);
        TreeParentsText.Text = $"Eltern: {FormatTreeGroup(snapshot.Parents.Select(node => node.DisplayName).ToList())}";
        TreeFocusText.Text = $"Fokusperson: {snapshot.FocusPerson.DisplayName}";
        TreePartnersText.Text = $"Partner: {FormatTreeGroup(snapshot.Partners.Select(node => node.DisplayName).ToList())}";
        TreeChildrenText.Text = $"Kinder: {FormatTreeGroup(snapshot.Children.Select(node => node.DisplayName).ToList())}";
        TreeOtherText.Text = $"Weitere oder unsichere Beziehungen: {FormatTreeGroup(snapshot.OtherRelations.Select(node => node.DisplayName).ToList())}";
        TreePreviewText.Text = snapshot.Links.Any(link => link.IsUncertain)
            ? "Unsichere oder ungerichtete Beziehungen werden in der Vorschau unter weitere Beziehungen geführt."
            : "Die gespeicherten Beziehungen wurden in die einfache Stammbaum-Vorschau übernommen.";
    }

    private string FindPersonName(Guid personId)
    {
        return _people.FirstOrDefault(person => person.Id == personId)?.MainName ?? "Unbekannte Person";
    }

    private static string FormatBibleReferenceTitle(BibleReference reference)
    {
        var versePart = reference.VerseStart is null
            ? string.Empty
            : $",{reference.VerseStart}";
        var endPart = reference.ChapterEnd is null && reference.VerseEnd is null
            ? string.Empty
            : $"-{reference.ChapterEnd?.ToString() ?? reference.ChapterStart.ToString()}{(reference.VerseEnd is null ? string.Empty : $",{reference.VerseEnd}")}";
        var translation = string.IsNullOrWhiteSpace(reference.Translation)
            ? string.Empty
            : $" ({reference.Translation})";

        return $"{reference.Book} {reference.ChapterStart}{versePart}{endPart}{translation}";
    }

    private string FormatMediaTitle(MediaFile mediaFile)
    {
        var availability = _currentWorkspace is not null && _mediaImportService.FileExists(_currentWorkspace, mediaFile)
            ? "vorhanden"
            : "fehlt";

        return $"{DisplayText.For(mediaFile.MediaType)}: {mediaFile.OriginalFileName} ({availability})";
    }

    private void FillMediaForm(MediaFile mediaFile)
    {
        MediaDescriptionTextBox.Text = mediaFile.Description;
        var availabilityText = _currentWorkspace is not null && _mediaImportService.FileExists(_currentWorkspace, mediaFile)
            ? "Datei vorhanden"
            : "Datei fehlt im Projektordner";
        SelectedMediaText.Text =
            $"{DisplayText.For(mediaFile.MediaType)} - {mediaFile.OriginalFileName}\n{mediaFile.RelativePath}\n{availabilityText}";
        UpdateMediaActionButtons();
    }

    private void UpdateMediaActionButtons()
    {
        var hasProject = _currentWorkspace is not null;
        var hasMedia = _currentMediaFile is not null;
        ImportMediaButton.IsEnabled = hasProject;
        SaveMediaButton.IsEnabled = hasProject && hasMedia;
        LinkMediaToPersonButton.IsEnabled = hasProject && hasMedia && _currentPerson is not null && _isCurrentPersonPersisted;
        LinkMediaToEventButton.IsEnabled = hasProject && hasMedia && _currentEvent is not null;
        SetPortraitButton.IsEnabled = hasProject
            && hasMedia
            && _currentMediaFile?.MediaType == MediaType.Image
            && _currentPerson is not null
            && _isCurrentPersonPersisted;
    }

    private static bool TryReadInt(string? value, out int result)
    {
        return int.TryParse(value?.Trim(), out result) && result > 0;
    }

    private static int? ReadOptionalInt(string? value)
    {
        return TryReadInt(value, out var result) ? result : null;
    }

    private static string FormatTreeGroup(IReadOnlyCollection<string> names)
    {
        return names.Count == 0 ? "keine" : string.Join(", ", names);
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

    private static void SelectComboItem<T>(ComboBox comboBox, Predicate<T> predicate)
    {
        foreach (var item in comboBox.Items)
        {
            if (item is T typedItem && predicate(typedItem))
            {
                comboBox.SelectedItem = typedItem;
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

    private sealed record EventListItem(Guid Id, string Title, string Description)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Description)
                ? Title
                : $"{Title} - {Description}";
        }
    }

    private sealed record BibleReferenceListItem(Guid Id, string Title, string Summary)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Summary)
                ? Title
                : $"{Title} - {Summary}";
        }
    }

    private sealed record MediaListItem(Guid Id, string Title, string Description)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Description)
                ? Title
                : $"{Title} - {Description}";
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
