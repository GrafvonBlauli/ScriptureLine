using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Core.Services;

public sealed record LifeDateCalculationResult(
    LifeDateCalculationKind Kind,
    int? Year,
    bool IsBeforeChrist,
    int? Age,
    string Message)
{
    public DateInfo ToDateInfo()
    {
        return new DateInfo
        {
            DateType = DateType.ExactYear,
            Year = Year,
            IsBeforeChrist = IsBeforeChrist,
            ApproximationText = Year is null
                ? string.Empty
                : IsBeforeChrist
                    ? $"{Year} v. Chr."
                    : Year.Value.ToString(),
            CertaintyLevel = CertaintyLevel.UserHypothesis,
            Comment = Message
        };
    }
}
