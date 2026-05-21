namespace BibleStudyGenealogy.Core.Models;

public sealed class Relationship
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PersonAId { get; set; }

    public Guid PersonBId { get; set; }

    public RelationshipType RelationshipType { get; set; } = RelationshipType.UnknownRelated;

    public RelationshipDirection Direction { get; set; } = RelationshipDirection.Undirected;

    public CertaintyLevel CertaintyLevel { get; set; } = CertaintyLevel.Unknown;

    public string SourceNote { get; set; } = string.Empty;

    public string Comment { get; set; } = string.Empty;

    public RelationshipStatus Status { get; set; } = RelationshipStatus.Active;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
