namespace BibleStudyGenealogy.Core.Models;

public sealed class Person
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string MainName { get; set; } = string.Empty;

    public string AlternativeNames { get; set; } = string.Empty;

    public string HebrewName { get; set; } = string.Empty;

    public string GreekName { get; set; } = string.Empty;

    public string NameMeaning { get; set; } = string.Empty;

    public Gender Gender { get; set; } = Gender.Unknown;

    public DateInfo? BirthDateInfo { get; set; }

    public DateInfo? DeathDateInfo { get; set; }

    public string PrimaryRole { get; set; } = string.Empty;

    public string Occupation { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string LongDescription { get; set; } = string.Empty;

    public Guid? PortraitMediaFileId { get; set; }

    public PersonStatus Status { get; set; } = PersonStatus.Active;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
