using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;

namespace BibleStudyGenealogy.Tests;

public sealed class PersonRepositoryTests
{
    [Fact]
    public async Task SaveAsync_CreatesPersonAndIncreasesCount()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new PersonRepository(project.Workspace.DatabasePath);

        await repository.SaveAsync(new Person
        {
            MainName = "Abraham",
            Gender = Gender.Male,
            PrimaryRole = "Patriarch",
            Status = PersonStatus.Active
        });

        var count = await repository.CountAsync();
        var people = await repository.SearchAsync("Abraham");

        Assert.Equal(1, count);
        Assert.Single(people);
        Assert.Equal("Patriarch", people[0].PrimaryRole);
        Assert.Equal(Gender.Male, people[0].Gender);
    }

    [Fact]
    public async Task SearchAsync_FindsPersonByAlternativeName()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new PersonRepository(project.Workspace.DatabasePath);

        await repository.SaveAsync(new Person
        {
            MainName = "Simon Petrus",
            AlternativeNames = "Kephas",
            Gender = Gender.Male
        });

        var people = await repository.SearchAsync("Kephas");

        Assert.Single(people);
        Assert.Equal("Simon Petrus", people[0].MainName);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingPersonWithoutCreatingDuplicate()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new PersonRepository(project.Workspace.DatabasePath);
        var person = new Person
        {
            MainName = "Sara",
            ShortDescription = "Erste Notiz",
            Gender = Gender.Female
        };

        await repository.SaveAsync(person);
        person.ShortDescription = "Aktualisierte Notiz";
        person.Status = PersonStatus.Uncertain;
        await repository.SaveAsync(person);

        var loadedPerson = await repository.GetByIdAsync(person.Id);
        var count = await repository.CountAsync();

        Assert.NotNull(loadedPerson);
        Assert.Equal("Aktualisierte Notiz", loadedPerson.ShortDescription);
        Assert.Equal(PersonStatus.Uncertain, loadedPerson.Status);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task SaveAsync_PersistsBirthAndDeathDates()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new PersonRepository(project.Workspace.DatabasePath);
        var person = new Person
        {
            MainName = "Ursula Täuber",
            BirthDateInfo = new DateInfo
            {
                ApproximationText = "ca. 1380",
                Year = 1380,
                DateType = DateType.TextOnly
            },
            DeathDateInfo = new DateInfo
            {
                ApproximationText = "1449",
                Year = 1449,
                DateType = DateType.ExactYear
            }
        };

        await repository.SaveAsync(person);

        var loadedPerson = await repository.GetByIdAsync(person.Id);

        Assert.NotNull(loadedPerson);
        Assert.Equal("ca. 1380", loadedPerson.BirthDateInfo?.ApproximationText);
        Assert.Equal(1380, loadedPerson.BirthDateInfo?.Year);
        Assert.Equal("1449", loadedPerson.DeathDateInfo?.ApproximationText);
        Assert.Equal(1449, loadedPerson.DeathDateInfo?.Year);
    }

    [Fact]
    public async Task SaveAsync_PersistsSideboardLifeDetails()
    {
        await using var project = await TestProject.CreateAsync();
        var repository = new PersonRepository(project.Workspace.DatabasePath);
        var person = new Person
        {
            MainName = "Mose",
            AgeAtDeath = 120,
            BirthPlaceText = "Ägypten",
            DeathPlaceText = "Moab",
            BirthDateInfo = new DateInfo
            {
                ApproximationText = "ca. 1526 v. Chr.",
                Year = 1526,
                IsBeforeChrist = true,
                DateType = DateType.TextOnly
            },
            DeathDateInfo = new DateInfo
            {
                ApproximationText = "ca. 1406 v. Chr.",
                Year = 1406,
                IsBeforeChrist = true,
                DateType = DateType.TextOnly
            }
        };

        await repository.SaveAsync(person);

        var loadedPerson = await repository.GetByIdAsync(person.Id);

        Assert.NotNull(loadedPerson);
        Assert.Equal(120, loadedPerson.AgeAtDeath);
        Assert.Equal("Ägypten", loadedPerson.BirthPlaceText);
        Assert.Equal("Moab", loadedPerson.DeathPlaceText);
        Assert.True(loadedPerson.BirthDateInfo?.IsBeforeChrist);
        Assert.True(loadedPerson.DeathDateInfo?.IsBeforeChrist);
    }
}
