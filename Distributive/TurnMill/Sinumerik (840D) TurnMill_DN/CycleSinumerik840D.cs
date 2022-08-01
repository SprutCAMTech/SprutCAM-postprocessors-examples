namespace SprutTechnology.SCPostprocessor;

public class CycleState
{
    public int KodCycle;
    public int CycleNumber; 
    public int CyclePlane; 
    
    public string CycleName; 
    public string CycleGeomName; 
    
    ///<summary>G81-G89 cycle is on; true - if cycle is on</summary>
    public bool CycleOn; 
    public bool IsFirstCycle; 
}

public class CycleSinumerik840D
{
    public CycleState State;
    public InpArray<double> Prms;

    NCFile _nc;
    Postprocessor _post;

    public CycleSinumerik840D(Postprocessor post, NCFile nc)
    {
        State = new CycleState();
        Prms = new InpArray<double>();
        _post = post;
        _nc = nc;
    }

    public void OffCycle() => State.CycleOn = false;
    public void OnCycle() => State.CycleOn = true;

    public bool IsFirstCycle => State.IsFirstCycle;
    public int KodCycle => State.KodCycle;
    public int CycleNumber => State.CycleNumber;
    public int CyclePlane => State.CyclePlane;
}