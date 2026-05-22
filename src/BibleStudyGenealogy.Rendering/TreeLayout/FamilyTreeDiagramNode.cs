namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeDiagramNode(
    Guid PersonId,
    string DisplayName,
    string Role,
    FamilyTreeNodeKind Kind,
    int Generation,
    double X,
    double Y,
    bool IsFocus,
    bool IsUncertain);
