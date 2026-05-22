namespace BibleStudyGenealogy.Core.Models;

public sealed class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public EventType EventType { get; set; } = EventType.Other;

    public DateInfo? DateInfo { get; set; }

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public CertaintyLevel CertaintyLevel { get; set; } = CertaintyLevel.Unknown;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}
