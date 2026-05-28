namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyUnit(Guid ParentGroupId, Guid? PartnerRelationshipId, IReadOnlyList<Guid> ChildPersonIds);
