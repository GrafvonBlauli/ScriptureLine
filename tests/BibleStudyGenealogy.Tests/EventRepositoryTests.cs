using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Tests;

public sealed class EventRepositoryTests
{
    [Fact]
    public async Task SaveAsync_CreatesEventAndIncreasesCount()
    {
        await using var project = await TestProject.CreateAsync();
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Auszug aus Ägypten",
            EventType = EventType.Journey,
            CertaintyLevel = CertaintyLevel.ExplicitlyMentioned,
            ShortDescription = "Israel verlässt Ägypten"
        };

        await eventRepository.SaveAsync(scriptureEvent);

        var count = await eventRepository.CountAsync();
        var loadedEvent = await eventRepository.GetByIdAsync(scriptureEvent.Id);
        var searchResults = await eventRepository.SearchAsync("Ägypten");

        Assert.Equal(1, count);
        Assert.NotNull(loadedEvent);
        Assert.Equal(EventType.Journey, loadedEvent.EventType);
        Assert.Single(searchResults);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingEventWithoutDuplicate()
    {
        await using var project = await TestProject.CreateAsync();
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Berufung",
            EventType = EventType.Calling,
            CertaintyLevel = CertaintyLevel.Unknown,
            LongDescription = "Erste Notiz"
        };

        await eventRepository.SaveAsync(scriptureEvent);
        scriptureEvent.LongDescription = "Aktualisierte Notiz";
        scriptureEvent.CertaintyLevel = CertaintyLevel.Likely;
        await eventRepository.SaveAsync(scriptureEvent);

        var loadedEvent = await eventRepository.GetByIdAsync(scriptureEvent.Id);
        var count = await eventRepository.CountAsync();

        Assert.NotNull(loadedEvent);
        Assert.Equal("Aktualisierte Notiz", loadedEvent.LongDescription);
        Assert.Equal(CertaintyLevel.Likely, loadedEvent.CertaintyLevel);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveAsync_PreservesApproximateDateText()
    {
        await using var project = await TestProject.CreateAsync();
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Tempelweihe",
            EventType = EventType.Other,
            DateInfo = new DateInfo
            {
                ApproximationText = "um 960 v. Chr.",
                DateType = DateType.Unknown,
                CertaintyLevel = CertaintyLevel.Likely
            }
        };

        await eventRepository.SaveAsync(scriptureEvent);

        var loadedEvent = await eventRepository.GetByIdAsync(scriptureEvent.Id);

        Assert.NotNull(loadedEvent);
        Assert.NotNull(loadedEvent.DateInfo);
        Assert.Equal("um 960 v. Chr.", loadedEvent.DateInfo.ApproximationText);
        Assert.Equal(CertaintyLevel.Likely, loadedEvent.DateInfo.CertaintyLevel);
    }

    [Fact]
    public async Task LinkPersonAsync_LoadsEventsForPersonAndPeopleForEvent()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var moses = new Person { MainName = "Mose", Gender = Gender.Male };
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Berufung am Dornbusch",
            EventType = EventType.Calling
        };

        await personRepository.SaveAsync(moses);
        await eventRepository.SaveAsync(scriptureEvent);
        await eventRepository.LinkPersonAsync(scriptureEvent.Id, moses.Id);

        var personEvents = await eventRepository.GetForPersonAsync(moses.Id);
        var eventPeople = await eventRepository.GetPeopleForEventAsync(scriptureEvent.Id);

        Assert.Single(personEvents);
        Assert.Equal(scriptureEvent.Id, personEvents[0].Id);
        Assert.Single(eventPeople);
        Assert.Equal("Mose", eventPeople[0].MainName);
    }

    [Fact]
    public async Task LinkPersonAsync_RejectsMissingPerson()
    {
        await using var project = await TestProject.CreateAsync();
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Leeres Ereignis",
            EventType = EventType.Other
        };

        await eventRepository.SaveAsync(scriptureEvent);

        await Assert.ThrowsAsync<SqliteException>(() =>
            eventRepository.LinkPersonAsync(scriptureEvent.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task ProjectStatistics_UsesEventCount()
    {
        await using var project = await TestProject.CreateAsync();
        var service = new LocalProjectService();
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);

        await eventRepository.SaveAsync(new ScriptureEvent
        {
            Title = "Pfingsten",
            EventType = EventType.Miracle
        });

        var statistics = await service.ReadStatisticsAsync(project.Workspace);

        Assert.Equal(1, statistics.EventCount);
    }
}
