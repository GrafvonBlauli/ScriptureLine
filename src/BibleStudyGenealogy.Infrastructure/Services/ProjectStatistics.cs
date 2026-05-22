namespace BibleStudyGenealogy.Infrastructure.Services;

public sealed record ProjectStatistics(
    int PersonCount,
    int RelationshipCount,
    int EventCount,
    int BibleReferenceCount,
    int MediaFileCount,
    int PlaceCount,
    int ResearchQuestionCount);
