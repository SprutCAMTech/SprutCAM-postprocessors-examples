using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        public string ProgName;
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;

        public void OutText(string text){
            Text.Show(text);
            Block.Out();
        }

        public void OutHeaderText(string text){
            WriteLine(";" + text);
        }

        public void ClearAxis(){
            X.v0 = double.NaN;
            Y.v0 = double.NaN;
            Z.v0 = double.NaN;
            A.v0 = double.NaN;
            B.v0 = double.NaN;
            C.v0 = double.NaN;
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        NCFile nc;

        double Plane_;
 
        #endregion

        #region Extentions

        public void ConvertPlaneNumber(int innerN, out double outerN){
            switch(innerN){
                case 33: 
                    outerN = 17;
                    break;
                case 41: 
                    outerN = 18;
                    break;
                case 37: 
                    outerN = 19;
                    break;
                case 133: 
                    outerN = -17;
                    break;
                case 141: 
                    outerN = -18;
                    break;
                case 137: 
                    outerN = -19;
                    break;  
                default: 
                    throw new ArgumentException();
            }
        }

        #endregion 

        public void PrintAllTools(){
            nc.OutHeaderText(" Tools list");

            SortedList<int, string> sl = new SortedList<int, string>();

            int firstCapitalIndex = -1;
            string currentTool = "", previousTool = "";

            for (var i = 0; i < CLDProject.Operations.Count; i++){
                var op = CLDProject.Operations[i];
                if(op.Tool != null && op.Tool.Command != null){
                    var t = 0;
                    while(!string.IsNullOrEmpty(op.Tool.Caption) && !char.IsLetter(op.Tool.Caption[t + 1])){
                        t++;
                        var objectsWithCharAndIndex = op.Tool.Caption.Substring(t).Select((c,i)=>new {Char = c,Index = i});
                        firstCapitalIndex = objectsWithCharAndIndex.First(o => Char.IsUpper(o.Char)).Index;
                        t += firstCapitalIndex;
                    }
                    var skip = t;
                    previousTool = currentTool;
                    currentTool = op.Tool.Caption.Substring(skip);
                    var text = string.IsNullOrEmpty(op.Tool.Caption) ? $"{Transliterate(previousTool) } D{op.Tool.Command.Geom.D} L{op.Tool.Command.Geom.L}" 
                        : $"{Transliterate(currentTool)} D{op.Tool.Command.Geom.D} L{op.Tool.Command.Geom.L}, {op.Tool.Caption}";
                    sl.TryAdd(op.Tool.Number, text);
                }
            }

            foreach (var tl in sl){
                nc.OutHeaderText(" T" + tl.Key + " = " + tl.Value);
            }
        }

        public void PrintCS(ICLDProject prj){
            var CLDFiles = prj.CLDFiles;

            double[] CSNum = new double[CLDProject.Operations.Count];

            double[] dx = new double[CLDProject.Operations.Count]; double[] dy = new double[CLDProject.Operations.Count]; 
            double[] dz = new double[CLDProject.Operations.Count]; double[] da = new double[CLDProject.Operations.Count];
            double[] db = new double[CLDProject.Operations.Count]; double[] dc = new double[CLDProject.Operations.Count];

            double tx, ty, tz, ta, tb, tc; 
            int n, ok, k, Cnt = 0, i = 0, j = 0;
            
            while (i < CLDFiles.FileCount) {
                j = 1;
                while (j < CLDFiles[i].CmdCount) {
                    var temp = CLDFiles[i].Cmd[j];
                    if (CLDFiles[i].Cmd[j].CmdTypeCode == 1027) 
                        if (CLDFiles[i].Cmd[j].CLD[4] == 0){
                            n = CLDFiles[i].Cmd[j].CLD[5];
                            tx = temp.Flt["MCS.OriginPoint.X"]; tx = Math.Round(tx, 3);
                            ty = temp.Flt["MCS.OriginPoint.Y"]; ty = Math.Round(ty, 3);
                            tz = temp.Flt["MCS.OriginPoint.Z"]; tz = Math.Round(tz, 3);
                            ta = temp.Flt["WCS.RotAngles.A"]; ta = Math.Round(ta, 3);
                            tb = temp.Flt["WCS.RotAngles.B"]; tb = Math.Round(tb, 3);
                            if(tb == -0)
                                tb = 0;
                            tc = temp.Flt["WCS.RotAngles.C"]; tc = Math.Round(tc, 3);
                            ok = 0; k = 1;
                            while (k <= Cnt) {
                                if ((n == CSNum[k]) && (Abs(tx-dx[k]) < 0.0001) && (Abs(ty-dy[k]) < 0.0001) && (Abs(tz-dz[k]) < 0.0001) &&
                                    (Abs(ta-da[k]) < 0.0001) && (Abs(tb-db[k]) < 0.0001) && (Abs(tc-dc[k]) < 0.0001))
                                    {
                                        ok = 1;
                                        k = Cnt;
                                    }
                                k += 1;
                            }
                            if (ok == 0) {
                                Cnt += 1;
                                CSNum[Cnt] = n;
                                dx[Cnt] = tx; dy[Cnt] = ty; dz[Cnt] = tz;
                                da[Cnt] = ta; db[Cnt] = tb; dc[Cnt] = tc;
                            }
                        }
                    j++;
                }
                i++;
            }
  
            if (Cnt > 0) {
                nc.OutHeaderText(" Workpiece coordinate systems");
                for (i = 1; i < Cnt + 1; i++) {
                    nc.OutHeaderText(" G" + Str(CSNum[i]) + " = X"+ Str(dx[i]) + " Y" + Str(dy[i]) +" Z" + Str(dz[i]) +
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

            nc.OutHeaderText($"%_N_{nc.ProgName}_MPF");
            nc.OutHeaderText($"$PATH=/_N_MPF_DIR");
            nc.WriteLine();

            nc.OutHeaderText(" Generated by SprutCAM"); 
            nc.OutHeaderText($" Date: {CurDate()}");
            nc.OutHeaderText($" Time: {CurTime()}");
            nc.WriteLine();

            PrintAllTools();
            nc.WriteLine();

            PrintCS(prj);
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

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            if(cmd.TechOperation.Tool.Command != null){
                nc.OutText($"T=\"T{cmd.TechOperation.Tool.Number}\"; {cmd.TechOperation.Tool.Caption}");
                nc.OutText("M6");
            }
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld)
        {
            ConvertPlaneNumber(cld[1], out Plane_);
            nc.GPlane.v = Plane_;
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            if (cld[1] == 71) { // Spindle On
                nc.GPlane.v = Plane_;
                if (Abs(nc.GPlane.v) == Abs(nc.GPlane.v0)) {
                    nc.GPlane.v0 = nc.GPlane.v;
                }
                nc.G54.Show();
                nc.Block.Out();
                switch (cld[4]){
                    case 0: //RPM
                        nc.S.v = cld[2]; nc.S.v0 = double.MaxValue;
                        if (cld[2] > 0)  
                            nc.Msp.v = 3;
                        else 
                            nc.Msp.v = 4;
                        nc.Msp.v0 = double.MaxValue;
                        nc.Block.Out();
                        break;
                    case 2: // css
                        throw new Exception("CSS mode not realized");
                }
            } 
            else if (cld[1] == 72){ // Spindle Off
                nc.Msp.v = 5; //Msp3@ = MaxReal
                nc.Block.Out();
            } 
            else if (cld[1] == 246) { /* Spindle Orient*/ }
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine();
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            if ((nc.G.v > 1) && (nc.G.v < 4))  nc.G.v = 1;
            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            if(nc.X.Changed || nc.Y.Changed || nc.Z.Changed){
                if(!SameText(nc.Feed.v, "10000"))
                    nc.Feed.UpdateState();
                nc.Block.Out();
            }
        }

        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            if(cmd.IsRapid)
                nc.Feed.Hide("10000");
            else
                nc.Feed.Hide(cmd.FeedValue.ToString());
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            if (nc.G.v >= 0) 
                nc.G.v = nc.G.v;

            if ((nc.G.v != 0) && (nc.G.v != 1)) 
                nc.G.v = 1;

            //N50 G0 B0 C0
            foreach(CLDMultiMotionAxis axis in cmd.Axes){
                if(axis.IsX){
                    nc.X.v = axis.Value;
                    nc.X.v0 = double.NaN;
                }  
                else if(axis.IsY){
                    nc.Y.v = axis.Value;
                    nc.Y.v0 = double.NaN;
                }
                else if(axis.IsZ){
                    nc.Z.v = axis.Value;
                    nc.Z.v0 = double.NaN;
                }
                else if(axis.IsA){
                    nc.A.v = axis.Value;
                    nc.A.v0 = double.NaN;
                }
                else if(axis.IsB){
                    nc.B.v = axis.Value;
                    nc.B.v0 = double.NaN;
                }
                else if(axis.IsC){
                    nc.C.v = axis.Value;
                    nc.C.v0 = double.NaN;
                }
            }
            
            if(!SameText(nc.Feed.v, "10000"))
                nc.Feed.UpdateState();
            nc.G.UpdateState();
            nc.Block.Out();
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            //N230 G3 X-810.31 Y-8.355 I=AC(-771.052) J=AC(-0.977)

            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            nc.I.v = cmd.Center.X;
            nc.J.v = cmd.Center.Y;
            if (cld[4] * Sgn(cld[17]) > 0){
                nc.G.v = 3;
            }
            else{
                nc.G.v = 2;
            } //G3/G2
            // Если спираль, то выводим Turn (количество оборотов) явно
            if ((Abs(cld[17]) == 17) && (cmd.SP.Z != nc.Z.v)){
                nc.Turn.v = 0;
                nc.Turn.v0 = double.MaxValue;
                if ((cmd.SP.X == nc.X.v) && (cmd.SP.Y == nc.Y.v))
                    nc.Turn.v = 1;  // полный оборот
            } 
            else if ((Abs(cld[17]) == 18) && (cmd.SP.Y  != nc.Y.v)) {
                nc.Turn.v = 0;
                nc.Turn.v0 = double.MaxValue;
                if ((cmd.SP.X == nc.X.v) && (cmd.SP.Z == nc.Z.v))
                    nc.Turn.v = 1;  // полный оборот
            } 
            else if ((Abs(cld[17]) == 19) && (cmd.SP.X != nc.X.v)){
                nc.Turn.v = 0;
                nc.Turn.v0 = double.MaxValue;
                if ((cmd.SP.Y  == nc.Y.v) && (cmd.SP.Z == nc.Z.v)) 
                    nc.Turn.v = 1;  // полный оборот
            };
            if(nc.X.Changed || nc.Y.Changed || nc.Z.Changed || nc.I.Changed || nc.J.Changed){
                if(!SameText(nc.Feed.v, "10000"))
                    nc.Feed.UpdateState();
                nc.Block.Out();
            }
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        // Uncomment line below (Ctrl + "/"), go to the end of "On" word and press Ctrl+Space to add a new CLData command handler
        // override On

    }
}

