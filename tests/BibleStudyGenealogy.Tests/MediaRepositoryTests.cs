using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Infrastructure.Repositories;
using BibleStudyGenealogy.Infrastructure.Services;
using ScriptureEvent = BibleStudyGenealogy.Core.Models.Event;

namespace BibleStudyGenealogy.Tests;

public sealed class MediaRepositoryTests
{
    [Fact]
    public async Task ImportAsync_CopiesFileAndStoresRelativePath()
    {
        await using var project = await TestProject.CreateAsync();
        var mediaImportService = new MediaImportService();
        var mediaRepository = new MediaRepository(project.Workspace.DatabasePath);
        var sourceFilePath = CreateSourceFile(project.Workspace.RootDirectory, "portrait.png", "image-data");

        var mediaFile = await mediaImportService.ImportAsync(project.Workspace, sourceFilePath, "Portrait");
        await mediaRepository.SaveAsync(mediaFile);

        var loadedMediaFile = await mediaRepository.GetByIdAsync(mediaFile.Id);
        var count = await mediaRepository.CountAsync();

        Assert.NotNull(loadedMediaFile);
        Assert.Equal(1, count);
        Assert.Equal("portrait.png", loadedMediaFile.OriginalFileName);
        Assert.Equal(MediaType.Image, loadedMediaFile.MediaType);
        Assert.False(Path.IsPathRooted(loadedMediaFile.RelativePath));
        Assert.True(mediaImportService.FileExists(project.Workspace, loadedMediaFile));
    }

    [Fact]
    public async Task ImportAsync_CreatesUniqueTargetFileNames()
    {
        await using var project = await TestProject.CreateAsync();
        var mediaImportService = new MediaImportService();
        var firstSourceFilePath = CreateSourceFile(project.Workspace.RootDirectory, "quelle-1.png", "first");
        var secondSourceFilePath = CreateSourceFile(project.Workspace.RootDirectory, "quelle-2.png", "second");
        var firstNamedSource = Path.Combine(Path.GetDirectoryName(firstSourceFilePath)!, "same.png");
        var secondNamedSource = Path.Combine(Path.GetDirectoryName(secondSourceFilePath)!, "same.png");

        File.Copy(firstSourceFilePath, firstNamedSource);
        var firstMediaFile = await mediaImportService.ImportAsync(project.Workspace, firstNamedSource);
        File.Delete(firstNamedSource);
        File.Copy(secondSourceFilePath, secondNamedSource);
        var secondMediaFile = await mediaImportService.ImportAsync(project.Workspace, secondNamedSource);

        Assert.NotEqual(firstMediaFile.RelativePath, secondMediaFile.RelativePath);
        Assert.EndsWith("same.png", firstMediaFile.RelativePath);
        Assert.Contains("same-001.png", secondMediaFile.RelativePath);
    }

    [Fact]
    public async Task SearchAsync_FindsMediaByDescription()
    {
        await using var project = await TestProject.CreateAsync();
        var mediaRepository = new MediaRepository(project.Workspace.DatabasePath);
        var mediaFile = new MediaFile
        {
            OriginalFileName = "notiz.pdf",
            RelativePath = Path.Combine("Media", "PDFs", "notiz.pdf"),
            MediaType = MediaType.Pdf,
            MimeType = "application/pdf",
            FileSizeBytes = 12,
            Description = "Archäologische Notiz"
        };

        await mediaRepository.SaveAsync(mediaFile);

        var results = await mediaRepository.SearchAsync("Archäologische");

        Assert.Single(results);
        Assert.Equal(mediaFile.Id, results[0].Id);
    }

    [Fact]
    public async Task LinkAsync_LoadsMediaForPersonAndEvent()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var eventRepository = new EventRepository(project.Workspace.DatabasePath);
        var mediaRepository = new MediaRepository(project.Workspace.DatabasePath);
        var person = new Person { MainName = "Paulus" };
        var scriptureEvent = new ScriptureEvent
        {
            Title = "Reise nach Rom",
            EventType = EventType.Journey
        };
        var mediaFile = new MediaFile
        {
            OriginalFileName = "karte.pdf",
            RelativePath = Path.Combine("Media", "PDFs", "karte.pdf"),
            MediaType = MediaType.Pdf,
            MimeType = "application/pdf",
            FileSizeBytes = 42
        };

