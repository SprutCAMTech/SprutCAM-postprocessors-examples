namespace SprutTechnology.SCPostprocessor;

public static class Extensions
{
    public static T2DPoint To2DPoint(this T3DPoint point)
    {
        return new(point.X, point.Y);
    }

    public static T2DPoint To2DPoint(this TInp3DPoint point)
    {
        return To2DPoint((T3DPoint)point);
    }

    public static void Increase(this CountingNCWord word)
    {
        word.v += word.AutoIncrementStep;
    }
}