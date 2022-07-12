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

        public void OutText(){
            if(X.Changed || Y.Changed || Z.Changed || I.Changed || J.Changed){
                if(!SameText(Feed.v, "10000"))
                    Feed.UpdateState();
                Block.Out();
            }  
        }

        public void OutHeaderText(string text){
            WriteLine(";" + text);
        }

        public void SetDefaultSpiralTurn(){
            Turn.v = 0;
            Turn.v0 = double.MaxValue;
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        NCFile nc;

        double Plane_;

        int WasCycle800 = 0;
 
        #endregion

        #region Extentions

        public void Cycle800SwitchOff(){
            double tInterp;
            tInterp = nc.G.v0; nc.G.v0 = nc.G.v;
            nc.Block.Out();
            if (WasCycle800 != 0) {
                nc.Block.Form();
                nc.WriteLine("CYCLE800()");
                WasCycle800 = 0;
            }
            nc.G.v0 = tInterp;
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

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld) => nc.WriteLine("; " + op.Comment);
        
        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            if(cmd.TechOperation.Tool.Command != null){
                nc.OutText($"T=\"T{cmd.TechOperation.Tool.Number}\"; {cmd.TechOperation.Tool.Caption}");
                nc.OutText("M6");
            }
            Plane_ = ((double)cmd.Plane);
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld) => nc.GPlane.v = ((double)cmd.Plane);

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) { // Spindle On
                nc.GPlane.v = Plane_;
                if (Abs(nc.GPlane.v) == Abs(nc.GPlane.v0))
                    nc.GPlane.v0 = nc.GPlane.v;
                nc.G54.Show();
                nc.Block.Out();
                switch (cld[4]){
                    case 0: //RPM
                        nc.S.v = cld[2]; nc.S.v0 = double.MaxValue;
                        nc.Msp.v = cmd.IsClockwiseDir ? 3 : 4; nc.Msp.v0 = double.MaxValue;
                        nc.Block.Out();
                        break;
                    case 2: // css
                        throw new Exception("CSS mode not realized");
                }
            } else if (cmd.IsOff){ // Spindle Off
                nc.Msp.v = 5; //Msp3@ = double.MaxValue;
                nc.MCoolant.v = 9;
                nc.Block.Out();
            } else if (cmd.IsOrient) { /* Spindle Orient*/ }
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld) => nc.WriteLine();


        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            if ((nc.G.v > 1) && (nc.G.v < 4))  nc.G.v = 1;
            nc.X.v = cmd.EP.X; nc.Y.v = cmd.EP.Y; nc.Z.v = cmd.EP.Z;
            nc.OutText();
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

            nc.X.v = cmd.EP.X; nc.Y.v = cmd.EP.Y; nc.Z.v = cmd.EP.Z;
            nc.I.v = cmd.Center.X; nc.J.v = cmd.Center.Y;
            nc.G.v = cld[4] * Sgn(cld[17]) > 0 ? 3 : 2; //G3/G2

            // Если спираль, то выводим Turn (количество оборотов) явно
            if ((Abs(cmd.Plane) == 17) && (cmd.SP.Z != nc.Z.v)){
                nc.SetDefaultSpiralTurn();
                if ((cmd.SP.X == nc.X.v) && (cmd.SP.Y == nc.Y.v))
                    nc.Turn.v = 1;  // полный оборот
            } 
            else if ((Abs(cmd.Plane) == 18) && (cmd.SP.Y  != nc.Y.v)) {
                nc.SetDefaultSpiralTurn();
                if ((cmd.SP.X == nc.X.v) && (cmd.SP.Z == nc.Z.v))
                    nc.Turn.v = 1;  // полный оборот
            } 
            else if ((Abs(cmd.Plane) == 19) && (cmd.SP.X != nc.X.v)){
                nc.SetDefaultSpiralTurn();
                if ((cmd.SP.Y  == nc.Y.v) && (cmd.SP.Z == nc.Z.v)) 
                    nc.Turn.v = 1;  // полный оборот
            };
            nc.OutText(); 
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            Cycle800SwitchOff();
            if (cmd.Ptr["Axes(AxisXPos)"] != null) {
                nc.X.v = cmd.Flt["Axes(AxisXPos).Value"]; nc.X.v0 = double.MaxValue;
            }
            if (cmd.Ptr["Axes(AxisYPos)"] != null) {
                nc.Y.v = cmd.Flt["Axes(AxisYPos).Value"]; nc.Y.v0 = double.MaxValue;
            }
            if (cmd.Ptr["Axes(AxisZPos)"] != null) {
                nc.Z.v = cmd.Flt["Axes(AxisZPos).Value"]; nc.Z.v0 = double.MaxValue;
            }
            if (cmd.Ptr["Axes(AxisAPos)"] != null) {
                nc.A.v = cmd.Flt["Axes(AxisAPos).Value"]; nc.A.v0 = double.MaxValue;
            }
            if (cmd.Ptr["Axes(AxisBPos)"] != null) {
                nc.B.v = cmd.Flt["Axes(AxisBPos).Value"]; nc.B.v0 = double.MaxValue;
            }
            if (cmd.Ptr["Axes(AxisCPos)"] != null) {
                nc.C.v = cmd.Flt["Axes(AxisCPos).Value"]; nc.C.v0 = double.MaxValue;
            }
            if ((nc.X.v != nc.X.v0) || (nc.Y.v != nc.Y.v) || (nc.Z.v != nc.Z.v0) || (nc.A.v != nc.A.v0) || (nc.B.v != nc.B.v0) || (nc.C.v != nc.C.v0))
            {
                // nc.G.v = 53; nc.G.v0 = double.MaxValue;
                nc.SUPA.v = 1; nc.SUPA.v0 = 0;
                nc.DTool.v = 0; // Корректор на длину
                nc.Block.Out();
            }
        }

        public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
        {
            switch (cld[1]) {
                // case 50:  // StartSub
                //     nc.Block.Out();
                //     CurNCFile = MainNCPath + NCSub.Name(cld[2]) + ".spf" !NCFileExt;
                //     ChangeNCFile CurNCFile;
                //     Output "%_N_" + NCSub.Name(cld[2]) + "_SPF";
                //     call OutHeader;
                //     nc.WriteLine();
                //     nc.BlockN.v = 0; nc.BlockN.v0 = nc.BlockN.v;
                //     nc.G.v = 0; nc.G.v0 = double.MaxValue;
                //     nc.Feed.v = "0"; nc.Feed.v0 = nc.Feed.v;
                //     break;
                // case 51: // EndSub
                //     nc.Block.Out();
                //     nc.M.v = 17; nc.M.v0 = double.MaxValue;
                //     nc.Block.Out();
                //     CurNCFile = MainNCFile
                //     break;
                // case 52: // CallSub
                //     CheckAxesBrake(2, 2, 2);
                //     nc.Block.Out();
                //     nc.WriteLine("CALL " + Chr(34) + "_N_" + NCSub.Name(cld[2]) + "_SPF" + Chr(34));
                //     break;
                // case 58: // TechInfo
                //     IsFirstCycle = 1;
                //     CSOnCount = 0;
                //     type_op = cmd.Ptr["PPFun(TechInfo).Operation(1)"].Name;
                //         // if type_op != "TST2DContouringOp" and type_op != "HoleMachiningOp" and type_op != "TSTFBMMillOp" then begin
                //         //     G641_on_off = 1
                //         // end else G641_on_off = 0
                //     break;
                case 59: // EndTechInfo
                    // if (G641_on_off == 10) {
                    //     BlockN = BlockN + BlockStep;
                    //     output "N"+str(blockN)+" G60";
                    //     G641_on_off = 0;
                    // }
                    Cycle800SwitchOff();
                    if (cmd.Int["PPFun(EndTechInfo).Enabled"] != 0) {

                            nc.M.v = 1;
                            nc.M.v0 = 0;
                            nc.Block.Out();

                        nc.WriteLine();
                    }
                    break;
            }
        }

        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {
            if(nc.G.v > 0) nc.G.v = 0; 
        }
    }
}

