namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record FamilyTreeLayoutOptions(
    int GenerationLimit,
    bool ShowAllConnected)
{
    public static FamilyTreeLayoutOptions Default { get; } = new(2, false);
}
