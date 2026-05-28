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
        FamilyTreeViewportState state,
        double contentWidth = 0,
        double contentHeight = 0)
    {
        var worldBeforeZoom = ScreenToWorld(pointerScreenPoint, state);
        var nextZoom = ClampZoom(state.ZoomFactor + zoomDelta);
        var nextOffsetX = worldBeforeZoom.X * nextZoom - pointerScreenPoint.X;
        var nextOffsetY = worldBeforeZoom.Y * nextZoom - pointerScreenPoint.Y;

        return ClampOffset(state with
        {
            ZoomFactor = nextZoom,
            OffsetX = nextOffsetX,
            OffsetY = nextOffsetY
        }, contentWidth, contentHeight);
    }

    public FamilyTreeViewportState CenterOnNode(
        FamilyTreeDiagramNode node,
        FamilyTreeViewportState state,
        double contentWidth = 0,
        double contentHeight = 0)
    {
        var zoom = ClampZoom(state.ZoomFactor);
        var nodeCenterX = (node.X + FamilyTreeLayoutMetrics.NodeCenterX) * zoom;
        var nodeCenterY = (node.Y + FamilyTreeLayoutMetrics.NodeCenterY) * zoom;

        return ClampOffset(state with
        {
            ZoomFactor = zoom,
            OffsetX = Math.Max(0, nodeCenterX - state.ViewportWidth / 2),
            OffsetY = Math.Max(0, nodeCenterY - state.ViewportHeight / 2)
        }, contentWidth, contentHeight);
    }

    public FamilyTreeViewportState PanBy(FamilyTreeViewportState state, double deltaX, double deltaY, double contentWidth, double contentHeight)
    {
        return ClampOffset(state with
        {
            OffsetX = state.OffsetX + deltaX,
            OffsetY = state.OffsetY + deltaY
        }, contentWidth, contentHeight);
    }

    public FamilyTreeViewportState FitToTree(FamilyTreeViewportState state, double contentWidth, double contentHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0 || state.ViewportWidth <= 0 || state.ViewportHeight <= 0)
        {
            return ResetView(state);
        }

        var zoomX = state.ViewportWidth / contentWidth;
        var zoomY = state.ViewportHeight / contentHeight;
        var zoom = ClampZoom(Math.Min(zoomX, zoomY));
        return ClampOffset(state with
        {
            ZoomFactor = zoom,
            OffsetX = Math.Max(0, (contentWidth * zoom - state.ViewportWidth) / 2),
            OffsetY = Math.Max(0, (contentHeight * zoom - state.ViewportHeight) / 2)
        }, contentWidth, contentHeight);
    }

    public static FamilyTreeViewportState ResetView(FamilyTreeViewportState state)
    {
        return state with
        {
            ZoomFactor = 1,
            OffsetX = 0,
            OffsetY = 0
        };
    }

    public static FamilyTreeViewportState ClampOffset(FamilyTreeViewportState state, double contentWidth, double contentHeight)
    {
        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return state with
            {
                OffsetX = Math.Max(0, state.OffsetX),
                OffsetY = Math.Max(0, state.OffsetY)
            };
        }

        var zoom = ClampZoom(state.ZoomFactor);
        var maxOffsetX = Math.Max(0, contentWidth * zoom - state.ViewportWidth);
        var maxOffsetY = Math.Max(0, contentHeight * zoom - state.ViewportHeight);
        return state with
        {
            ZoomFactor = zoom,
            OffsetX = Math.Clamp(state.OffsetX, 0, maxOffsetX),
            OffsetY = Math.Clamp(state.OffsetY, 0, maxOffsetY)
        };
    }

    public static double ClampZoom(double zoom)
    {
        return Math.Clamp(zoom, MinZoom, MaxZoom);
    }
}
