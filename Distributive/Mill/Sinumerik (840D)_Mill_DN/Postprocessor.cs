using System.Collections.Generic;
namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        public string ProgName;
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;

        public void OutText(string text){
            WriteLine(";" + text);
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        NCFile nc;
 
        #endregion

        public void PrintAllTools(){
            nc.OutText(" Tools list");

            SortedList<int, string> sl = new SortedList<int, string>();

            for (var i = 0; i < CLDProject.Operations.Count; i++){
                var op = CLDProject.Operations[i];
                if(op.Tool != null && op.Tool.Command != null){
                    var skip = op.Tool.Caption.IndexOf(" ") + 1;
                    sl.TryAdd(op.Tool.Number, $"{op.Tool.Caption.Substring(skip)} D{op.Tool.Command.Geom.D} L{op.Tool.Command.Geom.L}, {op.Tool.Caption}");
                }
            }

            foreach (var tl in sl){
                nc.OutText(" T" + tl.Key + " = " + tl.Value);
            }
        }

        public void PrintCS(){
            if(CLDProject.Operations.Count < 1)
                return;

            var op = CLDProject.Operations;

            double[] CSNum = new double[CLDProject.Operations.Count];

            double[] dx = new double[CLDProject.Operations.Count];
            double[] dy = new double[CLDProject.Operations.Count];
            double[] dz = new double[CLDProject.Operations.Count];
            double[] da = new double[CLDProject.Operations.Count];
            double[] db = new double[CLDProject.Operations.Count];
            double[] dc = new double[CLDProject.Operations.Count];

            var Cnt = 0; var i = 0; var j = 1; double tx, ty, tz, ta, tb, tc; int n, ok, k;
            
            while (i < op.Count) {
                while (j < op[i].CLDFile.CmdCount) {
                    var temp = op[i].CLDFile.Cmd[j].Flt;
                    if (op[i].CLDFile.Cmd[j].CmdTypeCode == 1027) {
                        n = (int)op[i].WorkpieceCSCommand.CSNumber;
                        tx = op[i].CLDFile.Cmd[j].Flt["MCS.OriginPoint.X"]; tx = Math.Round(tx, 3);
                        ty = op[i].CLDFile.Cmd[j].Flt["MCS.OriginPoint.Y"]; ty = Math.Round(ty, 3);
                        tz = op[i].CLDFile.Cmd[j].Flt["MCS.OriginPoint.Z"]; tz = Math.Round(tz, 3);
                        ta = op[i].CLDFile.Cmd[j].Flt["WCS.RotAngles.A"]; ta = Math.Round(ta, 3);
                        tb = op[i].CLDFile.Cmd[j].Flt["WCS.RotAngles.B"];
                        if(tb == -0)
                            tb = 0;
                        tc = op[i].CLDFile.Cmd[j].Flt["WCS.RotAngles.C"]; tc = Math.Round(tc, 3);
                        ok = 0;
                        k = 1;
                        while (k <= Cnt) {
                            if ((n == CSNum[k]) && (Abs(tx-dx[k]) < 0.0001) && (Abs(ty-dy[k]) < 0.0001) && (Abs(tz-dz[k]) < 0.0001) &&
                                (Abs(ta-da[k]) < 0.0001) && (Abs(tb-db[k]) < 0.0001) && (Abs(tc-dc[k]) < 0.0001))
                                {
                                    ok = 1;
                                    k = Cnt;
                                }
                            k = k + 1;
                        }
                        if (ok == 0) {
                            Cnt = Cnt + 1;
                            CSNum[Cnt] = n;
                            dx[Cnt] = tx;
                            dy[Cnt] = ty;
                            dz[Cnt] = tz;
                            da[Cnt] = ta;
                            db[Cnt] = tb;
                            dc[Cnt] = tc;
                        }
                    }
                    j = j + 1;
                }
                i = i + 1;
            }
  
            if (Cnt > 0) {
                nc.OutText(" Workpiece coordinate systems");
                for (i = 1; i < Cnt + 1; i++) {
                    nc.OutText(" G" + Str(CSNum[i]) + " = X"+ Str(dx[i]) + " Y" + Str(dy[i]) +" Z" + Str(dz[i]) +
                    " A" + Str(da[i]) + " B" + Str(db[i]) + " C" + Str(dc[i])) ;
                }
                nc.WriteLine();
            }
        }

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgName = Settings.Params.Str["OutFiles.NCProgName"];

            if(String.IsNullOrEmpty(nc.ProgName))
                nc.ProgName = "noname";

            nc.OutText($"%_N_{nc.ProgName}_MPF");
            nc.OutText($"$PATH=/_N_MPF_DIR");
            nc.WriteLine();

            nc.OutText(" Generated by SprutCAM"); 
            nc.OutText($" Date: {CurDate()}");
            nc.OutText($" Time: {CurTime()}");
            nc.WriteLine();

            PrintAllTools();
            nc.WriteLine();

            PrintCS();
            nc.WriteLine();
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.WriteLine("End of file: " + Path.GetFileName(nc.OutputFileName));
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine("; " + op.Comment);
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine();
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        // Uncomment line below (Ctrl + "/"), go to the end of "On" word and press Ctrl+Space to add a new CLData command handler
        // override On

    }
}
