namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeDiagram(
    IReadOnlyList<FamilyTreeDiagramNode> Nodes,
    IReadOnlyList<FamilyTreeDiagramLink> Links,
    IReadOnlyList<FamilyTreeDiagramConnector> Connectors,
    IReadOnlyList<FamilyTreeConnection> Connections,
    double Width,
    double Height);
