namespace BibleStudyGenealogy.Core.Models;

public sealed class BibleReference
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Translation { get; set; } = string.Empty;

    public string Book { get; set; } = string.Empty;

    public int ChapterStart { get; set; }

    public int? VerseStart { get; set; }

    public int? ChapterEnd { get; set; }

    public int? VerseEnd { get; set; }

    public string ReferenceText { get; set; } = string.Empty;

    public string UserSummary { get; set; } = string.Empty;

    public string UserComment { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
