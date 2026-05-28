namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class FamilyTreeViewportService
{
    public const double MinZoom = 0.45;
    public const double MaxZoom = 1.9;

    public TreePoint ScreenToWorld(TreePoint screenPoint, FamilyTreeViewportState state)
    {
        var zoom = ClampZoom(state.ZoomFactor);
        return new TreePoint(
            (state.OffsetX + screenPoint.X) / zoom,
            (state.OffsetY + screenPoint.Y) / zoom);
    }

    public TreePoint WorldToScreen(TreePoint worldPoint, FamilyTreeViewportState state)
    {
        var zoom = ClampZoom(state.ZoomFactor);
        return new TreePoint(
            worldPoint.X * zoom - state.OffsetX,
            worldPoint.Y * zoom - state.OffsetY);
    }

    public FamilyTreeViewportState ZoomAtPointer(
        TreePoint pointerScreenPoint,
        double zoomDelta,
        FamilyTreeViewportState state)
    {
        var worldBeforeZoom = ScreenToWorld(pointerScreenPoint, state);
        var nextZoom = ClampZoom(state.ZoomFactor + zoomDelta);
        var nextOffsetX = worldBeforeZoom.X * nextZoom - pointerScreenPoint.X;
        var nextOffsetY = worldBeforeZoom.Y * nextZoom - pointerScreenPoint.Y;

        return state with
        {
            ZoomFactor = nextZoom,
            OffsetX = Math.Max(0, nextOffsetX),
            OffsetY = Math.Max(0, nextOffsetY)
        };
    }

    public FamilyTreeViewportState CenterOnNode(
        FamilyTreeDiagramNode node,
        FamilyTreeViewportState state)
    {
        var zoom = ClampZoom(state.ZoomFactor);
        var nodeCenterX = (node.X + FamilyTreeLayoutMetrics.NodeCenterX) * zoom;
        var nodeCenterY = (node.Y + FamilyTreeLayoutMetrics.NodeCenterY) * zoom;

        return state with
        {
            ZoomFactor = zoom,
            OffsetX = Math.Max(0, nodeCenterX - state.ViewportWidth / 2),
            OffsetY = Math.Max(0, nodeCenterY - state.ViewportHeight / 2)
        };
    }

    public static double ClampZoom(double zoom)
    {
        return Math.Clamp(zoom, MinZoom, MaxZoom);
    }
}