        await personRepository.SaveAsync(person);
        await eventRepository.SaveAsync(scriptureEvent);
        await mediaRepository.SaveAsync(mediaFile);
        await mediaRepository.LinkAsync(mediaFile.Id, LinkedEntityType.Person, person.Id);
        await mediaRepository.LinkAsync(mediaFile.Id, LinkedEntityType.Event, scriptureEvent.Id);

        var personMedia = await mediaRepository.GetForEntityAsync(LinkedEntityType.Person, person.Id);
        var eventMedia = await mediaRepository.GetForEntityAsync(LinkedEntityType.Event, scriptureEvent.Id);

        Assert.Single(personMedia);
        Assert.Single(eventMedia);
        Assert.Equal(mediaFile.Id, personMedia[0].Id);
        Assert.Equal(mediaFile.Id, eventMedia[0].Id);
    }

    [Fact]
    public async Task FileExists_ReturnsFalseWhenImportedFileIsMissing()
    {
        await using var project = await TestProject.CreateAsync();
        var mediaImportService = new MediaImportService();
        var sourceFilePath = CreateSourceFile(project.Workspace.RootDirectory, "missing.pdf", "pdf-data");
        var mediaFile = await mediaImportService.ImportAsync(project.Workspace, sourceFilePath);
        var importedFilePath = mediaImportService.GetAbsolutePath(project.Workspace, mediaFile);

        File.Delete(importedFilePath);

        Assert.False(mediaImportService.FileExists(project.Workspace, mediaFile));
    }

    [Fact]
    public async Task PersonRepository_PersistsPortraitMediaFileId()
    {
        await using var project = await TestProject.CreateAsync();
        var personRepository = new PersonRepository(project.Workspace.DatabasePath);
        var mediaRepository = new MediaRepository(project.Workspace.DatabasePath);
        var mediaFile = new MediaFile
        {
            OriginalFileName = "portrait.png",
            RelativePath = Path.Combine("Media", "Persons", "portrait.png"),
            MediaType = MediaType.Image,
            MimeType = "image/png",
            FileSizeBytes = 128
        };
        var person = new Person
        {
            MainName = "Maria",
            PortraitMediaFileId = mediaFile.Id
        };

        await mediaRepository.SaveAsync(mediaFile);
        await personRepository.SaveAsync(person);

        var loadedPerson = await personRepository.GetByIdAsync(person.Id);

        Assert.NotNull(loadedPerson);
        Assert.Equal(mediaFile.Id, loadedPerson.PortraitMediaFileId);
    }

    [Fact]
    public async Task ProjectStatistics_UsesMediaFileCount()
    {
        await using var project = await TestProject.CreateAsync();
        var service = new LocalProjectService();
        var mediaRepository = new MediaRepository(project.Workspace.DatabasePath);

        await mediaRepository.SaveAsync(new MediaFile
        {
            OriginalFileName = "anhang.txt",
            RelativePath = Path.Combine("Media", "Other", "anhang.txt"),
            MediaType = MediaType.Document,
            MimeType = "text/plain",
            FileSizeBytes = 5
        });

        var statistics = await service.ReadStatisticsAsync(project.Workspace);

        Assert.Equal(1, statistics.MediaFileCount);
    }

    private static string CreateSourceFile(string projectRootDirectory, string fileName, string content)
    {
        var sourceDirectory = Path.Combine(projectRootDirectory, "TestSources");
        Directory.CreateDirectory(sourceDirectory);
        var sourceFilePath = Path.Combine(sourceDirectory, fileName);
        File.WriteAllText(sourceFilePath, content);
        return sourceFilePath;
    }
}
