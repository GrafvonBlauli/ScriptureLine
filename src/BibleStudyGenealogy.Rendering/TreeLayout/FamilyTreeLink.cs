using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeLink(
    Guid RelationshipId,
    Guid FromPersonId,
    Guid ToPersonId,
    RelationshipType RelationshipType,
    CertaintyLevel CertaintyLevel,
    bool IsDirectional,
    bool IsUncertain);
