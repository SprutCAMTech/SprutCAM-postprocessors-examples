namespace SprutTechnology.SCPostprocessor;

public class CycleState
{
    public int KodCycle;
    public int CycleNumber; 
    public int CyclePlane; 

    ///<summary>Индекс подпрограммы-контура для токарных циклов</summary>
    public int ContourN; 
    
    public string CycleName; 
    public string CycleGeomName; 
    
    ///<summary>G81-G89; true - if cycle is on</summary>
    public bool CycleOn; 
    public bool IsFirstCycle; 

    ///<summary>Является ли подпрограмма геометрией токарного цикла</summary>
    public bool IsCycleGeometry; 
}

public class CycleSinumerik840D
{
    public CycleState State;
    public InpArray<double> Prms; //Prms[0] always = 0; default list size is 4 items

    NCFile _nc;
    Postprocessor _post;

    public CycleSinumerik840D(Postprocessor post, NCFile nc)
    {
        State = new CycleState();
        Prms = new InpArray<double>(); 
        _post = post;
        _nc = nc;
    }

    public bool IsFirstCycle => State.IsFirstCycle;
    public bool CycleOn => State.CycleOn;
    public bool IsCycleGeometry => State.IsCycleGeometry;
    public int KodCycle => State.KodCycle;
    public int CycleNumber => State.CycleNumber;
    public int CyclePlane => State.CyclePlane;
    public int ContourN => State.ContourN;
    public string CycleName => State.CycleName; 
    public string CycleGeomName => State.CycleGeomName;

    public void OnCycle() => State.CycleOn = true; 
    public void OffCycle() => State.CycleOn = false;

    public void OnCycleGeometry() => State.IsCycleGeometry = true; 
    public void OffCycleGeometry() => State.IsCycleGeometry = false;
}