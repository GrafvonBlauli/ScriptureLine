namespace BibleStudyGenealogy.Rendering.TreeLayout;

public sealed class BezierConnectionBuilder
{
    public BezierConnectionPath Build(TreePoint start, TreePoint end)
    {
        var deltaX = Math.Abs(end.X - start.X);
        var deltaY = Math.Abs(end.Y - start.Y);

        if (deltaX > deltaY)
        {
            var controlX = (start.X + end.X) / 2;
            return new BezierConnectionPath(
                start,
                new TreePoint(controlX, start.Y),
                new TreePoint(controlX, end.Y),
                end);
        }

        var controlY = (start.Y + end.Y) / 2;
        return new BezierConnectionPath(
            start,
            new TreePoint(start.X, controlY),
            new TreePoint(end.X, controlY),
            end);
    }
}
