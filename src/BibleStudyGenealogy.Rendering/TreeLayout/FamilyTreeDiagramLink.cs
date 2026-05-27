using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeDiagramLink(
    Guid RelationshipId,
    Guid FromPersonId,
    Guid ToPersonId,
    RelationshipType RelationshipType,
    CertaintyLevel CertaintyLevel,
    bool IsDirectional,
    bool IsUncertain,
    FamilyTreeDiagramLinkKind LinkKind = FamilyTreeDiagramLinkKind.Direct,
    Guid? FamilyGroupId = null);
