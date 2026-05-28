using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record ParentGroup(
    Guid GroupId,
    string Key,
    Guid? FatherPersonId,
    Guid? MotherPersonId,
    IReadOnlyList<Guid> AdditionalParentIds,
    bool HasFatherPlaceholder,
    bool HasMotherPlaceholder,
    IReadOnlyList<Guid> ChildPersonIds,
    IReadOnlyList<Guid> RelationshipIds,
    CertaintyLevel CertaintyLevel,
    bool IsUncertain,
    Guid? PartnerRelationshipId);
