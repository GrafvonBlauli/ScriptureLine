using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.App;

public static class DisplayText
{
    public static string For(RelationshipType relationshipType)
    {
        return relationshipType switch
        {
            RelationshipType.ParentChild => "Eltern-Kind",
            RelationshipType.Spouse => "Partner / Ehe",
            RelationshipType.Sibling => "Geschwister",
            RelationshipType.AdoptiveParent => "Adoptivbeziehung",
            RelationshipType.LegalParent => "rechtliche Elternschaft",
            RelationshipType.TribeMember => "Stammeszugehörigkeit",
            RelationshipType.Custom => "benutzerdefiniert",
            _ => "unbekannt verwandt"
        };
    }

    public static string For(RelationshipDirection direction)
    {
        return direction switch
        {
            RelationshipDirection.PersonAToPersonB => "aktuelle Person -> Zielperson",
            RelationshipDirection.PersonBToPersonA => "Zielperson -> aktuelle Person",
            _ => "ungerichtet"
        };
    }

    public static string For(CertaintyLevel certaintyLevel)
    {
        return certaintyLevel switch
        {
            CertaintyLevel.ExplicitlyMentioned => "ausdrücklich erwähnt",
            CertaintyLevel.Likely => "wahrscheinlich",
            CertaintyLevel.Possible => "möglich",
            CertaintyLevel.Traditional => "traditionell angenommen",
            CertaintyLevel.Disputed => "umstritten",
            CertaintyLevel.UserHypothesis => "eigene Arbeitshypothese",
            _ => "unbekannt"
        };
    }

    public static string For(RelationshipStatus status)
    {
        return status switch
        {
            RelationshipStatus.Archived => "archiviert",
            RelationshipStatus.Rejected => "verworfen",
            _ => "aktiv"
        };
    }
}
