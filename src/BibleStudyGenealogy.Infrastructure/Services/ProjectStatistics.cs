namespace BibleStudyGenealogy.Infrastructure.Services;

public sealed record ProjectStatistics(
    int PersonCount,
    int RelationshipCount,
    int PlaceCount,
    int ResearchQuestionCount);
