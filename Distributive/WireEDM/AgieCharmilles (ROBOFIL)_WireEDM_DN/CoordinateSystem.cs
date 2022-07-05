namespace SprutTechnology.SCPostprocessor;

public class CoordinateSystem
{
    /// <summary>
    /// Displacement of the local coordinate system on the global coordinate system
    /// </summary>
    public T2DPoint Displacement { get; set; }

    public CoordinateSystemSet Set { get; set; }

    public CoordinateSystem(T2DPoint displacement, CoordinateSystemSet kind)
    {
        Displacement = displacement;
        Set = kind;
    }

    public CoordinateSystem()
    {
        Displacement = T2DPoint.Zero;
        Set = CoordinateSystemSet.NotDefined;
    }
}

public enum CoordinateSystemSet
{
    NotDefined = 0,
    Cylindrical = 1,
    Conical = 2
}