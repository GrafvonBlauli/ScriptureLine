namespace BibleStudyGenealogy.Core.Models;

public sealed class MediaLink
{
    public Guid MediaFileId { get; set; }

    public LinkedEntityType EntityType { get; set; }

    public Guid EntityId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
}
