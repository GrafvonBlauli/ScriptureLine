using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeConnection(
    Guid Id,
    FamilyTreeConnectionType Type,
    TreePoint Start,
    TreePoint End,
    bool IsUncertain,
    Guid? FromPersonId = null,
    Guid? ToPersonId = null,
    Guid? FamilyGroupId = null,
    Guid? ParentGroupId = null,
    IReadOnlyList<Guid>? RelationshipIds = null,
    CertaintyLevel CertaintyLevel = CertaintyLevel.Unknown,
    FamilyTreeLineStyle LineStyle = FamilyTreeLineStyle.Solid);
