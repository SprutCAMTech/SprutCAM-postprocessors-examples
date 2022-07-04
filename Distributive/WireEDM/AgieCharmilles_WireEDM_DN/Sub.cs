namespace SprutTechnology.SCPostprocessor;

public partial class Postprocessor : TPostprocessor
{
    public void InsertWire()
    {
        nc.Output("(Insert wire)");
        WireInserted = true;
    }

    public void BreakWire()
    {
        nc.Output("(Break wire)");
        WireInserted = false;
    }
}