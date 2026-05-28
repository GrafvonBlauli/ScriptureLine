using BibleStudyGenealogy.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class FamilyTreeBuilder
{
    private const double NodeWidth = FamilyTreeLayoutMetrics.NodeWidth;
    private const double NodeHeight = FamilyTreeLayoutMetrics.NodeHeight;
    private const double HorizontalGap = FamilyTreeLayoutMetrics.HorizontalGap;
    private const double VerticalGap = FamilyTreeLayoutMetrics.VerticalGap;
    private const double CanvasPadding = FamilyTreeLayoutMetrics.CanvasPadding;
    private readonly ParentGroupBuilder _parentGroupBuilder = new();
    private readonly FamilyTreeConnectionService _connectionService = new();

    public FamilyTreeSnapshot Build(Person focusPerson, IReadOnlyList<Person> people, IReadOnlyList<Relationship> relationships)
    {
        ArgumentNullException.ThrowIfNull(focusPerson);
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(relationships);

        var parents = new List<FamilyTreeNode>();
        var partners = new List<FamilyTreeNode>();
        var children = new List<FamilyTreeNode>();
        var otherRelations = new List<FamilyTreeNode>();
        var links = new List<FamilyTreeLink>();

        foreach (var relationship in relationships.Where(relationship => relationship.Status == RelationshipStatus.Active))
        {
            if (relationship.PersonAId != focusPerson.Id && relationship.PersonBId != focusPerson.Id)
            {
                continue;
            }

            var otherPersonId = relationship.PersonAId == focusPerson.Id
                ? relationship.PersonBId
                : relationship.PersonAId;
            var otherPerson = people.FirstOrDefault(person => person.Id == otherPersonId);
            if (otherPerson is null)
            {
                continue;
            }

            var nodeKind = DetermineNodeKind(focusPerson.Id, relationship);
            var node = CreateNode(otherPerson, nodeKind);

            switch (nodeKind)
            {
                case FamilyTreeNodeKind.Parent:
                    parents.Add(node);
                    break;
                case FamilyTreeNodeKind.Partner:
                    partners.Add(node);
                    break;
                case FamilyTreeNodeKind.Child:
                    children.Add(node);
                    break;
                default:
                    otherRelations.Add(node);
                    break;
            }

            links.Add(CreateLink(focusPerson.Id, relationship));
        }

        return new FamilyTreeSnapshot(
            CreateNode(focusPerson, FamilyTreeNodeKind.Focus),
            parents,
            partners,
            children,
            otherRelations,
            links);
    }

    public FamilyTreeDiagram BuildDiagram(
        Person focusPerson,
        IReadOnlyList<Person> people,
        IReadOnlyList<Relationship> relationships,
        FamilyTreeLayoutOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(focusPerson);
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(relationships);

        options ??= FamilyTreeLayoutOptions.Default;
        var peopleById = people.ToDictionary(person => person.Id);
        var relationshipGraph = RelationshipGraph.Create(peopleById, relationships);
        var activeRelationships = relationshipGraph.ActiveRelationships;

        var connectedPersonIds = FindConnectedPersonIds(focusPerson.Id, activeRelationships);
        var generations = AssignGenerations(focusPerson.Id, activeRelationships, connectedPersonIds);
        var visiblePersonIds = connectedPersonIds
            .Where(personId => options.ShowAllConnected || Math.Abs(generations.GetValueOrDefault(personId)) <= options.GenerationLimit)
            .Where(peopleById.ContainsKey)
            .ToHashSet();
        visiblePersonIds.Add(focusPerson.Id);

        var rowGroups = visiblePersonIds
            .Select(personId => new
            {
                Person = peopleById[personId],
                Generation = generations.GetValueOrDefault(personId),
                Kind = DetermineDiagramNodeKind(focusPerson.Id, personId, generations.GetValueOrDefault(personId), activeRelationships)
            })
            .GroupBy(item => item.Generation)
            .OrderBy(group => group.Key)
            .ToList();

        var nodes = new List<FamilyTreeDiagramNode>();
        var rowIndex = 0;
        var maxRowWidth = 0d;
        foreach (var row in rowGroups)
        {
            var orderedRow = row
                .OrderBy(item => item.Kind == FamilyTreeNodeKind.Focus ? 0 : 1)
                .ThenBy(item => item.Kind)
                .ThenBy(item => item.Person.MainName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            var rowWidth = orderedRow.Count * NodeWidth + Math.Max(0, orderedRow.Count - 1) * HorizontalGap;
            maxRowWidth = Math.Max(maxRowWidth, rowWidth);
            var x = CanvasPadding;
            var y = CanvasPadding + rowIndex * (NodeHeight + VerticalGap);

            foreach (var item in orderedRow)
            {
                var isUncertain = activeRelationships
                    .Where(relationship => relationship.PersonAId == item.Person.Id || relationship.PersonBId == item.Person.Id)
                    .Any(relationship => CreateLink(focusPerson.Id, relationship).IsUncertain);
                nodes.Add(new FamilyTreeDiagramNode(
                    item.Person.Id,
                    item.Person.MainName,
                    item.Person.PrimaryRole,
                    item.Kind,
                    item.Generation,
                    x,
                    y,
                    item.Person.Id == focusPerson.Id,
                    isUncertain));
                x += NodeWidth + HorizontalGap;
            }

            rowIndex++;
        }

        if (!HasVisibleParent(focusPerson.Id, activeRelationships, visiblePersonIds))
        {
            nodes = nodes
                .Select(node => node with { Y = node.Y + NodeHeight + VerticalGap })
                .ToList();
        }

        var parentGroups = _parentGroupBuilder.Build(focusPerson.Id, relationshipGraph, visiblePersonIds);
        var connectors = CreateFamilyConnectors(nodes, parentGroups);
        nodes = ApplyGenerationCollisionPass(nodes);
        connectors = RecalculateConnectors(connectors, nodes);
        var minX = nodes.Count == 0 ? CanvasPadding : nodes.Min(node => node.X);
        if (minX < CanvasPadding)
        {
            var shift = CanvasPadding - minX;
            nodes = nodes.Select(node => node with { X = node.X + shift }).ToList();
            connectors = connectors.Select(connector => connector with { X = connector.X + shift }).ToList();
        }

        maxRowWidth = Math.Max(maxRowWidth, nodes.Count == 0 ? 0 : nodes.Max(node => node.X) - nodes.Min(node => node.X) + NodeWidth);
        var maxY = nodes.Count == 0 ? 0 : nodes.Max(node => node.Y);
        var height = Math.Max(360, CanvasPadding + maxY + NodeHeight);
        var width = Math.Max(760, CanvasPadding * 2 + maxRowWidth);
        var centerOffset = (width - (CanvasPadding * 2 + maxRowWidth)) / 2;
        if (centerOffset > 0)
        {
            nodes = nodes
                .Select(node => node with { X = node.X + centerOffset })
                .ToList();
            connectors = connectors
                .Select(connector => connector with { X = connector.X + centerOffset })
                .ToList();
        }

        var connectorChildIds = connectors.Select(connector => connector.ChildPersonId).ToHashSet();
        var links = activeRelationships
            .Where(relationship => visiblePersonIds.Contains(relationship.PersonAId) && visiblePersonIds.Contains(relationship.PersonBId))
            .Where(relationship => !IsParentChildForConnector(relationship, connectorChildIds))
            .Where(relationship => !IsSiblingCoveredByParentGroup(relationship, parentGroups))
            .Select(CreateDiagramLink)
            .ToList();
        var connections = _connectionService.CreateConnections(nodes, links, connectors);

        return new FamilyTreeDiagram(nodes, links, connectors, connections, relationshipGraph.Issues, width, height);
    }

    private static FamilyTreeNodeKind DetermineNodeKind(Guid focusPersonId, Relationship relationship)
    {
        if (relationship.RelationshipType == RelationshipType.Spouse)
        {
            return FamilyTreeNodeKind.Partner;
        }

        if (relationship.RelationshipType == RelationshipType.Sibling)
        {
            return FamilyTreeNodeKind.Sibling;
        }

        if (!ParentGroupBuilder.IsParentLike(relationship.RelationshipType))
        {
            return FamilyTreeNodeKind.Other;
        }

        return relationship.Direction switch
        {
            RelationshipDirection.PersonAToPersonB when relationship.PersonAId == focusPersonId => FamilyTreeNodeKind.Child,
            RelationshipDirection.PersonAToPersonB when relationship.PersonBId == focusPersonId => FamilyTreeNodeKind.Parent,
            RelationshipDirection.PersonBToPersonA when relationship.PersonBId == focusPersonId => FamilyTreeNodeKind.Child,
            RelationshipDirection.PersonBToPersonA when relationship.PersonAId == focusPersonId => FamilyTreeNodeKind.Parent,
            _ => FamilyTreeNodeKind.Other
        };
    }

    private static FamilyTreeLink CreateLink(Guid focusPersonId, Relationship relationship)
    {
        var isDirectional = ParentGroupBuilder.IsParentLike(relationship.RelationshipType)
            && relationship.Direction != RelationshipDirection.Undirected;
        var isUncertain = relationship.CertaintyLevel is not CertaintyLevel.ExplicitlyMentioned and not CertaintyLevel.Likely
            || (ParentGroupBuilder.IsParentLike(relationship.RelationshipType) && relationship.Direction == RelationshipDirection.Undirected);
        var otherPersonId = relationship.PersonAId == focusPersonId
            ? relationship.PersonBId
            : relationship.PersonAId;

        return new FamilyTreeLink(
            relationship.Id,
            focusPersonId,
            otherPersonId,
            relationship.RelationshipType,
            relationship.CertaintyLevel,
            isDirectional,
            isUncertain);
    }

    private static FamilyTreeDiagramLink CreateDiagramLink(Relationship relationship)
    {
        var isDirectional = ParentGroupBuilder.IsParentLike(relationship.RelationshipType)
            && relationship.Direction != RelationshipDirection.Undirected;
        var isUncertain = relationship.CertaintyLevel is not CertaintyLevel.ExplicitlyMentioned and not CertaintyLevel.Likely
            || (ParentGroupBuilder.IsParentLike(relationship.RelationshipType) && relationship.Direction == RelationshipDirection.Undirected);
        var fromPersonId = relationship.PersonAId;
        var toPersonId = relationship.PersonBId;

        if (ParentGroupBuilder.IsParentLike(relationship.RelationshipType)
            && relationship.Direction == RelationshipDirection.PersonBToPersonA)
        {
            fromPersonId = relationship.PersonBId;
            toPersonId = relationship.PersonAId;
        }

        var linkKind = relationship.RelationshipType switch
        {
            RelationshipType.Spouse => FamilyTreeDiagramLinkKind.Partner,
            RelationshipType.Sibling => FamilyTreeDiagramLinkKind.Sibling,
            RelationshipType.ParentChild or RelationshipType.AdoptiveParent or RelationshipType.LegalParent => FamilyTreeDiagramLinkKind.ParentToFamily,
            _ => FamilyTreeDiagramLinkKind.Direct
        };

        return new FamilyTreeDiagramLink(
            relationship.Id,
            fromPersonId,
            toPersonId,
            relationship.RelationshipType,
            relationship.CertaintyLevel,
            isDirectional,
            isUncertain,
            linkKind);
    }

    private static FamilyTreeNode CreateNode(Person person, FamilyTreeNodeKind kind)
    {
        return new FamilyTreeNode(person.Id, person.MainName, person.PrimaryRole, kind);
    }

    private static IReadOnlyList<FamilyTreeDiagramConnector> CreateFamilyConnectors(
        List<FamilyTreeDiagramNode> nodes,
        IReadOnlyList<ParentGroup> parentGroups)
    {
        var connectors = new List<FamilyTreeDiagramConnector>();
        var nodesById = nodes.ToDictionary(node => node.PersonId);
        foreach (var parentGroup in parentGroups)
        {
            var childNodes = parentGroup.ChildPersonIds
                .Where(nodesById.ContainsKey)
                .Select(childId => nodesById[childId])
                .Where(node => !node.IsPlaceholder)
                .ToList();
            if (childNodes.Count == 0)
            {
                continue;
            }

            var childCenter = childNodes.Average(node => node.X + NodeWidth / 2);
            var parentY = Math.Max(CanvasPadding, childNodes.Min(node => node.Y) - NodeHeight - VerticalGap);
            var fatherX = childCenter - NodeWidth - HorizontalGap / 2;
            var motherX = childCenter + HorizontalGap / 2;
            var father = parentGroup.FatherPersonId is null || !nodesById.TryGetValue(parentGroup.FatherPersonId.Value, out var fatherNode)
                ? null
                : fatherNode;
            var mother = parentGroup.MotherPersonId is null || !nodesById.TryGetValue(parentGroup.MotherPersonId.Value, out var motherNode)
                ? null
                : motherNode;

            Guid? fatherPlaceholderId = null;
            Guid? motherPlaceholderId = null;
            if (father is null && parentGroup.HasFatherPlaceholder)
            {
                fatherPlaceholderId = CreateStablePlaceholderId(parentGroup.GroupId, FamilyTreePlaceholderKind.Father);
                nodes.Add(CreatePlaceholderNode(fatherPlaceholderId.Value, childNodes[0].PersonId, parentGroup.GroupId, FamilyTreePlaceholderKind.Father, fatherX, parentY, childNodes[0].Generation - 1));
            }
            else if (father is not null)
            {
                MoveNodeIfParentGeneration(nodes, father, fatherX, parentY, parentGroup.GroupId);
            }

            if (mother is null && parentGroup.HasMotherPlaceholder)
            {
                motherPlaceholderId = CreateStablePlaceholderId(parentGroup.GroupId, FamilyTreePlaceholderKind.Mother);
                nodes.Add(CreatePlaceholderNode(motherPlaceholderId.Value, childNodes[0].PersonId, parentGroup.GroupId, FamilyTreePlaceholderKind.Mother, motherX, parentY, childNodes[0].Generation - 1));
            }
            else if (mother is not null)
            {
                MoveNodeIfParentGeneration(nodes, mother, motherX, parentY, parentGroup.GroupId);
            }

            nodesById = nodes.ToDictionary(node => node.PersonId);
            var connectorX = GetFamilyConnectorX(
                nodesById,
                childCenter,
                parentGroup.FatherPersonId,
                parentGroup.MotherPersonId,
                fatherPlaceholderId,
                motherPlaceholderId);

            foreach (var childNode in childNodes)
            {
                connectors.Add(new FamilyTreeDiagramConnector(
                    parentGroup.GroupId,
                    childNode.PersonId,
                    parentGroup.FatherPersonId,
                    parentGroup.MotherPersonId,
                    fatherPlaceholderId,
                    motherPlaceholderId,
                    connectorX,
                    childNode.Y - VerticalGap / 2,
                    parentGroup.IsUncertain,
                    parentGroup.RelationshipIds,
                    parentGroup.CertaintyLevel));
            }
        }

        return connectors;
    }

    private static List<FamilyTreeDiagramNode> ApplyGenerationCollisionPass(IReadOnlyList<FamilyTreeDiagramNode> nodes)
    {
        var adjustedNodes = nodes.ToList();
        foreach (var generationGroup in adjustedNodes
                     .GroupBy(node => node.Generation)
                     .OrderBy(group => group.Key))
        {
            var orderedNodes = generationGroup
                .OrderBy(node => node.X)
                .ThenBy(node => node.IsPlaceholder ? 0 : 1)
                .ThenBy(node => node.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
            var nextX = orderedNodes.Count > 0 ? orderedNodes[0].X : CanvasPadding;
            foreach (var node in orderedNodes)
            {
                var targetX = Math.Max(node.X, nextX);
                MoveNode(adjustedNodes, node.PersonId, targetX, node.Y, node.FamilyGroupId);
                nextX = targetX + NodeWidth + HorizontalGap;
            }
        }

        return adjustedNodes;
    }

    private static IReadOnlyList<FamilyTreeDiagramConnector> RecalculateConnectors(
        IReadOnlyList<FamilyTreeDiagramConnector> connectors,
        IReadOnlyList<FamilyTreeDiagramNode> nodes)
    {
        var nodesById = nodes.ToDictionary(node => node.PersonId);
        return connectors
            .Select(connector =>
            {
                if (!nodesById.TryGetValue(connector.ChildPersonId, out var childNode))
                {
                    return connector;
                }

                var parentIds = new[]
                    {
                        connector.FatherPersonId,
                        connector.MotherPersonId,
                        connector.FatherPlaceholderId,
                        connector.MotherPlaceholderId
                    }
                    .Where(id => id is not null)
                    .Select(id => id!.Value)
                    .Where(nodesById.ContainsKey)
                    .ToList();
                var connectorX = parentIds.Count == 0
                    ? childNode.X + NodeWidth / 2
                    : parentIds.Average(id => nodesById[id].X + NodeWidth / 2);
                var parentBottomY = parentIds.Count == 0
                    ? childNode.Y - VerticalGap
                    : parentIds.Max(id => nodesById[id].Y + NodeHeight);
                var connectorY = parentBottomY + Math.Max(48, (childNode.Y - parentBottomY) / 2);

                return connector with
                {
                    X = connectorX,
                    Y = Math.Min(childNode.Y - 48, connectorY)
                };
            })
            .ToList();
    }

    private static IReadOnlyList<FamilyTreeConnection> CreateConnections(
        IReadOnlyList<FamilyTreeDiagramNode> nodes,
        IReadOnlyList<FamilyTreeDiagramLink> links,
        IReadOnlyList<FamilyTreeDiagramConnector> connectors)
    {
        var connections = new List<FamilyTreeConnection>();
        var nodesById = nodes.ToDictionary(node => node.PersonId);

        foreach (var connector in connectors)
        {
            if (!nodesById.TryGetValue(connector.ChildPersonId, out var childNode))
            {
                continue;
            }

            var familyPoint = new TreePoint(connector.X, connector.Y);
            var parentIds = new[]
            {
                connector.FatherPersonId,
                connector.MotherPersonId,
                connector.FatherPlaceholderId,
                connector.MotherPlaceholderId
            };

            foreach (var parentId in parentIds.Where(id => id is not null).Select(id => id!.Value))
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
                    connector.IsUncertain || parentNode.IsPlaceholder,
                    parentNode.PersonId,
                    childNode.PersonId,
                    connector.FamilyGroupId));
            }

            connections.Add(new FamilyTreeConnection(
                Guid.NewGuid(),
                FamilyTreeConnectionType.FamilyToChild,
                familyPoint,
                GetTreeEdgePoint(childNode, familyPoint),
                connector.IsUncertain,
                null,
                childNode.PersonId,
                connector.FamilyGroupId));
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
                link.FamilyGroupId));
        }

        return connections;
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
            new TreePoint(toNode.X + NodeWidth / 2, toNode.Y + NodeHeight / 2));
    }

    private static TreePoint GetTreeEdgePoint(FamilyTreeDiagramNode fromNode, TreePoint targetPoint)
    {
        var fromCenterX = fromNode.X + NodeWidth / 2;
        var fromCenterY = fromNode.Y + NodeHeight / 2;
        var deltaX = targetPoint.X - fromCenterX;
        var deltaY = targetPoint.Y - fromCenterY;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            return new TreePoint(
                deltaX >= 0 ? fromNode.X + NodeWidth : fromNode.X,
                fromCenterY);
        }

        return new TreePoint(
            fromCenterX,
            deltaY >= 0 ? fromNode.Y + NodeHeight : fromNode.Y);
    }

    private static bool HasVisibleParent(
        Guid childPersonId,
        IReadOnlyList<Relationship> relationships,
        IReadOnlySet<Guid> visiblePersonIds)
    {
        return relationships
            .Select(GetParentChildPair)
            .Any(pair => pair is not null
                && pair.Value.ChildPersonId == childPersonId
                && visiblePersonIds.Contains(pair.Value.ParentPersonId));
    }

    private static FamilyTreeDiagramNode CreatePlaceholderNode(
        Guid placeholderId,
        Guid sourcePersonId,
        Guid familyGroupId,
        FamilyTreePlaceholderKind kind,
        double x,
        double y,
        int generation)
    {
        return new FamilyTreeDiagramNode(
            placeholderId,
            kind == FamilyTreePlaceholderKind.Father ? "+ Vater" : "+ Mutter",
            string.Empty,
            FamilyTreeNodeKind.Parent,
            generation,
            x,
            y,
            false,
            true,
            true,
            kind,
            sourcePersonId,
            familyGroupId);
    }

    private static Guid CreateStablePlaceholderId(Guid parentGroupId, FamilyTreePlaceholderKind placeholderKind)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"{parentGroupId}:{placeholderKind}"));
        return new Guid(bytes);
    }

    private static void MoveNodeIfParentGeneration(
        List<FamilyTreeDiagramNode> nodes,
        FamilyTreeDiagramNode node,
        double x,
        double y,
        Guid familyGroupId)
    {
        if (node.Kind != FamilyTreeNodeKind.Parent)
        {
            return;
        }

        MoveNode(nodes, node.PersonId, x, y, familyGroupId);
    }

    private static double GetFamilyConnectorX(
        IReadOnlyDictionary<Guid, FamilyTreeDiagramNode> nodesById,
        double fallbackX,
        params Guid?[] personIds)
    {
        var parentCenters = personIds
            .Where(id => id is not null)
            .Select(id => id!.Value)
            .Where(nodesById.ContainsKey)
            .Select(id => nodesById[id].X + NodeWidth / 2)
            .ToList();

        return parentCenters.Count == 0
            ? fallbackX
            : parentCenters.Average();
    }

    private static void MoveNode(List<FamilyTreeDiagramNode> nodes, Guid personId, double x, double y, Guid? familyGroupId)
    {
        var index = nodes.FindIndex(node => node.PersonId == personId);
        if (index < 0)
        {
            return;
        }

        nodes[index] = nodes[index] with { X = x, Y = y, FamilyGroupId = familyGroupId };
    }

    private static ParentChildPair? GetParentChildPair(Relationship relationship)
    {
        if (!ParentGroupBuilder.IsParentLike(relationship.RelationshipType) || relationship.Direction == RelationshipDirection.Undirected)
        {
            return null;
        }

        var isUncertain = relationship.CertaintyLevel is not CertaintyLevel.ExplicitlyMentioned and not CertaintyLevel.Likely;
        return relationship.Direction == RelationshipDirection.PersonAToPersonB
            ? new ParentChildPair(relationship.PersonAId, relationship.PersonBId, isUncertain)
            : new ParentChildPair(relationship.PersonBId, relationship.PersonAId, isUncertain);
    }

    private static bool IsParentChildForConnector(Relationship relationship, IReadOnlySet<Guid> connectorChildIds)
    {
        var pair = GetParentChildPair(relationship);
        return pair is not null && connectorChildIds.Contains(pair.Value.ChildPersonId);
    }

    private static bool IsSiblingCoveredByParentGroup(Relationship relationship, IReadOnlyList<ParentGroup> parentGroups)
    {
        if (relationship.RelationshipType != RelationshipType.Sibling)
        {
            return false;
        }

        return parentGroups.Any(group =>
            group.ChildPersonIds.Contains(relationship.PersonAId)
            && group.ChildPersonIds.Contains(relationship.PersonBId));
    }

    private static HashSet<Guid> FindConnectedPersonIds(Guid focusPersonId, IReadOnlyList<Relationship> relationships)
    {
        var connected = new HashSet<Guid> { focusPersonId };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var relationship in relationships)
            {
                if (connected.Contains(relationship.PersonAId) && connected.Add(relationship.PersonBId))
                {
                    changed = true;
                }

                if (connected.Contains(relationship.PersonBId) && connected.Add(relationship.PersonAId))
                {
                    changed = true;
                }
            }
        }

        return connected;
    }

    private static Dictionary<Guid, int> AssignGenerations(
        Guid focusPersonId,
        IReadOnlyList<Relationship> relationships,
        IReadOnlySet<Guid> connectedPersonIds)
    {
        var generations = new Dictionary<Guid, int> { [focusPersonId] = 0 };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var relationship in relationships.Where(relationship =>
                         connectedPersonIds.Contains(relationship.PersonAId) && connectedPersonIds.Contains(relationship.PersonBId)))
            {
                var delta = GetGenerationDelta(relationship);
                if (delta is null)
                {
                    changed |= TryAssignSameGeneration(generations, relationship.PersonAId, relationship.PersonBId);
                    continue;
                }

                changed |= TryAssignGeneration(generations, relationship.PersonAId, relationship.PersonBId, delta.Value);
            }
        }

        foreach (var personId in connectedPersonIds)
        {
            generations.TryAdd(personId, 0);
        }

        return generations;
    }

    private static int? GetGenerationDelta(Relationship relationship)
    {
        if (!ParentGroupBuilder.IsParentLike(relationship.RelationshipType) || relationship.Direction == RelationshipDirection.Undirected)
        {
            return null;
        }

        return relationship.Direction == RelationshipDirection.PersonAToPersonB ? 1 : -1;
    }

    private static bool TryAssignGeneration(Dictionary<Guid, int> generations, Guid personAId, Guid personBId, int deltaFromAToB)
    {
        if (generations.TryGetValue(personAId, out var generationA) && !generations.ContainsKey(personBId))
        {
            generations[personBId] = generationA + deltaFromAToB;
            return true;
        }

        if (generations.TryGetValue(personBId, out var generationB) && !generations.ContainsKey(personAId))
        {
            generations[personAId] = generationB - deltaFromAToB;
            return true;
        }

        return false;
    }

    private static bool TryAssignSameGeneration(Dictionary<Guid, int> generations, Guid personAId, Guid personBId)
    {
        if (generations.TryGetValue(personAId, out var generationA) && !generations.ContainsKey(personBId))
        {
            generations[personBId] = generationA;
            return true;
        }

        if (generations.TryGetValue(personBId, out var generationB) && !generations.ContainsKey(personAId))
        {
            generations[personAId] = generationB;
            return true;
        }

        return false;
    }

    private static FamilyTreeNodeKind DetermineDiagramNodeKind(
        Guid focusPersonId,
        Guid personId,
        int generation,
        IReadOnlyList<Relationship> relationships)
    {
        if (personId == focusPersonId)
        {
            return FamilyTreeNodeKind.Focus;
        }

        var directRelationship = relationships.FirstOrDefault(relationship =>
            relationship.PersonAId == focusPersonId && relationship.PersonBId == personId
            || relationship.PersonAId == personId && relationship.PersonBId == focusPersonId);
        if (directRelationship is not null)
        {
            return DetermineNodeKind(focusPersonId, directRelationship);
        }

        return generation switch
        {
            < 0 => FamilyTreeNodeKind.Parent,
            > 0 => FamilyTreeNodeKind.Child,
            _ => FamilyTreeNodeKind.Other
        };
    }

    private readonly record struct ParentChildPair(Guid ParentPersonId, Guid ChildPersonId, bool IsUncertain);
}
