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

    public static string For(EventType eventType)
    {
        return eventType switch
        {
            EventType.Birth => "Geburt",
            EventType.Death => "Tod",
            EventType.Marriage => "Eheschließung",
            EventType.Calling => "Berufung",
            EventType.Journey => "Reise",
            EventType.Battle => "Konflikt",
            EventType.Reign => "Herrschaft",
            EventType.Prophecy => "Prophetie",
            EventType.Teaching => "Lehre",
            EventType.Miracle => "Wunder",
            _ => "sonstiges Ereignis"
        };
    }

    public static string For(PersonStatus status)
    {
        return status switch
        {
            PersonStatus.Uncertain => "unsicher",
            PersonStatus.Archived => "archiviert",
            PersonStatus.Rejected => "verworfen",
            PersonStatus.DuplicateCandidate => "mögliches Duplikat",
            _ => "aktiv"
        };
    }

    public static string For(MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Image => "Bild",
            MediaType.Pdf => "PDF",
            MediaType.Document => "Dokument",
            MediaType.Map => "Karte",
            _ => "Sonstige Datei"
        };
    }

    public static string For(Gender gender)
    {
        return gender switch
        {
            Gender.Male => "männlich",
            Gender.Female => "weiblich",
            Gender.Other => "andere Angabe",
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
