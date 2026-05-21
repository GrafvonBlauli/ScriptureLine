namespace BibleStudyGenealogy.Core.Models;

public sealed class ProjectMetadata
{
    public Guid ProjectId { get; set; } = Guid.NewGuid();

    public string ProjectName { get; set; } = string.Empty;

    public int SchemaVersion { get; set; } = ProjectDefaults.SchemaVersion;

    public string AppVersion { get; set; } = "0.1.0";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastOpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
