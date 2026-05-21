namespace BibleStudyGenealogy.Core.Models;

public enum DateType
{
    Unknown,
    ExactDate,
    ExactYear,
    ApproximateYear,
    YearRange,
    BeforeYear,
    AfterYear,
    BetweenEvents,
    RelativeToEvent,
    TextOnly
}
