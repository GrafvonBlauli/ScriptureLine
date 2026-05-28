namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeDiagram(
    IReadOnlyList<FamilyTreeDiagramNode> Nodes,
    IReadOnlyList<FamilyTreeDiagramLink> Links,
    IReadOnlyList<FamilyTreeDiagramConnector> Connectors,
    IReadOnlyList<FamilyTreeConnection> Connections,
    IReadOnlyList<RelationshipValidationIssue> ValidationIssues,
    double Width,
    double Height);
