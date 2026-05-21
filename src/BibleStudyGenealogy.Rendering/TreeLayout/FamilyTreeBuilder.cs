using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class FamilyTreeBuilder
{
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

    private static FamilyTreeNodeKind DetermineNodeKind(Guid focusPersonId, Relationship relationship)
    {
        if (relationship.RelationshipType == RelationshipType.Spouse)
        {
            return FamilyTreeNodeKind.Partner;
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

    private static FamilyTreeNode CreateNode(Person person, FamilyTreeNodeKind kind)
    {
        return new FamilyTreeNode(person.Id, person.MainName, person.PrimaryRole, kind);
    }
}
