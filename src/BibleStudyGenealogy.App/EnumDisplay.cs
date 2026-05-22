namespace BibleStudyGenealogy.App;

internal sealed record EnumDisplay<T>(T Value, string DisplayName)
    where T : struct, Enum
{
    public override string ToString()
    {
        return DisplayName;
    }
}
