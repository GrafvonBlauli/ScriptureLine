namespace BibleStudyGenealogy.Core.Models;

public sealed class DateInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateType DateType { get; set; } = DateType.Unknown;

    public DateOnly? ExactDate { get; set; }

    public int? Year { get; set; }

    public int? YearFrom { get; set; }

    public int? YearTo { get; set; }

    public string ApproximationText { get; set; } = string.Empty;

    public bool IsBeforeChrist { get; set; }

    public CertaintyLevel CertaintyLevel { get; set; } = CertaintyLevel.Unknown;

    public Guid? ChronologyModelId { get; set; }

    public string Comment { get; set; } = string.Empty;
}
