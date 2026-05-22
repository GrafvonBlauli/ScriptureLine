using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using Microsoft.Data.Sqlite;

namespace BibleStudyGenealogy.Tests;

public sealed class RelationshipRepositoryTests
{
    [Fact]
    public async Task SaveAsync_CreatesRelationshipAndIncreasesCount()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var abraham = new Person { MainName = "Abraham", Gender = Gender.Male };
        var isaac = new Person { MainName = "Isaak", Gender = Gender.Male };

        await personRepository.SaveAsync(abraham);
        await personRepository.SaveAsync(isaac);
        await relationshipRepository.SaveAsync(new Relationship
        {
            PersonAId = abraham.Id,
            PersonBId = isaac.Id,
            RelationshipType = RelationshipType.ParentChild,
            Direction = RelationshipDirection.PersonAToPersonB,
            CertaintyLevel = CertaintyLevel.ExplicitlyMentioned,
            Comment = "Vater und Sohn"
        });

        var count = await relationshipRepository.CountAsync();
        var relationships = await relationshipRepository.GetForPersonAsync(abraham.Id);

        Assert.Equal(1, count);
        Assert.Single(relationships);
        Assert.Equal(RelationshipType.ParentChild, relationships[0].RelationshipType);
        Assert.Equal("Vater und Sohn", relationships[0].Comment);
    }

    [Fact]
    public async Task GetForPersonAsync_ReturnsRelationshipForBothPeople()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var aquila = new Person { MainName = "Aquila", Gender = Gender.Male };
        var priscilla = new Person { MainName = "Priscilla", Gender = Gender.Female };

        await personRepository.SaveAsync(aquila);
        await personRepository.SaveAsync(priscilla);
        await relationshipRepository.SaveAsync(new Relationship
        {
            PersonAId = aquila.Id,
            PersonBId = priscilla.Id,
            RelationshipType = RelationshipType.Spouse,
            Direction = RelationshipDirection.Undirected,
            CertaintyLevel = CertaintyLevel.Likely
        });

        var firstPersonRelationships = await relationshipRepository.GetForPersonAsync(aquila.Id);
        var secondPersonRelationships = await relationshipRepository.GetForPersonAsync(priscilla.Id);

        Assert.Single(firstPersonRelationships);
        Assert.Single(secondPersonRelationships);
        Assert.Equal(firstPersonRelationships[0].Id, secondPersonRelationships[0].Id);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingRelationshipWithoutDuplicate()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var personA = new Person { MainName = "Person A" };
        var personB = new Person { MainName = "Person B" };
        var relationship = new Relationship
        {
            PersonAId = personA.Id,
            PersonBId = personB.Id,
            RelationshipType = RelationshipType.UnknownRelated,
            Direction = RelationshipDirection.Undirected,
            CertaintyLevel = CertaintyLevel.Unknown,
            Comment = "Erste Notiz"
        };

        await personRepository.SaveAsync(personA);
        await personRepository.SaveAsync(personB);
        await relationshipRepository.SaveAsync(relationship);
        relationship.Comment = "Aktualisierte Notiz";
        relationship.CertaintyLevel = CertaintyLevel.Possible;
        await relationshipRepository.SaveAsync(relationship);

        var loadedRelationship = await relationshipRepository.GetByIdAsync(relationship.Id);
        var count = await relationshipRepository.CountAsync();

        Assert.NotNull(loadedRelationship);
        Assert.Equal("Aktualisierte Notiz", loadedRelationship.Comment);
        Assert.Equal(CertaintyLevel.Possible, loadedRelationship.CertaintyLevel);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveAsync_RejectsDuplicateRelationship()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var personA = new Person { MainName = "Person A" };
        var personB = new Person { MainName = "Person B" };

        await personRepository.SaveAsync(personA);
        await personRepository.SaveAsync(personB);
        await relationshipRepository.SaveAsync(new Relationship
        {
            PersonAId = personA.Id,
            PersonBId = personB.Id,
            RelationshipType = RelationshipType.Sibling,
            Direction = RelationshipDirection.Undirected
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            relationshipRepository.SaveAsync(new Relationship
            {
                PersonAId = personB.Id,
                PersonBId = personA.Id,
                RelationshipType = RelationshipType.Sibling,
                Direction = RelationshipDirection.Undirected
            }));

        Assert.Equal("Diese Beziehung ist bereits vorhanden.", exception.Message);
    }

    [Fact]
    public async Task SaveAsync_RejectsMissingPeople()
    {
        await using var project = await TestProject.CreateAsync();
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);

        await Assert.ThrowsAsync<SqliteException>(() =>
            relationshipRepository.SaveAsync(new Relationship
            {
                PersonAId = Guid.NewGuid(),
                PersonBId = Guid.NewGuid(),
                RelationshipType = RelationshipType.Spouse,
                Direction = RelationshipDirection.Undirected
            }));
    }

    [Fact]
    public async Task ArchiveAsync_HidesRelationshipFromDefaultListsAndCount()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var personA = new Person { MainName = "Person A" };
        var personB = new Person { MainName = "Person B" };
        var relationship = new Relationship
        {
            PersonAId = personA.Id,
            PersonBId = personB.Id,
            RelationshipType = RelationshipType.Spouse,
            Direction = RelationshipDirection.Undirected
        };

        await personRepository.SaveAsync(personA);
        await personRepository.SaveAsync(personB);
        await relationshipRepository.SaveAsync(relationship);
        await relationshipRepository.ArchiveAsync(relationship.Id);

        var relationships = await relationshipRepository.GetForPersonAsync(personA.Id);
        var loadedRelationship = await relationshipRepository.GetByIdAsync(relationship.Id);
        var count = await relationshipRepository.CountAsync();

        Assert.Empty(relationships);
        Assert.NotNull(loadedRelationship);
        Assert.Equal(RelationshipStatus.Archived, loadedRelationship.Status);
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ProjectStatistics_UsesRelationshipCount()
    {
        await using var project = await TestProject.CreateAsync();
        var service = new LocalProjectService();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var relationshipRepository = new RelationshipRepository(project.Workspace.DatabasePath);
        var personA = new Person { MainName = "Person A" };
        var personB = new Person { MainName = "Person B" };

        await personRepository.SaveAsync(personA);
        await personRepository.SaveAsync(personB);
        await relationshipRepository.SaveAsync(new Relationship
        {
            PersonAId = personA.Id,
            PersonBId = personB.Id,
            RelationshipType = RelationshipType.TribeMember,
            Direction = RelationshipDirection.Undirected
        });

        var statistics = await service.ReadStatisticsAsync(project.Workspace);

        Assert.Equal(1, statistics.RelationshipCount);
    }
}
