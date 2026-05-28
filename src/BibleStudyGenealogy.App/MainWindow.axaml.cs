using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Core.Services;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using BibleStudyGenealogy.Rendering.Timeline;
using BibleStudyGenealogy.Rendering.TreeLayout;
using System.Text.Json;
using PathShape = Avalonia.Controls.Shapes.Path;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.App;

public partial class MainWindow : Window
{
    private const double TreeCardWidth = FamilyTreeLayoutMetrics.NodeWidth;
    private const double TreeCardHeight = FamilyTreeLayoutMetrics.NodeHeight;
    private const double TreeCardAvatarSize = FamilyTreeLayoutMetrics.AvatarSize;

    private readonly IProjectService _projectService = new LocalProjectService();
    private readonly AppStateStore _appStateStore = new();
    private IPersonRepository? _personRepository;
    private IRelationshipRepository? _relationshipRepository;
    private IEventRepository? _eventRepository;
    private IBibleReferenceRepository? _bibleReferenceRepository;
    private IMediaRepository? _mediaRepository;
    private readonly FamilyTreeBuilder _familyTreeBuilder = new();
    private readonly BezierConnectionBuilder _bezierConnectionBuilder = new();
    private readonly FamilyTreeViewportService _familyTreeViewportService = new();
    private readonly TimelineBuilder _timelineBuilder = new();
    private readonly LifeDateCalculationService _lifeDateCalculationService = new();
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
    private IReadOnlyList<Person> _treePeople = Array.Empty<Person>();
    private AppModule _currentModule = AppModule.Dashboard;
    private double _familyTreeZoom = 1;
    private bool _isFamilyTreePanning;
    private Point _familyTreePanStartPoint;
    private Vector _familyTreePanStartOffset;
    private Guid? _treeSelectedPersonId;
    private Guid? _addRelativeSourcePersonId;
    private FamilyTreeDiagram? _currentFamilyTreeDiagram;

    public MainWindow()
    {
        InitializeComponent();
        InitializePersonForm();
        InitializeRelationshipForm();
        InitializeEventForm();
        InitializeFamilyTreeForm();
        ShowModule(AppModule.Dashboard);
        RestoreLastProject();
    }

