using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record RelationshipGraph(
    IReadOnlyDictionary<Guid, Person> PeopleById,
    IReadOnlyList<Relationship> ActiveRelationships,
    IReadOnlyList<RelationshipValidationIssue> Issues)
{
    public static RelationshipGraph Create(
        IReadOnlyDictionary<Guid, Person> peopleById,
        IReadOnlyList<Relationship> relationships)
    {
        var activeRelationships = relationships
            .Where(relationship => relationship.Status == RelationshipStatus.Active)
            .Where(relationship => peopleById.ContainsKey(relationship.PersonAId) && peopleById.ContainsKey(relationship.PersonBId))
            .ToList();
        var issues = activeRelationships
            .GroupBy(relationship => new
            {
                First = relationship.PersonAId.CompareTo(relationship.PersonBId) <= 0 ? relationship.PersonAId : relationship.PersonBId,
                Second = relationship.PersonAId.CompareTo(relationship.PersonBId) <= 0 ? relationship.PersonBId : relationship.PersonAId,
                relationship.RelationshipType,
                relationship.Direction
            })
            .Where(group => group.Count() > 1)
            .Select(group => new RelationshipValidationIssue(
                RelationshipValidationIssueType.DuplicateRelationship,
                "Doppelte Beziehung erkannt.",
                RelationshipId: group.Skip(1).First().Id))
            .ToList();

        return new RelationshipGraph(peopleById, activeRelationships, issues);
    }
}
