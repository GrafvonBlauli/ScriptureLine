using BibleStudyGenealogy.Core.Models;

namespace BibleStudyGenealogy.Core.Services;

public sealed class LifeDateCalculationService
{
    public LifeDateCalculationResult? CalculateDeathFromBirthAndAge(DateInfo? birthDateInfo, int? ageAtDeath)
    {
        if (!TryGetAstronomicalYear(birthDateInfo, out var birthYear) || ageAtDeath is null || ageAtDeath < 0)
        {
            return null;
        }

        var deathYear = birthYear + ageAtDeath.Value;
        var (year, isBeforeChrist) = FromAstronomicalYear(deathYear);
        return new LifeDateCalculationResult(
            LifeDateCalculationKind.DeathFromBirthAndAge,
            year,
            isBeforeChrist,
            ageAtDeath,
            "Aus Geburtsjahr und Alter berechnet.");
    }

    public LifeDateCalculationResult? CalculateBirthFromDeathAndAge(DateInfo? deathDateInfo, int? ageAtDeath)
    {
        if (!TryGetAstronomicalYear(deathDateInfo, out var deathYear) || ageAtDeath is null || ageAtDeath < 0)
        {
            return null;
        }

        var birthYear = deathYear - ageAtDeath.Value;
        var (year, isBeforeChrist) = FromAstronomicalYear(birthYear);
        return new LifeDateCalculationResult(
            LifeDateCalculationKind.BirthFromDeathAndAge,
            year,
            isBeforeChrist,
            ageAtDeath,
            "Aus Sterbejahr und Alter berechnet.");
    }

    public LifeDateCalculationResult? CalculateAgeAtDeath(DateInfo? birthDateInfo, DateInfo? deathDateInfo)
    {
        if (!TryGetAstronomicalYear(birthDateInfo, out var birthYear)
            || !TryGetAstronomicalYear(deathDateInfo, out var deathYear))
        {
            return null;
        }

        var age = deathYear - birthYear;
        if (age < 0)
        {
            return null;
        }

        return new LifeDateCalculationResult(
            LifeDateCalculationKind.AgeFromBirthAndDeath,
            null,
            false,
            age,
            "Aus Geburts- und Sterbejahr berechnet.");
    }

    private static bool TryGetAstronomicalYear(DateInfo? dateInfo, out int year)
    {
        year = 0;
        if (dateInfo?.Year is null)
        {
            return false;
        }

        year = dateInfo.IsBeforeChrist
            ? 1 - dateInfo.Year.Value
            : dateInfo.Year.Value;
        return true;
    }

    private static (int Year, bool IsBeforeChrist) FromAstronomicalYear(int astronomicalYear)
    {
        return astronomicalYear <= 0
            ? (1 - astronomicalYear, true)
            : (astronomicalYear, false);
    }
}
