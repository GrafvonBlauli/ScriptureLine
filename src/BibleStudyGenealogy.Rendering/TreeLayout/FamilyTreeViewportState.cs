namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeViewportState(
    double ZoomFactor,
    double OffsetX,
    double OffsetY,
    double ViewportWidth,
    double ViewportHeight);
