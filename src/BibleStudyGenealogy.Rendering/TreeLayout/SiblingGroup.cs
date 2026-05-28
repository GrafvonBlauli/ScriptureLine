namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record SiblingGroup(Guid ParentGroupId, IReadOnlyList<Guid> SiblingPersonIds);
