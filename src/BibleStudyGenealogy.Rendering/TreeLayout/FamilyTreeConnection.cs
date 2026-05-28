namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeConnection(
    Guid Id,
    FamilyTreeConnectionType Type,
    TreePoint Start,
    TreePoint End,
    bool IsUncertain,
    Guid? FromPersonId = null,
    Guid? ToPersonId = null,
    Guid? FamilyGroupId = null);