    private void NavigateDashboardButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Dashboard);
    }

    private void NavigatePeopleButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.People);
    }

    private void NavigateFamilyTreeButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.FamilyTree);
    }

    private void NavigateTimelineButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Timeline);
    }

    private void NavigateMapButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Map);
    }

    private void NavigateEventsButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Events);
    }

    private void NavigatePlacesButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Places);
    }

    private void NavigateBibleReferencesButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.BibleReferences);
    }

    private void NavigateMediaButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Media);
    }

    private void NavigateResearchQuestionsButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.ResearchQuestions);
    }

    private void ShowModule(AppModule module)
    {
        _currentModule = module;
        var dashboardVisible = module == AppModule.Dashboard;
        DashboardHeroView.IsVisible = dashboardVisible;
        DashboardStatsView.IsVisible = dashboardVisible;
        DashboardProjectView.IsVisible = dashboardVisible;
        DashboardTreePreviewView.IsVisible = dashboardVisible;
        DashboardActionsView.IsVisible = dashboardVisible;

        PeopleView.IsVisible = module == AppModule.People;
        RelationshipsView.IsVisible = false;
        FamilyTreeView.IsVisible = module == AppModule.FamilyTree;
        TimelineView.IsVisible = module == AppModule.Timeline;
        EventsView.IsVisible = module == AppModule.Events;
        BibleReferencesView.IsVisible = module == AppModule.BibleReferences;
        MediaView.IsVisible = module == AppModule.Media;
        MapPlaceholderView.IsVisible = module == AppModule.Map;
        PlacesPlaceholderView.IsVisible = module == AppModule.Places;
        ResearchQuestionsPlaceholderView.IsVisible = module == AppModule.ResearchQuestions;

        SetNavActive(NavDashboardButton, module == AppModule.Dashboard);
        SetNavActive(NavPeopleButton, module == AppModule.People);
        SetNavActive(NavFamilyTreeButton, module == AppModule.FamilyTree);
        SetNavActive(NavTimelineButton, module == AppModule.Timeline);
        SetNavActive(NavMapButton, module == AppModule.Map);
        SetNavActive(NavEventsButton, module == AppModule.Events);
        SetNavActive(NavPlacesButton, module == AppModule.Places);
        SetNavActive(NavBibleReferencesButton, module == AppModule.BibleReferences);
        SetNavActive(NavMediaButton, module == AppModule.Media);
        SetNavActive(NavResearchQuestionsButton, module == AppModule.ResearchQuestions);
    }

    private static void SetNavActive(Button button, bool isActive)
    {
        button.Classes.Set("active", isActive);
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

    private async void CloseProjectButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_currentWorkspace is null)
        {
            SidebarProjectStatus.Text = "Kein Projekt geladen";
            ProjectStatusText.Text = "Es ist kein Projekt geöffnet.";
            return;
        }

        try
        {
            SetBusyStatus("Projekt wird gespeichert und geschlossen ...");
            var saveSummary = await SaveOpenEditorsBeforeCloseAsync();
            await _appStateStore.ClearAsync();
            await ClearProjectAsync($"Projekt wurde geschlossen. {saveSummary}");
        }
        catch (Exception exception)
        {
            ShowError($"Projekt konnte nicht geschlossen werden: {exception.Message}");
        }
    }

    private void CreatePersonButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.People);
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
            RefreshTimeline();
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
        RefreshTimeline();
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

    private async void FamilyTreeGenerationComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        await RefreshTreePreviewAsync();
    }

    private async void TreeEditPersonButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_treeSelectedPersonId is null || _personRepository is null)
        {
            return;
        }

        _currentPerson = await _personRepository.GetByIdAsync(_treeSelectedPersonId.Value);
        _isCurrentPersonPersisted = _currentPerson is not null;
        if (_currentPerson is null)
        {
            FamilyTreeStatusText.Text = "Person wurde nicht gefunden.";
            return;
        }

        FillFormFromPerson(_currentPerson);
        ShowModule(AppModule.People);
        PersonFormStatusText.Text = "Person aus dem Stammbaum geladen.";
        await RefreshRelationshipsAsync();
        await RefreshEventsAsync();
        await RefreshMediaFilesAsync();
    }

    private void TreeAddRelativeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_treeSelectedPersonId is null)
        {
            return;
        }

        OpenRelativeOverlay(_treeSelectedPersonId.Value);
    }

    private void AddExistingPersonSearchTextBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshExistingPersonOptions();
    }

    private async void ZoomInTreeButton_Click(object? sender, RoutedEventArgs e)
    {
        await ZoomFamilyTreeAtAsync(
            new Point(FamilyTreeScrollViewer.Bounds.Width / 2, FamilyTreeScrollViewer.Bounds.Height / 2),
            0.15);
    }

    private async void ZoomOutTreeButton_Click(object? sender, RoutedEventArgs e)
    {
        await ZoomFamilyTreeAtAsync(
            new Point(FamilyTreeScrollViewer.Bounds.Width / 2, FamilyTreeScrollViewer.Bounds.Height / 2),
            -0.15);
    }

    private void CenterTreeButton_Click(object? sender, RoutedEventArgs e)
    {
        CenterFamilyTree();
    }

    private void FitTreeButton_Click(object? sender, RoutedEventArgs e)
    {
        FitFamilyTreeToViewport();
    }

    private void ResetTreeViewButton_Click(object? sender, RoutedEventArgs e)
    {
        ResetFamilyTreeView();
    }

    private void PanTreeLeftButton_Click(object? sender, RoutedEventArgs e)
    {
        PanFamilyTree(-220, 0);
    }

    private void PanTreeRightButton_Click(object? sender, RoutedEventArgs e)
    {
        PanFamilyTree(220, 0);
    }

    private void PanTreeUpButton_Click(object? sender, RoutedEventArgs e)
    {
        PanFamilyTree(0, -180);
    }

    private void PanTreeDownButton_Click(object? sender, RoutedEventArgs e)
    {
        PanFamilyTree(0, 180);
    }

    private async void FamilyTreeCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        await ZoomFamilyTreeAtAsync(e.GetPosition(FamilyTreeScrollViewer), e.Delta.Y > 0 ? 0.1 : -0.1);
        e.Handled = true;
    }

    private void FamilyTreeCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(FamilyTreeCanvas);
        if (!point.Properties.IsLeftButtonPressed
            && !point.Properties.IsMiddleButtonPressed
            && !point.Properties.IsRightButtonPressed)
        {
            return;
        }

        _isFamilyTreePanning = true;
        _familyTreePanStartPoint = e.GetPosition(FamilyTreeScrollViewer);
        _familyTreePanStartOffset = FamilyTreeScrollViewer.Offset;
        e.Pointer.Capture(FamilyTreeCanvas);
        e.Handled = true;
    }

    private void FamilyTreeCanvas_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isFamilyTreePanning)
        {
            return;
        }

        var currentPoint = e.GetPosition(FamilyTreeScrollViewer);
        var delta = _familyTreePanStartPoint - currentPoint;
        var nextState = _familyTreeViewportService.PanBy(
            GetFamilyTreeViewportState() with
            {
                OffsetX = _familyTreePanStartOffset.X,
                OffsetY = _familyTreePanStartOffset.Y
            },
            delta.X,
            delta.Y,
            GetFamilyTreeContentWidth(),
            GetFamilyTreeContentHeight());
        FamilyTreeScrollViewer.Offset = new Vector(nextState.OffsetX, nextState.OffsetY);
        e.Handled = true;
    }

    private void FamilyTreeCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isFamilyTreePanning = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private async void SaveRelativeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_personRepository is null || _relationshipRepository is null || _addRelativeSourcePersonId is null)
        {
            AddRelativeStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
            return;
        }

        var kind = (AddRelativeTypeComboBox.SelectedItem as RelativeTypeOption)?.Value ?? RelativeAddKind.Son;
        var sourcePersonId = _addRelativeSourcePersonId.Value;
        var relationshipKind = kind;
        if (kind == RelativeAddKind.ExistingPerson)
        {
            if (AddExistingPersonComboBox.SelectedItem is not PersonListItem existingPerson)
            {
                AddRelativeStatusText.Text = "Wähle eine vorhandene Person aus.";
                return;
            }

            var existingKind = (AddExistingRelationshipTypeComboBox.SelectedItem as RelativeTypeOption)?.Value ?? RelativeAddKind.Father;
            var existingRelationship = CreateRelationshipForRelative(existingPerson.Id, sourcePersonId, existingKind);
            try
            {
                await _relationshipRepository.SaveAsync(existingRelationship);
                AddRelativeOverlay.IsVisible = false;
                await RefreshStatisticsAsync();
                await FocusTreePersonAfterRelativeSaveAsync(existingPerson.Id, "Vorhandene Person wurde verknüpft.");
                await RefreshRelationshipsAsync();
            }
            catch (Exception exception)
            {
                AddRelativeStatusText.Text = $"Beziehung konnte nicht gespeichert werden: {exception.Message}";
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(AddRelativeNameTextBox.Text))
        {
            AddRelativeStatusText.Text = "Bitte gib mindestens einen Hauptnamen ein.";
            return;
        }

        var newPerson = new Person
        {
            MainName = AddRelativeNameTextBox.Text.Trim(),
            PrimaryRole = AddRelativeRoleTextBox.Text?.Trim() ?? string.Empty,
            Gender = GetDefaultGenderForRelative(relationshipKind, (AddRelativeGenderComboBox.SelectedItem as EnumDisplay<Gender>)?.Value ?? Gender.Unknown),
            Status = (AddRelativeStatusComboBox.SelectedItem as EnumDisplay<PersonStatus>)?.Value ?? PersonStatus.Active,
            BirthDateInfo = CreateDateInfo(AddRelativeBirthDateTextBox.Text),
            DeathDateInfo = CreateDateInfo(AddRelativeDeathDateTextBox.Text)
        };
        var newRelationship = CreateRelationshipForRelative(newPerson.Id, sourcePersonId, relationshipKind);

        try
        {
            await _personRepository.SaveAsync(newPerson);
            await _relationshipRepository.SaveAsync(newRelationship);
            AddRelativeOverlay.IsVisible = false;
            await RefreshPeopleAsync();
            await RefreshStatisticsAsync();
            await FocusTreePersonAfterRelativeSaveAsync(newPerson.Id, "Neue Person und Beziehung wurden gespeichert.");
            await RefreshRelationshipsAsync();
        }
        catch (Exception exception)
        {
            AddRelativeStatusText.Text = $"Verwandte Person konnte nicht gespeichert werden: {exception.Message}";
        }
    }

    private async Task FocusTreePersonAfterRelativeSaveAsync(Guid personId, string statusText)
    {
        if (_personRepository is null)
        {
            AddRelativeStatusText.Text = statusText;
            return;
        }

        var person = await _personRepository.GetByIdAsync(personId);
        if (person is null)
        {
            AddRelativeStatusText.Text = statusText;
            return;
        }

        _currentPerson = person;
        _isCurrentPersonPersisted = true;
        _treeSelectedPersonId = person.Id;
        FillFormFromPerson(person);
        await RefreshTreePreviewAsync();
        SelectTreePerson(person.Id);
        CenterFamilyTree();
        AddRelativeStatusText.Text = statusText;
        FamilyTreeStatusText.Text = statusText;
    }

    private void CancelRelativeButton_Click(object? sender, RoutedEventArgs e)
    {
        AddRelativeOverlay.IsVisible = false;
    }

    private void AddRelativeTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateRelativeOverlayMode();
    }

    private async void TreeSavePersonButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_personRepository is null || _treeSelectedPersonId is null)
        {
            FamilyTreeStatusText.Text = "Wähle zuerst eine Person im Stammbaum aus.";
            return;
        }

        var person = await _personRepository.GetByIdAsync(_treeSelectedPersonId.Value);
        if (person is null)
        {
            FamilyTreeStatusText.Text = "Person wurde nicht gefunden.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TreeMainNameTextBox.Text))
        {
            FamilyTreeStatusText.Text = "Bitte gib mindestens einen Hauptnamen ein.";
            return;
        }

        FillTreePersonFromForm(person);

        await _personRepository.SaveAsync(person);
        if (_currentPerson?.Id == person.Id)
        {
            _currentPerson = person;
            FillFormFromPerson(person);
        }

        await RefreshPeopleAsync();
        await RefreshTreePreviewAsync();
        SelectTreePerson(person.Id);
        FamilyTreeStatusText.Text = "Person wurde im Stammbaum gespeichert.";
    }

    private async void TreeCancelEditButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_treeSelectedPersonId is null || _personRepository is null)
        {
            ClearTreePersonForm();
            return;
        }

        var person = await _personRepository.GetByIdAsync(_treeSelectedPersonId.Value);
        if (person is null)
        {
            ClearTreePersonForm();
        }
        else
        {
            FillTreePersonForm(person);
        }
        FamilyTreeStatusText.Text = person is null
            ? "Person wurde nicht gefunden."
            : "Änderungen wurden verworfen.";
    }

    private void CalculateDeathFromAgeButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _lifeDateCalculationService.CalculateDeathFromBirthAndAge(
            CreateDateInfo(TreeBirthDateTextBox.Text),
            TryReadInt(TreeAgeTextBox.Text, out var age) ? age : null);

        if (result is null)
        {
            FamilyTreeStatusText.Text = "Gib ein Alter und ein Geburtsjahr ein, um das Sterbejahr zu berechnen.";
            return;
        }

        TreeDeathDateTextBox.Text = FormatDateInfo(result.ToDateInfo());
        FamilyTreeStatusText.Text = $"Sterbejahr aus Alter berechnet: {TreeDeathDateTextBox.Text}.";
    }

    private void CalculateBirthFromAgeButton_Click(object? sender, RoutedEventArgs e)
    {
        var result = _lifeDateCalculationService.CalculateBirthFromDeathAndAge(
            CreateDateInfo(TreeDeathDateTextBox.Text),
            TryReadInt(TreeAgeTextBox.Text, out var age) ? age : null);

        if (result is null)
        {
            FamilyTreeStatusText.Text = "Gib ein Alter und ein Sterbejahr ein, um das Geburtsjahr zu berechnen.";
            return;
        }

        TreeBirthDateTextBox.Text = FormatDateInfo(result.ToDateInfo());
        FamilyTreeStatusText.Text = $"Geburtsjahr aus Alter berechnet: {TreeBirthDateTextBox.Text}.";
    }

    private void CreateEventButton_Click(object? sender, RoutedEventArgs e)
    {
        ShowModule(AppModule.Events);
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
            RefreshTimeline();
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
        _treeSelectedPersonId = null;
        _addRelativeSourcePersonId = null;
        ClearTreePersonForm();
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
        CloseProjectButton.IsEnabled = true;
        ImportMediaButton.IsEnabled = true;
        SaveMediaButton.IsEnabled = false;
        PersonFormStatusText.Text = "Bereit für die erste Person.";
        EventFormStatusText.Text = "Bereit für Ereignisse.";
        BibleReferenceFormStatusText.Text = "Bereit für Bibelstellen.";
        MediaFormStatusText.Text = "Bereit für Medien.";

        await RefreshPeopleAsync();
        await RefreshRelationshipsAsync();
        await RefreshEventsAsync();
        RefreshTimeline();
        await RefreshBibleReferencesAsync();
        await RefreshMediaFilesAsync();
        await _appStateStore.SaveAsync(new AppState(workspace.RootDirectory));
    }

    private async Task<string> SaveOpenEditorsBeforeCloseAsync()
    {
        var savedItems = 0;

        if (_personRepository is not null && _currentPerson is not null && !string.IsNullOrWhiteSpace(MainNameTextBox.Text))
        {
            FillPersonFromForm(_currentPerson);
            await _personRepository.SaveAsync(_currentPerson);
            _isCurrentPersonPersisted = true;
            savedItems++;
        }

        if (_personRepository is not null
            && _treeSelectedPersonId is not null
            && !string.IsNullOrWhiteSpace(TreeMainNameTextBox.Text)
            && _treeSelectedPersonId != _currentPerson?.Id)
        {
            var treePerson = await _personRepository.GetByIdAsync(_treeSelectedPersonId.Value);
            if (treePerson is not null)
            {
                FillTreePersonFromForm(treePerson);
                await _personRepository.SaveAsync(treePerson);
                savedItems++;
            }
        }

        if (_eventRepository is not null && _currentEvent is not null && !string.IsNullOrWhiteSpace(EventTitleTextBox.Text))
        {
            FillEventFromForm(_currentEvent);
            await _eventRepository.SaveAsync(_currentEvent);
            if (_currentPerson is not null && _isCurrentPersonPersisted)
            {
                await _eventRepository.LinkPersonAsync(_currentEvent.Id, _currentPerson.Id);
            }

            savedItems++;
        }

        if (_bibleReferenceRepository is not null
            && _currentBibleReference is not null
            && !string.IsNullOrWhiteSpace(BibleBookTextBox.Text)
            && TryReadInt(BibleChapterStartTextBox.Text, out var chapterStart))
        {
            FillBibleReferenceFromForm(_currentBibleReference, chapterStart);
            await _bibleReferenceRepository.SaveAsync(_currentBibleReference);
            savedItems++;
        }

        if (_mediaRepository is not null && _currentMediaFile is not null)
        {
            _currentMediaFile.Description = MediaDescriptionTextBox.Text?.Trim() ?? string.Empty;
            await _mediaRepository.SaveAsync(_currentMediaFile);
            savedItems++;
        }

        return savedItems == 0
            ? "Es waren keine offenen Bearbeitungen zu speichern."
            : $"{savedItems} offene Bearbeitung(en) wurden gespeichert.";
    }

    private async Task ClearProjectAsync(string status)
    {
        _currentWorkspace = null;
        _personRepository = null;
        _relationshipRepository = null;
        _eventRepository = null;
        _bibleReferenceRepository = null;
        _mediaRepository = null;
        _currentPerson = null;
        _isCurrentPersonPersisted = false;
        _currentRelationship = null;
        _currentEvent = null;
        _currentBibleReference = null;
        _currentMediaFile = null;
        _people = Array.Empty<Person>();
        _currentRelationships = Array.Empty<Relationship>();
        _currentEvents = Array.Empty<ScriptureEvent>();
        _mediaFiles = Array.Empty<MediaFile>();
        _treePeople = Array.Empty<Person>();
        _treeSelectedPersonId = null;
        _addRelativeSourcePersonId = null;

        SidebarProjectTitle.Text = "Studienmodus";
        SidebarProjectStatus.Text = "Kein Projekt geladen";
        ProjectStatusText.Text = status;
        CurrentProjectTitle.Text = "Kein Projekt geöffnet";
        CurrentProjectDetails.Text = "Öffne oder erstelle ein lokales Projekt, um weiterzuarbeiten.";
        PersonCountText.Text = "0";
        RelationshipCountText.Text = "0 Beziehungen vorbereitet";
        EventCountText.Text = "0 Ereignisse";
        BibleReferenceCountText.Text = "0 Bibelstellen";
        MediaFileCountText.Text = "0 Medien";
        PlaceCountText.Text = "0";
        ResearchQuestionCountText.Text = "0 offene Fragen";

        CreatePersonButton.IsEnabled = false;
        QuickAddPersonButton.IsEnabled = false;
        QuickAddEventButton.IsEnabled = false;
        SavePersonButton.IsEnabled = false;
        SaveRelationshipButton.IsEnabled = false;
        ArchiveRelationshipButton.IsEnabled = false;
        SaveEventButton.IsEnabled = false;
        SaveBibleReferenceButton.IsEnabled = false;
        ImportMediaButton.IsEnabled = false;
        SaveMediaButton.IsEnabled = false;
        CloseProjectButton.IsEnabled = false;

        ClearPersonForm();
        ClearRelationshipForm();
        ClearEventForm();
        ClearBibleReferenceForm();
        ClearMediaForm();
        ClearTreePersonForm();
        FamilyTreeCanvas.Children.Clear();
        AddRelativeOverlay.IsVisible = false;

        await RefreshPeopleAsync();
        await RefreshRelationshipsAsync();
        await RefreshEventsAsync();
        RefreshTimeline();
        await RefreshBibleReferencesAsync();
        await RefreshMediaFilesAsync();
        UpdateMediaActionButtons();
        ShowModule(AppModule.Dashboard);
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

    private void InitializeFamilyTreeForm()
    {
        FamilyTreeGenerationComboBox.ItemsSource = new[]
        {
            new TreeGenerationOption("2 Generationen", 2, false),
            new TreeGenerationOption("3 Generationen", 3, false),
            new TreeGenerationOption("4 Generationen", 4, false),
            new TreeGenerationOption("Alle", int.MaxValue, true)
        };
        FamilyTreeGenerationComboBox.SelectedIndex = 0;
        AddRelativeTypeComboBox.ItemsSource = new[]
        {
            new RelativeTypeOption(RelativeAddKind.Father, "Vater hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.Mother, "Mutter hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.Son, "Sohn hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.Daughter, "Tochter hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.Child, "Kind hinzufügen, Geschlecht unbekannt"),
            new RelativeTypeOption(RelativeAddKind.Brother, "Bruder hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.Sister, "Schwester hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.Partner, "Partner hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.OtherRelationship, "Weitere Beziehung hinzufügen"),
            new RelativeTypeOption(RelativeAddKind.ExistingPerson, "Bestehende Person verknüpfen")
        };
        AddRelativeTypeComboBox.SelectedIndex = 2;
        AddExistingRelationshipTypeComboBox.ItemsSource = new[]
        {
            new RelativeTypeOption(RelativeAddKind.Father, "als Vater verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Mother, "als Mutter verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Son, "als Sohn verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Daughter, "als Tochter verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Child, "als Kind verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Brother, "als Bruder verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Sister, "als Schwester verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.Partner, "als Partner verknüpfen"),
            new RelativeTypeOption(RelativeAddKind.OtherRelationship, "als weitere Beziehung verknüpfen")
        };
        AddExistingRelationshipTypeComboBox.SelectedIndex = 0;
        AddRelativeGenderComboBox.ItemsSource = DisplayOptions.Genders();
        AddRelativeGenderComboBox.SelectedIndex = 0;
        AddRelativeStatusComboBox.ItemsSource = DisplayOptions.PersonStatuses();
        AddRelativeStatusComboBox.SelectedIndex = 0;
        AddRelativeCertaintyComboBox.ItemsSource = DisplayOptions.CertaintyLevels();
        SelectEnumValue(AddRelativeCertaintyComboBox, CertaintyLevel.Likely);
        TreeGenderComboBox.ItemsSource = DisplayOptions.Genders();
        TreeGenderComboBox.SelectedIndex = 0;
        TreeStatusComboBox.ItemsSource = DisplayOptions.PersonStatuses();
        TreeStatusComboBox.SelectedIndex = 0;
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
            FamilyTreeStatusText.Text = "Wähle eine gespeicherte Person aus, um den Stammbaum zu zeichnen.";
            TreeSelectedPersonText.Text = "Noch keine Person ausgewählt.";
            ClearTreePersonForm();
            TreeSavePersonButton.IsEnabled = false;
            TreeCancelEditButton.IsEnabled = false;
            TreeAddRelativeButton.IsEnabled = false;
            FamilyTreeCanvas.Children.Clear();
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
        await RefreshTreePreviewAsync();
    }

    private async Task RefreshEventsAsync()
    {
        if (_eventRepository is null)
        {
            EventsListBox.ItemsSource = Array.Empty<EventListItem>();
            EventsEmptyText.Text = "Noch kein Projekt geöffnet.";
            RefreshTimeline();
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
        RefreshTimeline();
    }

    private void RefreshTimeline()
    {
        var entries = _timelineBuilder.Build(_currentEvents);
        var timelineItems = entries
            .Select(entry => new TimelineListItem(
                entry.EventId,
                $"{entry.DateText} - {DisplayText.For(entry.EventType)}: {entry.Title}",
                entry.Description))
            .ToList();

        TimelineListBox.ItemsSource = timelineItems;
        TimelineModuleListBox.ItemsSource = timelineItems;

        var statusText = entries.Count == 0
            ? "Noch keine Ereignisse für die Zeitleiste."
            : _currentPerson is null
                ? $"{entries.Count} Ereignis(se) in der globalen Zeitleiste."
                : $"{entries.Count} Ereignis(se) für die ausgewählte Person.";
        TimelineEmptyText.Text = statusText;
        TimelineModuleEmptyText.Text = statusText;
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

    private void ClearBibleReferenceForm()
    {
        BibleTranslationTextBox.Text = string.Empty;
        BibleBookTextBox.Text = string.Empty;
        BibleChapterStartTextBox.Text = string.Empty;
        BibleVerseStartTextBox.Text = string.Empty;
        BibleChapterEndTextBox.Text = string.Empty;
        BibleVerseEndTextBox.Text = string.Empty;
        BibleReferenceTextBox.Text = string.Empty;
        BibleSummaryTextBox.Text = string.Empty;
        BibleCommentTextBox.Text = string.Empty;
        BibleReferenceFormStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
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
        person.BirthDateInfo = CreateDateInfo(BirthDateTextBox.Text);
        person.DeathDateInfo = CreateDateInfo(DeathDateTextBox.Text);
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
        BirthDateTextBox.Text = FormatDateInfo(person.BirthDateInfo);
        DeathDateTextBox.Text = FormatDateInfo(person.DeathDateInfo);
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

    private async Task RefreshTreePreviewAsync()
    {
        if (_currentPerson is null || _relationshipRepository is null || _personRepository is null)
        {
            FamilyTreeCanvas.Children.Clear();
            ClearTreePersonForm();
            TreeSavePersonButton.IsEnabled = false;
            TreeCancelEditButton.IsEnabled = false;
            TreeAddRelativeButton.IsEnabled = false;
            return;
        }

        var allPeople = await _personRepository.SearchAsync(string.Empty);
        _treePeople = allPeople;
        var treeRelationships = await LoadConnectedRelationshipsAsync(_currentPerson.Id);
        var snapshot = _familyTreeBuilder.Build(_currentPerson, allPeople, treeRelationships);
        TreeParentsText.Text = $"Eltern: {FormatTreeGroup(snapshot.Parents.Select(node => node.DisplayName).ToList())}";
        TreeFocusText.Text = $"Fokusperson: {snapshot.FocusPerson.DisplayName}";
        TreePartnersText.Text = $"Partner: {FormatTreeGroup(snapshot.Partners.Select(node => node.DisplayName).ToList())}";
        TreeChildrenText.Text = $"Kinder: {FormatTreeGroup(snapshot.Children.Select(node => node.DisplayName).ToList())}";
        TreeOtherText.Text = $"Weitere oder unsichere Beziehungen: {FormatTreeGroup(snapshot.OtherRelations.Select(node => node.DisplayName).ToList())}";
        TreePreviewText.Text = snapshot.Links.Any(link => link.IsUncertain)
            ? "Unsichere oder ungerichtete Beziehungen werden in der Vorschau unter weitere Beziehungen geführt."
            : "Die gespeicherten Beziehungen wurden in die einfache Stammbaum-Vorschau übernommen.";
        FamilyTreeStatusText.Text = $"{snapshot.FocusPerson.DisplayName} ist der Fokus. Nutze + an einer Karte, um Verwandte hinzuzufügen.";
        _treeSelectedPersonId ??= _currentPerson.Id;
        var diagram = _familyTreeBuilder.BuildDiagram(_currentPerson, allPeople, treeRelationships, GetTreeLayoutOptions());
        _currentFamilyTreeDiagram = diagram;
        DrawFamilyTree(diagram);
    }

    private async Task<IReadOnlyList<Relationship>> LoadConnectedRelationshipsAsync(Guid focusPersonId)
    {
        if (_relationshipRepository is null)
        {
            return Array.Empty<Relationship>();
        }

        var relationshipsById = new Dictionary<Guid, Relationship>();
        var visitedPeople = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(focusPersonId);

        while (queue.Count > 0)
        {
            var personId = queue.Dequeue();
            if (!visitedPeople.Add(personId))
            {
                continue;
            }

            var relationships = await _relationshipRepository.GetForPersonAsync(personId);
            foreach (var relationship in relationships)
            {
                relationshipsById.TryAdd(relationship.Id, relationship);
                var otherPersonId = relationship.PersonAId == personId
                    ? relationship.PersonBId
                    : relationship.PersonAId;
                if (!visitedPeople.Contains(otherPersonId))
                {
                    queue.Enqueue(otherPersonId);
                }
            }
        }

        return relationshipsById.Values.ToList();
    }

    private FamilyTreeLayoutOptions GetTreeLayoutOptions()
    {
        return FamilyTreeGenerationComboBox.SelectedItem is TreeGenerationOption option
            ? new FamilyTreeLayoutOptions(option.GenerationLimit, option.ShowAllConnected)
            : FamilyTreeLayoutOptions.Default;
    }

    private void DrawFamilyTree(FamilyTreeDiagram diagram)
    {
        FamilyTreeCanvas.Children.Clear();
        FamilyTreeCanvas.Width = diagram.Width * _familyTreeZoom;
        FamilyTreeCanvas.Height = diagram.Height * _familyTreeZoom;
        DrawFamilyTreeConnections(diagram.Connections);

        foreach (var node in diagram.Nodes)
        {
            var card = CreateTreePersonCard(node);
            Canvas.SetLeft(card, node.X * _familyTreeZoom);
            Canvas.SetTop(card, node.Y * _familyTreeZoom);
            FamilyTreeCanvas.Children.Add(card);
        }
    }

    private Point ScalePoint(double x, double y)
    {
        return new Point(x * _familyTreeZoom, y * _familyTreeZoom);
    }

    private Point ScalePoint(Point point)
    {
        return ScalePoint(point.X, point.Y);
    }

    private void DrawFamilyTreeConnections(IReadOnlyList<FamilyTreeConnection> connections)
    {
        foreach (var connection in connections)
        {
            var isSelectedConnection = IsTreeConnectionSelected(connection);
            IBrush stroke = isSelectedConnection
                ? new SolidColorBrush(Color.Parse("#C75C3E"))
                : connection.LineStyle == FamilyTreeLineStyle.MutedDashed ? new SolidColorBrush(Color.Parse("#8B7D75"))
                : connection.IsUncertain ? Brushes.Gray : Brushes.DarkSlateGray;
            AddTreePath(
                connection.Type,
                ScalePoint(connection.Start.X, connection.Start.Y),
                ScalePoint(connection.End.X, connection.End.Y),
                stroke,
                connection.LineStyle);
        }
    }

    private bool IsTreeConnectionSelected(FamilyTreeConnection connection)
    {
        return _treeSelectedPersonId is not null
            && (_treeSelectedPersonId.Value == connection.FromPersonId || _treeSelectedPersonId.Value == connection.ToPersonId);
    }

    private void AddTreePath(FamilyTreeConnectionType type, Point start, Point end, IBrush stroke, FamilyTreeLineStyle lineStyle)
    {
        var pathData = _bezierConnectionBuilder
            .Build(new TreePoint(start.X, start.Y), new TreePoint(end.X, end.Y), type)
            .ToInvariantPathData();

        var path = new PathShape
        {
            Data = Geometry.Parse(pathData),
            Stroke = stroke,
            StrokeThickness = lineStyle == FamilyTreeLineStyle.Solid ? 2.1 : 1.5
        };
        if (lineStyle == FamilyTreeLineStyle.Dashed || lineStyle == FamilyTreeLineStyle.MutedDashed)
        {
            path.StrokeDashArray = new AvaloniaList<double> { 4, 4 };
        }
        else if (lineStyle == FamilyTreeLineStyle.Dotted)
        {
            path.StrokeDashArray = new AvaloniaList<double> { 1, 4 };
        }

        FamilyTreeCanvas.Children.Add(path);
    }

    private static Point GetTreeEdgePoint(FamilyTreeDiagramNode fromNode, FamilyTreeDiagramNode toNode)
    {
        var toCenterX = toNode.X + TreeCardWidth / 2;
        var toCenterY = toNode.Y + TreeCardHeight / 2;
        return GetTreeEdgePoint(fromNode, new Point(toCenterX, toCenterY));
    }

    private static Point GetTreeEdgePoint(FamilyTreeDiagramNode fromNode, Point targetPoint)
    {
        var fromCenterX = fromNode.X + TreeCardWidth / 2;
        var fromCenterY = fromNode.Y + TreeCardHeight / 2;
        var toCenterX = targetPoint.X;
        var toCenterY = targetPoint.Y;
        var deltaX = toCenterX - fromCenterX;
        var deltaY = toCenterY - fromCenterY;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            return new Point(
                deltaX >= 0 ? fromNode.X + TreeCardWidth : fromNode.X,
                fromCenterY);
        }

        return new Point(
            fromCenterX,
            deltaY >= 0 ? fromNode.Y + TreeCardHeight : fromNode.Y);
    }

    private Control CreateTreePersonCard(FamilyTreeDiagramNode node)
    {
        var person = _treePeople.FirstOrDefault(person => person.Id == node.PersonId);
        var isSelected = _treeSelectedPersonId == node.PersonId;
        var background = node.IsPlaceholder
            ? new SolidColorBrush(Color.Parse("#F4F4F1"))
            : node.IsUncertain
                ? new SolidColorBrush(Color.Parse("#F1E2B9"))
                : new SolidColorBrush(Color.Parse("#FDE8A8"));
        var hoverBackground = node.IsPlaceholder
            ? new SolidColorBrush(Color.Parse("#FAFAF7"))
            : new SolidColorBrush(Color.Parse("#FFF0BF"));
        var birthText = node.IsPlaceholder
            ? "noch offen"
            : person is null ? DisplayTreeNodeKind(node.Kind) : $"* {EmptyAsUnknown(FormatDateInfo(person.BirthDateInfo))}";
        var deathText = node.IsPlaceholder
            ? "klicken zum Hinzufügen"
            : person is null ? string.Empty : $"† {EmptyAsUnknown(FormatDateInfo(person.DeathDateInfo))}";
        var model = new FamilyTreeCardModel(
            _familyTreeZoom,
            node.DisplayName,
            birthText,
            deathText,
            GetAvatarGlyph(person),
            background,
            hoverBackground,
            GetTreeCardBorderBrush(person, node),
            isSelected,
            node.IsFocus,
            node.IsPlaceholder);

        return FamilyTreeCardFactory.Create(
            model,
            onSelect: () =>
            {
                if (node.IsPlaceholder)
                {
                    OpenTreeNodeAddOverlay(node);
                    return;
                }

                SelectTreePerson(node.PersonId);
            },
            onEdit: () =>
            {
                if (!node.IsPlaceholder)
                {
                    SelectTreePerson(node.PersonId);
                    TreeMainNameTextBox.Focus();
                }
            },
            onAddRelative: () => OpenTreeNodeAddOverlay(node));
    }

    private void OpenTreeNodeAddOverlay(FamilyTreeDiagramNode node)
    {
        if (node.IsPlaceholder && node.SourcePersonId is not null)
        {
            var kind = node.PlaceholderKind == FamilyTreePlaceholderKind.Father
                ? RelativeAddKind.Father
                : RelativeAddKind.Mother;
            OpenRelativeOverlay(node.SourcePersonId.Value, kind);
            return;
        }

        OpenRelativeOverlay(node.PersonId);
    }

    private static IBrush GetTreeCardBorderBrush(Person? person, FamilyTreeDiagramNode node)
    {
        if (node.IsPlaceholder)
        {
            return Brushes.Gray;
        }

        if (node.IsUncertain)
        {
            return Brushes.Gray;
        }

        return person?.Gender switch
        {
            Gender.Male => new SolidColorBrush(Color.Parse("#00A8C8")),
            Gender.Female => new SolidColorBrush(Color.Parse("#FF6B6B")),
            _ => new SolidColorBrush(Color.Parse("#D7A65A"))
        };
    }

    private static string GetAvatarGlyph(Person? person)
    {
        return person?.Gender switch
        {
            Gender.Male => "♂",
            Gender.Female => "♀",
            _ => "○"
        };
    }

    private void SelectTreePerson(Guid personId)
    {
        _treeSelectedPersonId = personId;
        var person = _people.FirstOrDefault(person => person.Id == personId);
        person ??= _treePeople.FirstOrDefault(person => person.Id == personId);
        TreeSelectedPersonText.Text = person is null
            ? "Person ist nicht in der aktuellen Liste sichtbar."
            : $"{person.MainName}\n{FormatLifeDateLine(person)}";
        if (person is not null)
        {
            FillTreePersonForm(person);
        }

        TreeSavePersonButton.IsEnabled = person is not null;
        TreeCancelEditButton.IsEnabled = person is not null;
        TreeAddRelativeButton.IsEnabled = true;

        if (_currentFamilyTreeDiagram is not null)
        {
            DrawFamilyTree(_currentFamilyTreeDiagram);
        }
    }

    private void FillTreePersonForm(Person person)
    {
        TreeMainNameTextBox.Text = person.MainName;
        TreeAlternativeNamesTextBox.Text = person.AlternativeNames;
        TreeRoleTextBox.Text = person.PrimaryRole;
        TreeBirthDateTextBox.Text = FormatDateInfo(person.BirthDateInfo);
        TreeDeathDateTextBox.Text = FormatDateInfo(person.DeathDateInfo);
        TreeAgeTextBox.Text = person.AgeAtDeath?.ToString()
            ?? (TryGetLifeAge(person, out var age) ? age.ToString() : string.Empty);
        TreeBirthPlaceTextBox.Text = person.BirthPlaceText;
        TreeDeathPlaceTextBox.Text = person.DeathPlaceText;
        TreeShortDescriptionTextBox.Text = person.ShortDescription;
        TreeEventsText.Text = "Ereignisse der Person werden im Ereignisse-Modul gepflegt.";
        TreeBibleReferencesText.Text = "Bibelstellen werden im Bibelstellen-Modul gepflegt.";
        TreeMediaText.Text = person.PortraitMediaFileId is null
            ? "Kein Portrait oder Medium verknüpft."
            : "Portrait/Medium ist verknüpft.";
        TreeResearchNotesText.Text = string.IsNullOrWhiteSpace(person.LongDescription)
            ? "Noch keine ausführliche Forschungsnotiz hinterlegt."
            : person.LongDescription;
        SelectEnumValue(TreeGenderComboBox, person.Gender);
        SelectEnumValue(TreeStatusComboBox, person.Status);
    }

    private void FillTreePersonFromForm(Person person)
    {
        person.MainName = TreeMainNameTextBox.Text?.Trim() ?? string.Empty;
        person.AlternativeNames = TreeAlternativeNamesTextBox.Text?.Trim() ?? string.Empty;
        person.PrimaryRole = TreeRoleTextBox.Text?.Trim() ?? string.Empty;
        person.ShortDescription = TreeShortDescriptionTextBox.Text?.Trim() ?? string.Empty;
        person.Gender = (TreeGenderComboBox.SelectedItem as EnumDisplay<Gender>)?.Value ?? Gender.Unknown;
        person.Status = (TreeStatusComboBox.SelectedItem as EnumDisplay<PersonStatus>)?.Value ?? PersonStatus.Active;
        person.BirthDateInfo = CreateDateInfo(TreeBirthDateTextBox.Text);
        person.DeathDateInfo = CreateDateInfo(TreeDeathDateTextBox.Text);
        person.AgeAtDeath = TryReadInt(TreeAgeTextBox.Text, out var ageAtDeath) ? ageAtDeath : null;
        person.BirthPlaceText = TreeBirthPlaceTextBox.Text?.Trim() ?? string.Empty;
        person.DeathPlaceText = TreeDeathPlaceTextBox.Text?.Trim() ?? string.Empty;
    }

    private void ClearTreePersonForm()
    {
        TreeMainNameTextBox.Text = string.Empty;
        TreeAlternativeNamesTextBox.Text = string.Empty;
        TreeRoleTextBox.Text = string.Empty;
        TreeBirthDateTextBox.Text = string.Empty;
        TreeDeathDateTextBox.Text = string.Empty;
        TreeAgeTextBox.Text = string.Empty;
        TreeBirthPlaceTextBox.Text = string.Empty;
        TreeDeathPlaceTextBox.Text = string.Empty;
        TreeShortDescriptionTextBox.Text = string.Empty;
        TreeEventsText.Text = "Keine Ereignisse geladen.";
        TreeBibleReferencesText.Text = "Keine Bibelstellen verknüpft.";
        TreeMediaText.Text = "Keine Medien ausgewählt.";
        TreeResearchNotesText.Text = "Keine Forschungsnotizen geladen.";
        TreeGenderComboBox.SelectedIndex = 0;
        TreeStatusComboBox.SelectedIndex = 0;
    }

    private void OpenRelativeOverlay(Guid sourcePersonId, RelativeAddKind? presetKind = null)
    {
        _addRelativeSourcePersonId = sourcePersonId;
        var sourceName = _people.FirstOrDefault(person => person.Id == sourcePersonId)?.MainName ?? "dieser Person";
        sourceName = _treePeople.FirstOrDefault(person => person.Id == sourcePersonId)?.MainName ?? sourceName;
        AddRelativeTitleText.Text = $"Verwandte zu {sourceName}";
        if (presetKind is not null)
        {
            SelectComboItem(AddRelativeTypeComboBox, (RelativeTypeOption option) => option.Value == presetKind.Value);
            SelectComboItem(AddExistingRelationshipTypeComboBox, (RelativeTypeOption option) => option.Value == presetKind.Value);
        }

        AddRelativeNameTextBox.Text = string.Empty;
        AddRelativeRoleTextBox.Text = string.Empty;
        AddRelativeBirthDateTextBox.Text = string.Empty;
        AddRelativeDeathDateTextBox.Text = string.Empty;
        AddRelativeSourceNoteTextBox.Text = string.Empty;
        AddRelativeCommentTextBox.Text = string.Empty;
        AddExistingPersonSearchTextBox.Text = string.Empty;
        RefreshExistingPersonOptions();
        UpdateRelativeOverlayMode();
        AddRelativeStatusText.Text = "Neue Person und Beziehung werden gemeinsam gespeichert.";
        AddRelativeOverlay.IsVisible = true;
        AddRelativeNameTextBox.Focus();
    }

    private void UpdateRelativeOverlayMode()
    {
        var kind = (AddRelativeTypeComboBox.SelectedItem as RelativeTypeOption)?.Value ?? RelativeAddKind.Son;
        var usesExistingPerson = kind == RelativeAddKind.ExistingPerson;

        AddExistingRelationshipTypeComboBox.IsVisible = usesExistingPerson;
        AddExistingPersonSearchTextBox.IsVisible = usesExistingPerson;
        AddExistingPersonComboBox.IsVisible = usesExistingPerson;

        AddRelativeNameTextBox.IsVisible = !usesExistingPerson;
        AddRelativeRoleTextBox.IsVisible = !usesExistingPerson;
        AddRelativeBirthDateTextBox.IsVisible = !usesExistingPerson;
        AddRelativeDeathDateTextBox.IsVisible = !usesExistingPerson;
        AddRelativeGenderComboBox.IsVisible = !usesExistingPerson;
        AddRelativeStatusComboBox.IsVisible = !usesExistingPerson;
        if (!usesExistingPerson)
        {
            SelectEnumValue(AddRelativeGenderComboBox, GetDefaultGenderForRelative(kind, Gender.Unknown));
        }

        AddRelativeStatusText.Text = usesExistingPerson
            ? "Vorhandene Person suchen, Beziehungstyp festlegen und verknüpfen."
            : "Neue Person und Beziehung werden gemeinsam gespeichert.";
    }

    private void RefreshExistingPersonOptions()
    {
        var searchText = AddExistingPersonSearchTextBox.Text?.Trim() ?? string.Empty;
        AddExistingPersonComboBox.ItemsSource = _people
            .Where(person => _addRelativeSourcePersonId is null || person.Id != _addRelativeSourcePersonId.Value)
            .Where(person => string.IsNullOrWhiteSpace(searchText)
                || person.MainName.Contains(searchText, StringComparison.CurrentCultureIgnoreCase)
                || person.PrimaryRole.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
            .Take(40)
            .Select(person => new PersonListItem(person.Id, person.MainName, person.PrimaryRole))
            .ToList();
    }

    private Relationship CreateRelationshipForRelative(Guid relativePersonId, Guid sourcePersonId, RelativeAddKind kind)
    {
        var certainty = (AddRelativeCertaintyComboBox.SelectedItem as EnumDisplay<CertaintyLevel>)?.Value ?? CertaintyLevel.Likely;
        var relationship = kind switch
        {
            RelativeAddKind.Father or RelativeAddKind.Mother => new Relationship
            {
                PersonAId = relativePersonId,
                PersonBId = sourcePersonId,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB,
                CertaintyLevel = certainty
            },
            RelativeAddKind.Partner => new Relationship
            {
                PersonAId = sourcePersonId,
                PersonBId = relativePersonId,
                RelationshipType = RelationshipType.Spouse,
                Direction = RelationshipDirection.Undirected,
                CertaintyLevel = certainty
            },
            RelativeAddKind.Brother or RelativeAddKind.Sister => new Relationship
            {
                PersonAId = sourcePersonId,
                PersonBId = relativePersonId,
                RelationshipType = RelationshipType.Sibling,
                Direction = RelationshipDirection.Undirected,
                CertaintyLevel = certainty
            },
            RelativeAddKind.OtherRelationship => new Relationship
            {
                PersonAId = sourcePersonId,
                PersonBId = relativePersonId,
                RelationshipType = RelationshipType.UnknownRelated,
                Direction = RelationshipDirection.Undirected,
                CertaintyLevel = certainty
            },
            _ => new Relationship
            {
                PersonAId = sourcePersonId,
                PersonBId = relativePersonId,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB,
                CertaintyLevel = certainty
            }
        };

        relationship.SourceNote = AddRelativeSourceNoteTextBox.Text?.Trim() ?? string.Empty;
        relationship.Comment = AddRelativeCommentTextBox.Text?.Trim() ?? string.Empty;
        return relationship;
    }

    private static Gender GetDefaultGenderForRelative(RelativeAddKind kind, Gender selectedGender)
    {
        return kind switch
        {
            RelativeAddKind.Father or RelativeAddKind.Son or RelativeAddKind.Brother => Gender.Male,
            RelativeAddKind.Mother or RelativeAddKind.Daughter or RelativeAddKind.Sister => Gender.Female,
            _ => selectedGender
        };
    }

    private void SetFamilyTreeZoom(double zoom)
    {
        _familyTreeZoom = FamilyTreeViewportService.ClampZoom(zoom);
        if (_currentPerson is not null)
        {
            _ = RefreshTreePreviewAsync();
        }
    }

    private async Task ZoomFamilyTreeAtAsync(Point pointerScreenPoint, double zoomDelta)
    {
        var currentState = GetFamilyTreeViewportState();
        var nextState = _familyTreeViewportService.ZoomAtPointer(
            new TreePoint(pointerScreenPoint.X, pointerScreenPoint.Y),
            zoomDelta,
            currentState,
            GetFamilyTreeContentWidth(),
            GetFamilyTreeContentHeight());
        _familyTreeZoom = nextState.ZoomFactor;
        if (_currentPerson is not null)
        {
            await RefreshTreePreviewAsync();
        }

        FamilyTreeScrollViewer.Offset = new Vector(nextState.OffsetX, nextState.OffsetY);
    }

    private void CenterFamilyTree()
    {
        if (_currentFamilyTreeDiagram is not null && _treeSelectedPersonId is not null)
        {
            var selectedNode = _currentFamilyTreeDiagram.Nodes.FirstOrDefault(node => node.PersonId == _treeSelectedPersonId.Value);
            if (selectedNode is not null)
            {
                var centeredState = _familyTreeViewportService.CenterOnNode(
                    selectedNode,
                    GetFamilyTreeViewportState(),
                    GetFamilyTreeContentWidth(),
                    GetFamilyTreeContentHeight());
                FamilyTreeScrollViewer.Offset = new Vector(centeredState.OffsetX, centeredState.OffsetY);
                return;
            }
        }

        FamilyTreeScrollViewer.Offset = new Vector(
            Math.Max(0, (FamilyTreeCanvas.Width - FamilyTreeScrollViewer.Bounds.Width) / 2),
            Math.Max(0, (FamilyTreeCanvas.Height - FamilyTreeScrollViewer.Bounds.Height) / 2));
    }

    private void PanFamilyTree(double deltaX, double deltaY)
    {
        var nextState = _familyTreeViewportService.PanBy(
            GetFamilyTreeViewportState(),
            deltaX,
            deltaY,
            GetFamilyTreeContentWidth(),
            GetFamilyTreeContentHeight());
        FamilyTreeScrollViewer.Offset = new Vector(nextState.OffsetX, nextState.OffsetY);
    }

    private void FitFamilyTreeToViewport()
    {
        var nextState = _familyTreeViewportService.FitToTree(
            GetFamilyTreeViewportState(),
            GetFamilyTreeContentWidth(),
            GetFamilyTreeContentHeight());
        _familyTreeZoom = nextState.ZoomFactor;
        if (_currentPerson is not null)
        {
            _ = RefreshTreePreviewAsync();
        }

        FamilyTreeScrollViewer.Offset = new Vector(nextState.OffsetX, nextState.OffsetY);
    }

    private void ResetFamilyTreeView()
    {
        var nextState = FamilyTreeViewportService.ResetView(GetFamilyTreeViewportState());
        _familyTreeZoom = nextState.ZoomFactor;
        if (_currentPerson is not null)
        {
            _ = RefreshTreePreviewAsync();
        }

        FamilyTreeScrollViewer.Offset = new Vector(nextState.OffsetX, nextState.OffsetY);
    }

    private FamilyTreeViewportState GetFamilyTreeViewportState()
    {
        return new FamilyTreeViewportState(
            _familyTreeZoom,
            FamilyTreeScrollViewer.Offset.X,
            FamilyTreeScrollViewer.Offset.Y,
            FamilyTreeScrollViewer.Bounds.Width,
            FamilyTreeScrollViewer.Bounds.Height);
    }

    private double GetFamilyTreeContentWidth()
    {
        return _currentFamilyTreeDiagram?.Width ?? Math.Max(1, FamilyTreeCanvas.Width / Math.Max(_familyTreeZoom, 0.01));
    }

    private double GetFamilyTreeContentHeight()
    {
        return _currentFamilyTreeDiagram?.Height ?? Math.Max(1, FamilyTreeCanvas.Height / Math.Max(_familyTreeZoom, 0.01));
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

    private static DateInfo? CreateDateInfo(string? text)
    {
        var trimmedText = text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedText))
        {
            return null;
        }

        return new DateInfo
        {
            DateType = TryReadYear(trimmedText, out var year) && trimmedText == year.ToString()
                ? DateType.ExactYear
                : DateType.TextOnly,
            ApproximationText = trimmedText,
            Year = TryReadYear(trimmedText, out year) ? year : null,
            IsBeforeChrist = ContainsBeforeChristMarker(trimmedText),
            CertaintyLevel = CertaintyLevel.Unknown
        };
    }

    private static string FormatDateInfo(DateInfo? dateInfo)
    {
        if (dateInfo is null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(dateInfo.ApproximationText))
        {
            return dateInfo.ApproximationText;
        }

        return dateInfo.Year is null
            ? string.Empty
            : dateInfo.IsBeforeChrist
                ? $"{dateInfo.Year} v. Chr."
                : dateInfo.Year.Value.ToString();
    }

    private static string FormatLifeDateLine(Person person)
    {
        var birth = FormatDateInfo(person.BirthDateInfo);
        var death = FormatDateInfo(person.DeathDateInfo);
        if (string.IsNullOrWhiteSpace(birth) && string.IsNullOrWhiteSpace(death))
        {
            return string.IsNullOrWhiteSpace(person.PrimaryRole) ? "Keine Lebensdaten hinterlegt" : person.PrimaryRole;
        }

        var age = TryGetLifeAge(person, out var value) ? $" (~{value} Jahre)" : string.Empty;
        return $"* {EmptyAsUnknown(birth)}   † {EmptyAsUnknown(death)}{age}";
    }

    private static string FormatTreeCardDetail(Person person, FamilyTreeNodeKind kind)
    {
        var lifeDate = FormatLifeDateLine(person);
        if (!lifeDate.StartsWith('*') && !string.IsNullOrWhiteSpace(person.PrimaryRole))
        {
            return person.PrimaryRole;
        }

        return lifeDate.StartsWith('*')
            ? lifeDate
            : DisplayTreeNodeKind(kind);
    }

    private static bool TryGetLifeAge(Person person, out int age)
    {
        age = 0;
        if (person.AgeAtDeath is not null)
        {
            age = person.AgeAtDeath.Value;
            return age >= 0;
        }

        var calculator = new LifeDateCalculationService();
        var result = calculator.CalculateAgeAtDeath(person.BirthDateInfo, person.DeathDateInfo);
        age = result?.Age ?? 0;
        return result?.Age is not null;
    }

    private static string EmptyAsUnknown(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unbekannt" : value;
    }

    private static bool TryReadYear(string? value, out int year)
    {
        year = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = System.Text.RegularExpressions.Regex.Match(value, @"-?\d{1,4}");
        return match.Success && int.TryParse(match.Value, out year);
    }

    private static bool ContainsBeforeChristMarker(string value)
    {
        return value.Contains("v. Chr", StringComparison.CurrentCultureIgnoreCase)
            || value.Contains("v.Chr", StringComparison.CurrentCultureIgnoreCase)
            || value.Contains("bc", StringComparison.OrdinalIgnoreCase)
            || value.Contains("bce", StringComparison.OrdinalIgnoreCase);
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

    private void ClearMediaForm()
    {
        MediaDescriptionTextBox.Text = string.Empty;
        SelectedMediaText.Text = "Kein Medium ausgewählt.";
        MediaFormStatusText.Text = "Öffne oder erstelle zuerst ein Projekt.";
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

    private static string DisplayTreeNodeKind(FamilyTreeNodeKind kind)
    {
        return kind switch
        {
            FamilyTreeNodeKind.Focus => "Fokus",
            FamilyTreeNodeKind.Parent => "Elternteil",
            FamilyTreeNodeKind.Partner => "Partner",
            FamilyTreeNodeKind.Sibling => "Geschwister",
            FamilyTreeNodeKind.Child => "Kind",
            _ => "Verwandt"
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
        BirthDateTextBox.Text = string.Empty;
        DeathDateTextBox.Text = string.Empty;
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

    private sealed record TimelineListItem(Guid Id, string Title, string Description)
    {
        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(Description)
                ? Title
                : $"{Title} - {Description}";
        }
    }

    private sealed record TreeGenerationOption(string Label, int GenerationLimit, bool ShowAllConnected)
    {
        public override string ToString()
        {
            return Label;
        }
    }

    private enum RelativeAddKind
    {
        Father,
        Mother,
        Son,
        Daughter,
        Child,
        Brother,
        Sister,
        Partner,
        OtherRelationship,
        ExistingPerson
    }

    private sealed record RelativeTypeOption(RelativeAddKind Value, string Label)
    {
        public override string ToString()
        {
            return Label;
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

        public Task ClearAsync()
        {
            if (File.Exists(_stateFilePath))
            {
                File.Delete(_stateFilePath);
            }

            return Task.CompletedTask;
        }
    }
}
