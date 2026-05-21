namespace BibleStudyGenealogy.Core.Models;

public sealed class ProjectSettings
{
    public string ProjectName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Language { get; set; } = ProjectDefaults.Language;

    public string PreferredBibleTranslation { get; set; } = ProjectDefaults.PreferredBibleTranslation;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastOpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
