using System.Security.Cryptography;
using System.Text;
using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class ParentGroupBuilder
{
    public IReadOnlyList<ParentGroup> Build(
        Guid focusPersonId,
        RelationshipGraph graph,
        IReadOnlySet<Guid> visiblePersonIds)
    {
        var parentPairs = graph.ActiveRelationships
            .Select(GetParentChildPair)
            .Where(pair => pair is not null)
            .Select(pair => pair!.Value)
            .Where(pair => visiblePersonIds.Contains(pair.ParentPersonId) && visiblePersonIds.Contains(pair.ChildPersonId))
            .ToList();
        var pairsByChild = parentPairs
            .GroupBy(pair => pair.ChildPersonId)
            .ToDictionary(group => group.Key, group => group.ToList());
        if (!pairsByChild.ContainsKey(focusPersonId))
        {
            pairsByChild[focusPersonId] = new List<ParentChildPair>();
        }

        var groups = new Dictionary<string, MutableParentGroup>(StringComparer.Ordinal);
        foreach (var (childPersonId, pairs) in pairsByChild)
        {
            var parentIds = pairs.Select(pair => pair.ParentPersonId).Distinct().ToList();
            var fatherId = parentIds.FirstOrDefault(parentId =>
                graph.PeopleById.TryGetValue(parentId, out var person) && person.Gender == Gender.Male);
            var motherId = parentIds.FirstOrDefault(parentId =>
                graph.PeopleById.TryGetValue(parentId, out var person) && person.Gender == Gender.Female);
            Guid? fatherPersonId = fatherId == Guid.Empty ? null : fatherId;
            Guid? motherPersonId = motherId == Guid.Empty ? null : motherId;
            var additionalParentIds = parentIds
                .Where(parentId => parentId != fatherPersonId && parentId != motherPersonId)
                .OrderBy(parentId => parentId)
                .ToList();
            if (fatherPersonId is null && additionalParentIds.Count > 0)
            {
                fatherPersonId = additionalParentIds[0];
                additionalParentIds.RemoveAt(0);
            }

            if (motherPersonId is null && additionalParentIds.Count > 0)
            {
                motherPersonId = additionalParentIds[0];
                additionalParentIds.RemoveAt(0);
            }

            var hasFatherPlaceholder = childPersonId == focusPersonId && fatherPersonId is null;
            var hasMotherPlaceholder = childPersonId == focusPersonId && motherPersonId is null;
            var key = CreateKey(fatherPersonId, motherPersonId, additionalParentIds, hasFatherPlaceholder, hasMotherPlaceholder);
            if (!groups.TryGetValue(key, out var group))
            {
                var parentRelationshipIds = pairs.Select(pair => pair.RelationshipId).Distinct().ToList();
                group = new MutableParentGroup(
                    CreateStableGuid(key),
                    key,
                    fatherPersonId,
                    motherPersonId,
                    additionalParentIds,
                    hasFatherPlaceholder,
                    hasMotherPlaceholder,
                    parentRelationshipIds,
                    AggregateCertainty(pairs.Select(pair => pair.CertaintyLevel)),
                    pairs.Any(pair => pair.IsUncertain),
                    FindPartnerRelationshipId(graph.ActiveRelationships, fatherPersonId, motherPersonId));
                groups[key] = group;
            }

            group.ChildPersonIds.Add(childPersonId);
            foreach (var relationshipId in pairs.Select(pair => pair.RelationshipId))
            {
                if (!group.RelationshipIds.Contains(relationshipId))
                {
                    group.RelationshipIds.Add(relationshipId);
                }
            }

            group.CertaintyLevel = AggregateCertainty(group.RelationshipIds
                .Select(id => graph.ActiveRelationships.First(relationship => relationship.Id == id).CertaintyLevel));
            group.IsUncertain |= pairs.Any(pair => pair.IsUncertain);
        }

        return groups.Values
            .Select(group => new ParentGroup(
                group.GroupId,
                group.Key,
                group.FatherPersonId,
                group.MotherPersonId,
                group.AdditionalParentIds,
                group.HasFatherPlaceholder,
                group.HasMotherPlaceholder,
                group.ChildPersonIds.OrderBy(childId => graph.PeopleById.TryGetValue(childId, out var person) ? person.MainName : string.Empty, StringComparer.CurrentCultureIgnoreCase).ToList(),
                group.RelationshipIds,
                group.CertaintyLevel,
                group.IsUncertain,
                group.PartnerRelationshipId))
            .ToList();
    }

    public static bool IsParentLike(RelationshipType relationshipType)
    {
        return relationshipType is RelationshipType.ParentChild or RelationshipType.AdoptiveParent or RelationshipType.LegalParent;
    }

    private static ParentChildPair? GetParentChildPair(Relationship relationship)
    {
        if (!IsParentLike(relationship.RelationshipType) || relationship.Direction == RelationshipDirection.Undirected)
        {
            return null;
        }

        var isUncertain = relationship.CertaintyLevel is not CertaintyLevel.ExplicitlyMentioned and not CertaintyLevel.Likely;
        return relationship.Direction == RelationshipDirection.PersonAToPersonB
            ? new ParentChildPair(relationship.Id, relationship.PersonAId, relationship.PersonBId, relationship.CertaintyLevel, isUncertain)
            : new ParentChildPair(relationship.Id, relationship.PersonBId, relationship.PersonAId, relationship.CertaintyLevel, isUncertain);
    }

    private static Guid? FindPartnerRelationshipId(IReadOnlyList<Relationship> relationships, Guid? firstParentId, Guid? secondParentId)
    {
        if (firstParentId is null || secondParentId is null)
        {
            return null;
        }

        return relationships.FirstOrDefault(relationship =>
            relationship.RelationshipType == RelationshipType.Spouse
            && (relationship.PersonAId == firstParentId && relationship.PersonBId == secondParentId
                || relationship.PersonAId == secondParentId && relationship.PersonBId == firstParentId))?.Id;
    }

    private static string CreateKey(Guid? fatherPersonId, Guid? motherPersonId, IReadOnlyList<Guid> additionalParentIds, bool hasFatherPlaceholder, bool hasMotherPlaceholder)
    {
        var parts = new List<string>();
        if (fatherPersonId is not null)
        {
            parts.Add($"P:{fatherPersonId}");
        }

        if (motherPersonId is not null)
        {
            parts.Add($"P:{motherPersonId}");
        }

        parts.AddRange(additionalParentIds.Select(parentId => $"P:{parentId}"));
        if (hasFatherPlaceholder)
        {
            parts.Add("PH:Father");
        }

        if (hasMotherPlaceholder)
        {
            parts.Add("PH:Mother");
        }

        return string.Join("|", parts.Order(StringComparer.Ordinal));
    }

    private static CertaintyLevel AggregateCertainty(IEnumerable<CertaintyLevel> certaintyLevels)
    {
        return certaintyLevels
            .DefaultIfEmpty(CertaintyLevel.Unknown)
            .OrderBy(GetCertaintySeverity)
            .Last();
    }

    private static int GetCertaintySeverity(CertaintyLevel certaintyLevel)
    {
        return certaintyLevel switch
        {
            CertaintyLevel.ExplicitlyMentioned => 0,
            CertaintyLevel.Likely => 1,
            CertaintyLevel.Possible => 2,
            CertaintyLevel.Traditional => 3,
            CertaintyLevel.Disputed => 4,
            CertaintyLevel.UserHypothesis => 5,
            _ => 6
        };
    }

    private static Guid CreateStableGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private readonly record struct ParentChildPair(
        Guid RelationshipId,
        Guid ParentPersonId,
        Guid ChildPersonId,
        CertaintyLevel CertaintyLevel,
        bool IsUncertain);

    private sealed class MutableParentGroup
    {
        public MutableParentGroup(
            Guid groupId,
            string key,
            Guid? fatherPersonId,
            Guid? motherPersonId,
            IReadOnlyList<Guid> additionalParentIds,
            bool hasFatherPlaceholder,
            bool hasMotherPlaceholder,
            IReadOnlyList<Guid> relationshipIds,
            CertaintyLevel certaintyLevel,
            bool isUncertain,
            Guid? partnerRelationshipId)
        {
            GroupId = groupId;
            Key = key;
            FatherPersonId = fatherPersonId;
            MotherPersonId = motherPersonId;
            AdditionalParentIds = additionalParentIds;
            HasFatherPlaceholder = hasFatherPlaceholder;
            HasMotherPlaceholder = hasMotherPlaceholder;
            RelationshipIds = relationshipIds.ToList();
            CertaintyLevel = certaintyLevel;
            IsUncertain = isUncertain;
            PartnerRelationshipId = partnerRelationshipId;
        }

        public Guid GroupId { get; }

        public string Key { get; }

        public Guid? FatherPersonId { get; }

        public Guid? MotherPersonId { get; }

        public IReadOnlyList<Guid> AdditionalParentIds { get; }

        public bool HasFatherPlaceholder { get; }

        public bool HasMotherPlaceholder { get; }

        public List<Guid> ChildPersonIds { get; } = new();

        public List<Guid> RelationshipIds { get; }

        public CertaintyLevel CertaintyLevel { get; set; }

        public bool IsUncertain { get; set; }

        public Guid? PartnerRelationshipId { get; }
    }
}
