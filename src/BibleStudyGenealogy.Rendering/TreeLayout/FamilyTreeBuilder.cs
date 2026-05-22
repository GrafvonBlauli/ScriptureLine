using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class FamilyTreeBuilder
{
    private const double NodeWidth = 150;
    private const double NodeHeight = 72;
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

        var height = Math.Max(360, CanvasPadding * 2 + rowGroups.Count * NodeHeight + Math.Max(0, rowGroups.Count - 1) * VerticalGap);
        var width = Math.Max(760, CanvasPadding * 2 + maxRowWidth);
        var centerOffset = (width - (CanvasPadding * 2 + maxRowWidth)) / 2;
        if (centerOffset > 0)
        {
            nodes = nodes
                .Select(node => node with { X = node.X + centerOffset })
                .ToList();
        }

        var links = activeRelationships
            .Where(relationship => visiblePersonIds.Contains(relationship.PersonAId) && visiblePersonIds.Contains(relationship.PersonBId))
            .Select(CreateDiagramLink)
            .ToList();

        return new FamilyTreeDiagram(nodes, links, width, height);
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

        return new FamilyTreeDiagramLink(
            relationship.Id,
            fromPersonId,
            toPersonId,
            relationship.RelationshipType,
            relationship.CertaintyLevel,
            isDirectional,
            isUncertain);
    }

    private static FamilyTreeNode CreateNode(Person person, FamilyTreeNodeKind kind)
    {
        return new FamilyTreeNode(person.Id, person.MainName, person.PrimaryRole, kind);
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
}
