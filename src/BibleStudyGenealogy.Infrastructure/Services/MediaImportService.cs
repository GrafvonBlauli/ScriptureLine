using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Infrastructure.Services;

public sealed class MediaImportService
{
    public async Task<MediaFile> ImportAsync(
        ProjectWorkspace workspace,
        string sourceFilePath,
        string description = "",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);

        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Die ausgewählte Datei wurde nicht gefunden.", sourceFilePath);
        }

        var sourceFileInfo = new FileInfo(sourceFilePath);
        var mediaType = DetermineMediaType(sourceFileInfo.Extension);
        var targetFolder = GetTargetFolder(workspace.RootDirectory, mediaType);
        Directory.CreateDirectory(targetFolder);

        var targetFileName = CreateUniqueFileName(targetFolder, sourceFileInfo.Name);
        var targetPath = Path.Combine(targetFolder, targetFileName);

        await using (var sourceStream = File.OpenRead(sourceFilePath))
        await using (var targetStream = File.Create(targetPath))
        {
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }

        return new MediaFile
        {
            OriginalFileName = sourceFileInfo.Name,
            RelativePath = Path.GetRelativePath(workspace.RootDirectory, targetPath),
            MediaType = mediaType,
            MimeType = DetermineMimeType(sourceFileInfo.Extension),
            FileSizeBytes = sourceFileInfo.Length,
            Description = description.Trim()
        };
    }

    public bool FileExists(ProjectWorkspace workspace, MediaFile mediaFile)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(mediaFile);

        return File.Exists(GetAbsolutePath(workspace, mediaFile));
    }

    public string GetAbsolutePath(ProjectWorkspace workspace, MediaFile mediaFile)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(mediaFile);

        return Path.GetFullPath(Path.Combine(workspace.RootDirectory, mediaFile.RelativePath));
    }

    private static string GetTargetFolder(string projectRootDirectory, MediaType mediaType)
    {
        var folder = mediaType switch
        {
            MediaType.Image => Path.Combine("Media", "Persons"),
            MediaType.Pdf => Path.Combine("Media", "PDFs"),
            MediaType.Map => Path.Combine("Media", "Maps"),
            _ => Path.Combine("Media", "Other")
        };

        return Path.Combine(projectRootDirectory, folder);
    }

    private static string CreateUniqueFileName(string targetFolder, string originalFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(originalFileName);
        var extension = Path.GetExtension(originalFileName);
        var safeBaseName = SanitizeFileName(baseName);
        var safeExtension = SanitizeExtension(extension);
        var candidate = $"{safeBaseName}{safeExtension}";

        if (!File.Exists(Path.Combine(targetFolder, candidate)))
        {
            return candidate;
        }

        for (var counter = 1; counter < 10_000; counter++)
        {
            candidate = $"{safeBaseName}-{counter:000}{safeExtension}";
            if (!File.Exists(Path.Combine(targetFolder, candidate)))
            {
                return candidate;
            }
        }

        return $"{safeBaseName}-{Guid.NewGuid():N}{safeExtension}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName
            .Select(character => invalidCharacters.Contains(character) ? '-' : character)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "media" : sanitized.Trim();
    }

    private static string SanitizeExtension(string extension)
    {
        return string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : new string(extension
                .Where(character => !Path.GetInvalidFileNameChars().Contains(character))
                .ToArray());
    }

    private static MediaType DetermineMediaType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => MediaType.Image,
            ".pdf" => MediaType.Pdf,
            ".doc" or ".docx" or ".txt" or ".md" or ".rtf" => MediaType.Document,
            ".geojson" or ".kml" or ".kmz" => MediaType.Map,
            _ => MediaType.Other
        };
    }

    private static string DetermineMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".rtf" => "application/rtf",
            ".geojson" => "application/geo+json",
            ".kml" => "application/vnd.google-earth.kml+xml",
            ".kmz" => "application/vnd.google-earth.kmz",
            _ => "application/octet-stream"
        };
    }
}
