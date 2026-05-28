namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class BezierConnectionBuilder
{
    public BezierConnectionPath Build(TreePoint start, TreePoint end, FamilyTreeConnectionType type = FamilyTreeConnectionType.Direct)
    {
        return type switch
        {
            FamilyTreeConnectionType.Partner or FamilyTreeConnectionType.Sibling => BuildHorizontal(start, end),
            FamilyTreeConnectionType.ParentToFamily or FamilyTreeConnectionType.FamilyToChild or FamilyTreeConnectionType.Placeholder => BuildVertical(start, end),
            _ => BuildBalanced(start, end)
        };
    }

    private static BezierConnectionPath BuildBalanced(TreePoint start, TreePoint end)
    {
        var deltaX = Math.Abs(end.X - start.X);
        var deltaY = Math.Abs(end.Y - start.Y);

        if (deltaX > deltaY)
        {
            return BuildHorizontal(start, end);
        }

        return BuildVertical(start, end);
    }

    private static BezierConnectionPath BuildHorizontal(TreePoint start, TreePoint end)
    {
        var controlX = (start.X + end.X) / 2;
        return new BezierConnectionPath(
            start,
            new TreePoint(controlX, start.Y),
            new TreePoint(controlX, end.Y),
            end);
    }

    private static BezierConnectionPath BuildVertical(TreePoint start, TreePoint end)
    {
        var controlY = (start.Y + end.Y) / 2;
        return new BezierConnectionPath(
            start,
            new TreePoint(start.X, controlY),
            new TreePoint(end.X, controlY),
            end);
    }
}
