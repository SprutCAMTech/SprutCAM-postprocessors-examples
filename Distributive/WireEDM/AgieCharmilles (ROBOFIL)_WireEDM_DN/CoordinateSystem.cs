namespace SprutTechnology.SCPostprocessor;

public class CoordinateSystem
{
    /// <summary>
    /// Displacement of the local coordinate system on the global coordinate system
    /// </summary>
    public T2DPoint Displacement { get; set; }

    public bool SetDefined { get; set; }

    public CoordinateSystem(T2DPoint displacement, bool setDefined)
    {
        Displacement = displacement;
        SetDefined = setDefined;
    }

    public CoordinateSystem()
    {
        Displacement = T2DPoint.Zero;
        SetDefined = false;
    }
}
