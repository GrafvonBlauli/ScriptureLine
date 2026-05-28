using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Rendering.TreeLayout;

namespace BibleStudyGenealogy.Tests;

public sealed class FamilyTreeViewportServiceTests
{
    [Fact]
    public void ZoomAtPointer_KeepsWorldPointUnderPointerStable()
    {
        var service = new FamilyTreeViewportService();
        var state = new FamilyTreeViewportState(1, 240, 120, 900, 600);
        var pointer = new TreePoint(300, 200);
        var worldBefore = service.ScreenToWorld(pointer, state);

        var nextState = service.ZoomAtPointer(pointer, 0.35, state);
        var screenAfter = service.WorldToScreen(worldBefore, nextState);

        Assert.Equal(pointer.X, screenAfter.X, precision: 6);
        Assert.Equal(pointer.Y, screenAfter.Y, precision: 6);
    }

    [Fact]
    public void ZoomAtPointer_ClampsOffsetToContentBounds()
    {
        var service = new FamilyTreeViewportService();
        var state = new FamilyTreeViewportState(1, 700, 500, 500, 400);

        var nextState = service.ZoomAtPointer(new TreePoint(480, 380), 0.5, state, 900, 700);

        Assert.InRange(nextState.OffsetX, 0, 900 * nextState.ZoomFactor - state.ViewportWidth);
        Assert.InRange(nextState.OffsetY, 0, 700 * nextState.ZoomFactor - state.ViewportHeight);
    }

    [Fact]
    public void CenterOnNode_PlacesNodeCenterInViewportCenter()
    {
        var service = new FamilyTreeViewportService();
        var node = new FamilyTreeDiagramNode(
            Guid.NewGuid(),
            "Noah",
            string.Empty,
            FamilyTreeNodeKind.Focus,
            0,
            500,
            300,
            true,
            false);
        var state = new FamilyTreeViewportState(1, 0, 0, 1000, 700);

        var centeredState = service.CenterOnNode(node, state);
        var nodeCenter = service.WorldToScreen(
            new TreePoint(
                node.X + FamilyTreeLayoutMetrics.NodeCenterX,
                node.Y + FamilyTreeLayoutMetrics.NodeCenterY),
            centeredState);

        Assert.Equal(state.ViewportWidth / 2, nodeCenter.X, precision: 6);
        Assert.Equal(state.ViewportHeight / 2, nodeCenter.Y, precision: 6);
    }

    [Fact]
    public void FitToTree_ChoosesZoomThatFitsContentIntoViewport()
    {
        var service = new FamilyTreeViewportService();
        var state = new FamilyTreeViewportState(1, 200, 180, 500, 400);

        var nextState = service.FitToTree(state, 1000, 800);

        Assert.Equal(0.5, nextState.ZoomFactor, precision: 6);
        Assert.Equal(0, nextState.OffsetX);
        Assert.Equal(0, nextState.OffsetY);
    }

    [Fact]
    public void PanBy_ClampsToContentBounds()
    {
        var service = new FamilyTreeViewportService();
        var state = new FamilyTreeViewportState(1, 50, 60, 500, 400);

        var nextState = service.PanBy(state, 700, 600, 900, 700);

        Assert.Equal(400, nextState.OffsetX);
        Assert.Equal(300, nextState.OffsetY);
    }
}
