using System.Diagnostics;
using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using Xunit.Abstractions;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Tests;

public sealed class RepositoryPerformanceTests
{
    private readonly ITestOutputHelper _output;

    public RepositoryPerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MediumStressProfile_CompletesCoreRepositoryOperations()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var bibleReferenceRepository = new BibleReferenceRepository(project.Workspace.DatabasePath);
        var service = new LocalProjectService();
        var people = CreatePeople();
        var events = CreateEvents();
        var references = CreateBibleReferences();

        var insertElapsed = await MeasureAsync("Bulk insert", async () =>
        {
            foreach (var person in people)
            {
                await personRepository.SaveAsync(person);
            }

            foreach (var relationship in CreateRelationships(people))
            {
                await relationshipRepository.SaveAsync(relationship);
            }

            foreach (var scriptureEvent in events)
            {
                await eventRepository.SaveAsync(scriptureEvent);
            }

            for (var index = 0; index < events.Count; index++)
            {
                await eventRepository.LinkPersonAsync(events[index].Id, people[(index * 2) % people.Count].Id);
            }

            foreach (var reference in references)
            {
                await bibleReferenceRepository.SaveAsync(reference);
            }

            for (var index = 0; index < events.Count; index++)
            {
                await bibleReferenceRepository.LinkEventAsync(events[index].Id, references[index].Id);
            }
        });

        var personSearchElapsed = await MeasureAsync("Person search", async () =>
        {
            var results = await personRepository.SearchAsync("Person 0199");
            Assert.NotEmpty(results);
        });
        var relationshipLoadElapsed = await MeasureAsync("Relationships for person", async () =>
        {
            var results = await relationshipRepository.GetForPersonAsync(people[500].Id);
            Assert.NotEmpty(results);
        });
        var eventLoadElapsed = await MeasureAsync("Events for person", async () =>
        {
            var results = await eventRepository.GetForPersonAsync(people[1000].Id);
            Assert.NotEmpty(results);
        });
        var bibleSearchElapsed = await MeasureAsync("Bible reference search", async () =>
        {
            var results = await bibleReferenceRepository.SearchAsync("Buch 12");
            Assert.NotEmpty(results);
        });
        var statisticsElapsed = await MeasureAsync("Statistics", async () =>
        {
            var statistics = await service.ReadStatisticsAsync(project.Workspace);
            Assert.Equal(2_000, statistics.PersonCount);
            Assert.Equal(4_000, statistics.RelationshipCount);
            Assert.Equal(1_000, statistics.EventCount);
            Assert.Equal(2_000, statistics.BibleReferenceCount);
        });

        Assert.True(insertElapsed < TimeSpan.FromSeconds(300), $"Bulk insert took {insertElapsed}.");
        Assert.True(personSearchElapsed < TimeSpan.FromSeconds(5), $"Person search took {personSearchElapsed}.");
        Assert.True(relationshipLoadElapsed < TimeSpan.FromSeconds(5), $"Relationship load took {relationshipLoadElapsed}.");
        Assert.True(eventLoadElapsed < TimeSpan.FromSeconds(5), $"Event load took {eventLoadElapsed}.");
        Assert.True(bibleSearchElapsed < TimeSpan.FromSeconds(5), $"Bible search took {bibleSearchElapsed}.");
        Assert.True(statisticsElapsed < TimeSpan.FromSeconds(2), $"Statistics took {statisticsElapsed}.");
    }

    private async Task<TimeSpan> MeasureAsync(string label, Func<Task> action)
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();
        _output.WriteLine($"{label}: {stopwatch.Elapsed}");
        return stopwatch.Elapsed;
    }

    private static IReadOnlyList<Person> CreatePeople()
    {
        return Enumerable.Range(0, 2_000)
            .Select(index => new Person
            {
                MainName = $"Person {index:0000}",
                PrimaryRole = index % 2 == 0 ? "Zeuge" : "Familienmitglied",
                Gender = index % 3 == 0 ? Gender.Female : Gender.Male
            })
            .ToList();
    }

    private static IReadOnlyList<Relationship> CreateRelationships(IReadOnlyList<Person> people)
    {
        var relationships = new List<Relationship>(4_000);

        for (var index = 0; index < 2_000; index++)
        {
            relationships.Add(new Relationship
            {
                PersonAId = people[index].Id,
                PersonBId = people[(index + 1) % people.Count].Id,
                RelationshipType = RelationshipType.ParentChild,
                Direction = RelationshipDirection.PersonAToPersonB,
                CertaintyLevel = CertaintyLevel.Likely
            });
        }

        for (var index = 0; index < 1_000; index++)
        {
            relationships.Add(new Relationship
            {
                PersonAId = people[index].Id,
                PersonBId = people[index + 1_000].Id,
                RelationshipType = RelationshipType.Spouse,
                Direction = RelationshipDirection.Undirected,
                CertaintyLevel = CertaintyLevel.Possible
            });
        }

        for (var index = 0; index < 1_000; index++)
        {
            relationships.Add(new Relationship
            {
                PersonAId = people[index].Id,
                PersonBId = people[(index + 500) % people.Count].Id,
                RelationshipType = RelationshipType.TribeMember,
                Direction = RelationshipDirection.Undirected,
                CertaintyLevel = CertaintyLevel.Unknown
            });
        }

        return relationships;
    }

    private static IReadOnlyList<ScriptureEvent> CreateEvents()
    {
        return Enumerable.Range(0, 1_000)
            .Select(index => new ScriptureEvent
            {
                Title = $"Ereignis {index:0000}",
                EventType = index % 2 == 0 ? EventType.Journey : EventType.Teaching,
                CertaintyLevel = CertaintyLevel.Likely,
                ShortDescription = $"Kurzbeschreibung {index:0000}",
                DateInfo = new DateInfo
                {
                    ApproximationText = $"Jahr {index}",
                    DateType = DateType.Unknown,
                    CertaintyLevel = CertaintyLevel.Possible
                }
            })
            .ToList();
    }

    private static IReadOnlyList<BibleReference> CreateBibleReferences()
    {
        return Enumerable.Range(0, 2_000)
            .Select(index => new BibleReference
            {
                Translation = "SL-Test",
                Book = $"Buch {index % 40:00}",
                ChapterStart = (index % 50) + 1,
                VerseStart = (index % 30) + 1,
                UserSummary = $"Zusammenfassung {index:0000}",
                UserComment = $"Kommentar {index:0000}"
            })
            .ToList();
    }
}
