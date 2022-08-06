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

    public void OutCyle(string ACycleID)
    {
        int n = 0;
        string sss = ACycleID;

        if (this.Prms.Count > 0) sss += "(";
        if (this.CycleGeomName != "") sss += Chr(34) + this.CycleGeomName + Chr(34);

        for (int i = 1; i <= this.Prms.Count; i++){
            if(this.Prms[i] != 99999) n = i;
        }

        for (int i = 1; i <= n; i++){
            if (i > 1 || this.CycleGeomName != "") sss += ",";
            if (this.Prms[i] != 99999) sss += Str(Math.Round(Prms[i], 3)); ; //Str(Prms[i]:3)
        }

        if (this.Prms.Count > 0) sss += ")";
        this._nc.WriteLineWithBlockN(sss); 
    }

}