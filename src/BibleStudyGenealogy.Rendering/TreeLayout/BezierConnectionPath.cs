namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed record BezierConnectionPath(TreePoint Start, TreePoint Control1, TreePoint Control2, TreePoint End)
{
    public string ToInvariantPathData()
    {
        return FormattableString.Invariant(
            $"M {Start.X:0.###},{Start.Y:0.###} C {Control1.X:0.###},{Control1.Y:0.###} {Control2.X:0.###},{Control2.Y:0.###} {End.X:0.###},{End.Y:0.###}");
    }
}
