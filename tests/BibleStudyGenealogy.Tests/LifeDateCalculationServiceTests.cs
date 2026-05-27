using BibleStudyGenealogy.Core.Models;
using BibleStudyGenealogy.Core.Services;

namespace BibleStudyGenealogy.Tests;

public sealed class LifeDateCalculationServiceTests
{
    private readonly LifeDateCalculationService _service = new();

    [Fact]
    public void CalculateDeathFromBirthAndAge_HandlesBeforeChristYears()
    {
        var birth = new DateInfo
        {
            Year = 1526,
            IsBeforeChrist = true
        };

        var result = _service.CalculateDeathFromBirthAndAge(birth, 120);

        Assert.NotNull(result);
        Assert.Equal(1406, result.Year);
        Assert.True(result.IsBeforeChrist);
        Assert.Equal(120, result.Age);
    }

    [Fact]
    public void CalculateBirthFromDeathAndAge_HandlesBeforeChristYears()
    {
        var death = new DateInfo
        {
            Year = 1406,
            IsBeforeChrist = true
        };

        var result = _service.CalculateBirthFromDeathAndAge(death, 120);

        Assert.NotNull(result);
        Assert.Equal(1526, result.Year);
        Assert.True(result.IsBeforeChrist);
    }

    [Fact]
    public void CalculateAgeAtDeath_HandlesTransitionAcrossEraBoundary()
    {
        var birth = new DateInfo
        {
            Year = 2,
            IsBeforeChrist = true
        };
        var death = new DateInfo
        {
            Year = 1,
            IsBeforeChrist = false
        };

        var result = _service.CalculateAgeAtDeath(birth, death);

        Assert.NotNull(result);
        Assert.Equal(2, result.Age);
    }
}
