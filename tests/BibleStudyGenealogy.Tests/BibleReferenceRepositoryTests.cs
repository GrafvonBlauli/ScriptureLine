using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Tests;

public sealed class BibleReferenceRepositoryTests
{
    [Fact]
    public async Task SaveAsync_CreatesReferenceAndIncreasesCount()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new BibleReferenceRepository(project.Workspace.DatabasePath);
        var reference = new BibleReference
        {
            Translation = "LUT",
            Book = "Genesis",
            ChapterStart = 12,
            VerseStart = 1,
            UserSummary = "Abrams Berufung"
        };

        await repository.SaveAsync(reference);

        var count = await repository.CountAsync();
        var loadedReference = await repository.GetByIdAsync(reference.Id);
        var searchResults = await repository.SearchAsync("Abrams");

        Assert.Equal(1, count);
        Assert.NotNull(loadedReference);
        Assert.Equal("Genesis", loadedReference.Book);
        Assert.Single(searchResults);
    }

    [Fact]
    public async Task SaveAsync_UpdatesReferenceWithoutAutomaticBibleText()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new BibleReferenceRepository(project.Workspace.DatabasePath);
        var reference = new BibleReference
        {
            Book = "Johannes",
            ChapterStart = 1,
            UserComment = "Erste Notiz"
        };

        await repository.SaveAsync(reference);
        reference.UserComment = "Aktualisierte Notiz";
        await repository.SaveAsync(reference);

        var loadedReference = await repository.GetByIdAsync(reference.Id);
        var count = await repository.CountAsync();

        Assert.NotNull(loadedReference);
        Assert.Equal("Aktualisierte Notiz", loadedReference.UserComment);
        Assert.Equal(string.Empty, loadedReference.ReferenceText);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveAsync_RejectsInvalidReferenceRange()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new BibleReferenceRepository(project.Workspace.DatabasePath);

        var chapterException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveAsync(new BibleReference
            {
                Book = "Genesis",
                ChapterStart = 12,
                ChapterEnd = 11
            }));
        var verseException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.SaveAsync(new BibleReference
            {
                Book = "Genesis",
                ChapterStart = 12,
                VerseStart = 10,
                VerseEnd = 9
            }));

        Assert.Equal("Das Endkapitel darf nicht vor dem Startkapitel liegen.", chapterException.Message);
        Assert.Equal("Der Endvers darf nicht vor dem Startvers liegen.", verseException.Message);
    }

    [Fact]
    public async Task LinkEventAsync_LoadsReferencesForEvent()
    {
        await using var project = await TestProject.CreateAsync();
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var bibleReferenceRepository = new BibleReferenceRepository(project.Workspace.DatabasePath);
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Jesu Taufe",
            EventType = EventType.Other
        };
        var reference = new BibleReference
        {
            Book = "Matthäus",
            ChapterStart = 3,
            VerseStart = 13
        };

        await eventRepository.SaveAsync(scriptureEvent);
        await bibleReferenceRepository.SaveAsync(reference);
        await bibleReferenceRepository.LinkEventAsync(scriptureEvent.Id, reference.Id);

        var eventReferences = await bibleReferenceRepository.GetForEventAsync(scriptureEvent.Id);

        Assert.Single(eventReferences);
        Assert.Equal(reference.Id, eventReferences[0].Id);
    }

    [Fact]
    public async Task ProjectStatistics_UsesBibleReferenceCount()
    {
        await using var project = await TestProject.CreateAsync();
        var service = new LocalProjectService();
        var repository = new BibleReferenceRepository(project.Workspace.DatabasePath);

        await repository.SaveAsync(new BibleReference
        {
            Book = "Römer",
            ChapterStart = 8
        });

        var statistics = await service.ReadStatisticsAsync(project.Workspace);

        Assert.Equal(1, statistics.BibleReferenceCount);
    }
}
