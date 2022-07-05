namespace SprutTechnology.SCPostprocessor;

public partial class Postprocessor : TPostprocessor
{
    private void InsertWire()
    {
        nc.Output("(Insert wire)");
        wireInserted = true;
    }

    private void BreakWire()
    {
        nc.Output("(Break wire)");
        wireInserted = false;
    }
}