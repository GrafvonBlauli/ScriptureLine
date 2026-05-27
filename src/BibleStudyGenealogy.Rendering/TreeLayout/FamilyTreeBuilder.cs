using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class FamilyTreeBuilder
{
    private const double NodeWidth = 176;
    private const double NodeHeight = 92;
    private const double HorizontalGap = 34;
    private const double VerticalGap = 118;
    private const double CanvasPadding = 90;

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
        var activeRelationships = relationships
            .Where(relationship => relationship.Status == RelationshipStatus.Active)
            .Where(relationship => peopleById.ContainsKey(relationship.PersonAId) && peopleById.ContainsKey(relationship.PersonBId))
            .ToList();

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

        var connectors = CreateFamilyConnectors(focusPerson.Id, nodes, activeRelationships, visiblePersonIds, peopleById);
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
            .Select(CreateDiagramLink)
            .ToList();

        return new FamilyTreeDiagram(nodes, links, connectors, width, height);
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

        if (relationship.RelationshipType != RelationshipType.ParentChild)
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
        var isDirectional = relationship.RelationshipType == RelationshipType.ParentChild
            && relationship.Direction != RelationshipDirection.Undirected;
        var isUncertain = relationship.CertaintyLevel is not CertaintyLevel.ExplicitlyMentioned and not CertaintyLevel.Likely
            || (relationship.RelationshipType == RelationshipType.ParentChild && relationship.Direction == RelationshipDirection.Undirected);
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
        var isDirectional = relationship.RelationshipType == RelationshipType.ParentChild
            && relationship.Direction != RelationshipDirection.Undirected;
        var isUncertain = relationship.CertaintyLevel is not CertaintyLevel.ExplicitlyMentioned and not CertaintyLevel.Likely
            || (relationship.RelationshipType == RelationshipType.ParentChild && relationship.Direction == RelationshipDirection.Undirected);
        var fromPersonId = relationship.PersonAId;
        var toPersonId = relationship.PersonBId;

        if (relationship.RelationshipType == RelationshipType.ParentChild
            && relationship.Direction == RelationshipDirection.PersonBToPersonA)
        {
            fromPersonId = relationship.PersonBId;
            toPersonId = relationship.PersonAId;
        }

        var linkKind = relationship.RelationshipType switch
        {
            RelationshipType.Spouse => FamilyTreeDiagramLinkKind.Partner,
            RelationshipType.Sibling => FamilyTreeDiagramLinkKind.Sibling,
            RelationshipType.ParentChild => FamilyTreeDiagramLinkKind.ParentToFamily,
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
        Guid focusPersonId,
        List<FamilyTreeDiagramNode> nodes,
        IReadOnlyList<Relationship> relationships,
        IReadOnlySet<Guid> visiblePersonIds,
        IReadOnlyDictionary<Guid, Person> peopleById)
    {
        var connectors = new List<FamilyTreeDiagramConnector>();
        var nodesById = nodes.ToDictionary(node => node.PersonId);
        foreach (var childNode in nodes.Where(node => !node.IsPlaceholder && node.Generation >= 0).ToList())
        {
            var parentRelationships = relationships
                .Select(GetParentChildPair)
                .Where(pair => pair is not null && pair.Value.ChildPersonId == childNode.PersonId)
                .Select(pair => pair!.Value)
                .Where(pair => visiblePersonIds.Contains(pair.ParentPersonId))
                .ToList();
            var hasFocusParents = childNode.PersonId == focusPersonId;
            if (parentRelationships.Count == 0 && !hasFocusParents)
            {
                continue;
            }

            var father = parentRelationships
                .Select(pair => nodesById.GetValueOrDefault(pair.ParentPersonId))
                .FirstOrDefault(node => node is not null && peopleById.TryGetValue(node.PersonId, out var person) && person.Gender == Gender.Male);
            var mother = parentRelationships
                .Select(pair => nodesById.GetValueOrDefault(pair.ParentPersonId))
                .FirstOrDefault(node => node is not null && peopleById.TryGetValue(node.PersonId, out var person) && person.Gender == Gender.Female);
            var unknownParents = parentRelationships
                .Select(pair => nodesById.GetValueOrDefault(pair.ParentPersonId))
                .Where(node => node is not null && node.Kind == FamilyTreeNodeKind.Parent && node != father && node != mother)
                .ToList();
            father ??= unknownParents.FirstOrDefault();
            mother ??= unknownParents.Skip(father is null ? 0 : 1).FirstOrDefault();

            var familyGroupId = Guid.NewGuid();
            var childCenter = childNode.X + NodeWidth / 2;
            var parentY = Math.Max(CanvasPadding, childNode.Y - NodeHeight - VerticalGap);
            var fatherX = childCenter - NodeWidth - HorizontalGap / 2;
            var motherX = childCenter + HorizontalGap / 2;

            Guid? fatherPlaceholderId = null;
            Guid? motherPlaceholderId = null;
            if (father is null && hasFocusParents)
            {
                fatherPlaceholderId = Guid.NewGuid();
                nodes.Add(CreatePlaceholderNode(fatherPlaceholderId.Value, childNode.PersonId, familyGroupId, FamilyTreePlaceholderKind.Father, fatherX, parentY, childNode.Generation - 1));
            }
            else if (father is not null)
            {
                MoveNode(nodes, father.PersonId, fatherX, parentY, familyGroupId);
            }

            if (mother is null && hasFocusParents)
            {
                motherPlaceholderId = Guid.NewGuid();
                nodes.Add(CreatePlaceholderNode(motherPlaceholderId.Value, childNode.PersonId, familyGroupId, FamilyTreePlaceholderKind.Mother, motherX, parentY, childNode.Generation - 1));
            }
            else if (mother is not null)
            {
                MoveNode(nodes, mother.PersonId, motherX, parentY, familyGroupId);
            }

            connectors.Add(new FamilyTreeDiagramConnector(
                familyGroupId,
                childNode.PersonId,
                father?.PersonId,
                mother?.PersonId,
                fatherPlaceholderId,
                motherPlaceholderId,
                childCenter,
                childNode.Y - VerticalGap / 2,
                parentRelationships.Any(pair => pair.IsUncertain)));
        }

        return connectors;
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

    private static void MoveNode(List<FamilyTreeDiagramNode> nodes, Guid personId, double x, double y, Guid familyGroupId)
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
        if (relationship.RelationshipType != RelationshipType.ParentChild || relationship.Direction == RelationshipDirection.Undirected)
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
        if (relationship.RelationshipType != RelationshipType.ParentChild || relationship.Direction == RelationshipDirection.Undirected)
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
