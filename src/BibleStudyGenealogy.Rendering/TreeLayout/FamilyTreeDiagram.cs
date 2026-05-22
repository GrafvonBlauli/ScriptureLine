namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeDiagram(
    IReadOnlyList<FamilyTreeDiagramNode> Nodes,
    IReadOnlyList<FamilyTreeDiagramLink> Links,
    double Width,
    double Height);
