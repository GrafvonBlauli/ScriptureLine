namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeSnapshot(
    FamilyTreeNode FocusPerson,
    IReadOnlyList<FamilyTreeNode> Parents,
    IReadOnlyList<FamilyTreeNode> Partners,
    IReadOnlyList<FamilyTreeNode> Children,
    IReadOnlyList<FamilyTreeNode> OtherRelations,
    IReadOnlyList<FamilyTreeLink> Links);
