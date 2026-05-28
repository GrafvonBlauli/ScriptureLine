namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record ChildGroup(Guid ParentGroupId, IReadOnlyList<Guid> ChildPersonIds);
