using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.App;

internal static class DisplayOptions
{
    public static IReadOnlyList<EnumDisplay<Gender>> Genders()
    {
        return
        [
            new(Gender.Unknown, DisplayText.For(Gender.Unknown)),
            new(Gender.Male, DisplayText.For(Gender.Male)),
            new(Gender.Female, DisplayText.For(Gender.Female)),
            new(Gender.Other, DisplayText.For(Gender.Other))
        ];
    }

    public static IReadOnlyList<EnumDisplay<PersonStatus>> PersonStatuses()
    {
        return
        [
            new(PersonStatus.Active, DisplayText.For(PersonStatus.Active)),
            new(PersonStatus.Uncertain, DisplayText.For(PersonStatus.Uncertain)),
            new(PersonStatus.Archived, DisplayText.For(PersonStatus.Archived)),
            new(PersonStatus.Rejected, DisplayText.For(PersonStatus.Rejected)),
            new(PersonStatus.DuplicateCandidate, DisplayText.For(PersonStatus.DuplicateCandidate))
        ];
    }

    public static IReadOnlyList<EnumDisplay<RelationshipType>> RelationshipTypes()
    {
        return
        [
            new(RelationshipType.ParentChild, DisplayText.For(RelationshipType.ParentChild)),
            new(RelationshipType.Spouse, DisplayText.For(RelationshipType.Spouse)),
            new(RelationshipType.Sibling, DisplayText.For(RelationshipType.Sibling)),
            new(RelationshipType.AdoptiveParent, DisplayText.For(RelationshipType.AdoptiveParent)),
            new(RelationshipType.LegalParent, DisplayText.For(RelationshipType.LegalParent)),
            new(RelationshipType.TribeMember, DisplayText.For(RelationshipType.TribeMember)),
            new(RelationshipType.UnknownRelated, DisplayText.For(RelationshipType.UnknownRelated)),
            new(RelationshipType.Custom, DisplayText.For(RelationshipType.Custom))
        ];
    }

    public static IReadOnlyList<EnumDisplay<RelationshipDirection>> RelationshipDirections()
    {
        return
        [
            new(RelationshipDirection.Undirected, DisplayText.For(RelationshipDirection.Undirected)),
            new(RelationshipDirection.PersonAToPersonB, DisplayText.For(RelationshipDirection.PersonAToPersonB)),
            new(RelationshipDirection.PersonBToPersonA, DisplayText.For(RelationshipDirection.PersonBToPersonA))
        ];
    }

    public static IReadOnlyList<EnumDisplay<CertaintyLevel>> CertaintyLevels()
    {
        return
        [
            new(CertaintyLevel.ExplicitlyMentioned, DisplayText.For(CertaintyLevel.ExplicitlyMentioned)),
            new(CertaintyLevel.Likely, DisplayText.For(CertaintyLevel.Likely)),
            new(CertaintyLevel.Possible, DisplayText.For(CertaintyLevel.Possible)),
            new(CertaintyLevel.Traditional, DisplayText.For(CertaintyLevel.Traditional)),
            new(CertaintyLevel.Disputed, DisplayText.For(CertaintyLevel.Disputed)),
            new(CertaintyLevel.UserHypothesis, DisplayText.For(CertaintyLevel.UserHypothesis)),
            new(CertaintyLevel.Unknown, DisplayText.For(CertaintyLevel.Unknown))
        ];
    }

    public static IReadOnlyList<EnumDisplay<EventType>> EventTypes()
    {
        return
        [
            new(EventType.Birth, DisplayText.For(EventType.Birth)),
            new(EventType.Death, DisplayText.For(EventType.Death)),
            new(EventType.Marriage, DisplayText.For(EventType.Marriage)),
            new(EventType.Calling, DisplayText.For(EventType.Calling)),
            new(EventType.Journey, DisplayText.For(EventType.Journey)),
            new(EventType.Battle, DisplayText.For(EventType.Battle)),
            new(EventType.Reign, DisplayText.For(EventType.Reign)),
            new(EventType.Prophecy, DisplayText.For(EventType.Prophecy)),
            new(EventType.Teaching, DisplayText.For(EventType.Teaching)),
            new(EventType.Miracle, DisplayText.For(EventType.Miracle)),
            new(EventType.Other, DisplayText.For(EventType.Other))
        ];
    }
}
