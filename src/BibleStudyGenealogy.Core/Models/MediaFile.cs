namespace BibleStudyGenealogy.Core.Models;

public sealed class MediaFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string OriginalFileName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public MediaType MediaType { get; set; } = MediaType.Other;

    public string MimeType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
