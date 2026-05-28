using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class FamilyTreeConnectionService
{
    public IReadOnlyList<FamilyTreeConnection> CreateConnections(
        IReadOnlyList<FamilyTreeDiagramNode> nodes,
        IReadOnlyList<FamilyTreeDiagramLink> links,
        IReadOnlyList<FamilyTreeDiagramConnector> connectors)
    {
        var connections = new List<FamilyTreeConnection>();
        var nodesById = nodes.ToDictionary(node => node.PersonId);

        foreach (var connectorGroup in connectors.GroupBy(connector => connector.FamilyGroupId))
        {
            var connectorList = connectorGroup.ToList();
            var firstConnector = connectorList[0];
            var familyPoint = new TreePoint(
                connectorList.Average(connector => connector.X),
                connectorList.Average(connector => connector.Y));
            var parentIds = new[]
                {
                    firstConnector.FatherPersonId,
                    firstConnector.MotherPersonId,
                    firstConnector.FatherPlaceholderId,
                    firstConnector.MotherPlaceholderId
                }
                .Where(id => id is not null)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            foreach (var parentId in parentIds)
            {
                if (!nodesById.TryGetValue(parentId, out var parentNode))
                {
                    continue;
                }

                var type = parentNode.IsPlaceholder
                    ? FamilyTreeConnectionType.Placeholder
                    : FamilyTreeConnectionType.ParentToFamily;
                connections.Add(new FamilyTreeConnection(
                    Guid.NewGuid(),
                    type,
                    GetTreeEdgePoint(parentNode, familyPoint),
                    familyPoint,
                    firstConnector.IsUncertain || parentNode.IsPlaceholder,
                    parentNode.PersonId,
                    firstConnector.ChildPersonId,
                    firstConnector.FamilyGroupId,
                    firstConnector.FamilyGroupId,
                    firstConnector.RelationshipIds ?? Array.Empty<Guid>(),
                    firstConnector.CertaintyLevel,
                    ResolveLineStyle(firstConnector.CertaintyLevel, firstConnector.IsUncertain || parentNode.IsPlaceholder)));
            }

            foreach (var connector in connectorList)
            {
                if (!nodesById.TryGetValue(connector.ChildPersonId, out var childNode))
                {
                    continue;
                }

                connections.Add(new FamilyTreeConnection(
                    Guid.NewGuid(),
                    FamilyTreeConnectionType.FamilyToChild,
                    familyPoint,
                    GetTreeEdgePoint(childNode, familyPoint),
                    connector.IsUncertain,
                    null,
                    childNode.PersonId,
                    connector.FamilyGroupId,
                    connector.FamilyGroupId,
                    connector.RelationshipIds ?? Array.Empty<Guid>(),
                    connector.CertaintyLevel,
                    ResolveLineStyle(connector.CertaintyLevel, connector.IsUncertain)));
            }
        }

        foreach (var link in links)
        {
            if (!nodesById.TryGetValue(link.FromPersonId, out var fromNode)
                || !nodesById.TryGetValue(link.ToPersonId, out var toNode))
            {
                continue;
            }

            connections.Add(new FamilyTreeConnection(
                link.RelationshipId,
                ToConnectionType(link.LinkKind),
                GetTreeEdgePoint(fromNode, toNode),
                GetTreeEdgePoint(toNode, fromNode),
                link.IsUncertain,
                fromNode.PersonId,
                toNode.PersonId,
                link.FamilyGroupId,
                link.FamilyGroupId,
                new[] { link.RelationshipId },
                link.CertaintyLevel,
                ResolveLineStyle(link.CertaintyLevel, link.IsUncertain)));
        }

        return connections;
    }

    public static FamilyTreeLineStyle ResolveLineStyle(CertaintyLevel certaintyLevel, bool isUncertain)
    {
        return certaintyLevel switch
        {
            CertaintyLevel.ExplicitlyMentioned or CertaintyLevel.Likely when !isUncertain => FamilyTreeLineStyle.Solid,
            CertaintyLevel.UserHypothesis => FamilyTreeLineStyle.Dotted,
            CertaintyLevel.Disputed => FamilyTreeLineStyle.MutedDashed,
            _ => isUncertain ? FamilyTreeLineStyle.Dashed : FamilyTreeLineStyle.Solid
        };
    }

    private static FamilyTreeConnectionType ToConnectionType(FamilyTreeDiagramLinkKind linkKind)
    {
        return linkKind switch
        {
            FamilyTreeDiagramLinkKind.Partner => FamilyTreeConnectionType.Partner,
            FamilyTreeDiagramLinkKind.Sibling => FamilyTreeConnectionType.Sibling,
            FamilyTreeDiagramLinkKind.ParentToFamily => FamilyTreeConnectionType.ParentToFamily,
            FamilyTreeDiagramLinkKind.FamilyToChild => FamilyTreeConnectionType.FamilyToChild,
            FamilyTreeDiagramLinkKind.Placeholder => FamilyTreeConnectionType.Placeholder,
            _ => FamilyTreeConnectionType.Direct
        };
    }

    private static TreePoint GetTreeEdgePoint(FamilyTreeDiagramNode fromNode, FamilyTreeDiagramNode toNode)
    {
        return GetTreeEdgePoint(
            fromNode,
            new TreePoint(
                toNode.X + FamilyTreeLayoutMetrics.NodeCenterX,
                toNode.Y + FamilyTreeLayoutMetrics.NodeCenterY));
    }

    private static TreePoint GetTreeEdgePoint(FamilyTreeDiagramNode fromNode, TreePoint targetPoint)
    {
        var fromCenterX = fromNode.X + FamilyTreeLayoutMetrics.NodeCenterX;
        var fromCenterY = fromNode.Y + FamilyTreeLayoutMetrics.NodeCenterY;
        var deltaX = targetPoint.X - fromCenterX;
        var deltaY = targetPoint.Y - fromCenterY;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            return new TreePoint(
                deltaX >= 0 ? fromNode.X + FamilyTreeLayoutMetrics.NodeWidth : fromNode.X,
                fromCenterY);
        }

        return new TreePoint(
            fromCenterX,
            deltaY >= 0 ? fromNode.Y + FamilyTreeLayoutMetrics.NodeHeight : fromNode.Y);
    }
}
