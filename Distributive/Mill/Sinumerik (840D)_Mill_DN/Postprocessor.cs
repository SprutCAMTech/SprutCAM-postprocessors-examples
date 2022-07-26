namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        public string ProgName;
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;

        public void OutText(){
            if(X.Changed || Y.Changed || Z.Changed || XC_.Changed || YC_.Changed){
                Block.Out();
            }  
        }

        public void OutText(string text){
            Text.Show(text);
            Block.Out();
        }

        public void OutHeaderText(string text) => WriteLine(";" + text);

        public void SetDefaultSpiralTurn() => Turn.Show(0);

        public void SetAxisValues(ICLDMultiGotoCommand cmd, TInp3DPoint lastPnt){
            foreach(CLDMultiMotionAxis axis in cmd.Axes){
                if(axis.IsX){
                    X.v = axis.Value;
                    lastPnt.X = X.v;
                }
                else if(axis.IsY){
                    Y.v = axis.Value;
                    lastPnt.Y = Y.v;
                }
                else if(axis.IsZ){
                    Z.v = axis.Value;
                    lastPnt.Z = Z.v;
                }
                else if(axis.IsA)
                    A.v = axis.Value;
                else if(axis.IsB)
                    B.v = axis.Value;
                else if(axis.IsC)
                    C.v = axis.Value;
            }
        }

        public void SetAxisValues(ICLDPhysicGotoCommand cmd){
            foreach(CLDMultiMotionAxis axis in cmd.Axes){
                if(axis.IsX)
                    X.Show(axis.Value);
                else if(axis.IsY)
                    Y.Show(axis.Value);
                else if(axis.IsZ)
                    Z.Show(axis.Value);
                else if(axis.IsA)
                    A.Show(axis.Value);
                else if(axis.IsB)
                    B.Show(axis.Value);
                else if(axis.IsC)
                    C.Show(axis.Value);
            }
        }

        public void SetAxisValues(ICLDGoHomeCommand cmd){
            foreach(CLDMultiMotionAxis axis in cmd.Axes){
                if(axis.IsX)
                    X.Show(axis.Value);
                else if(axis.IsY)
                    Y.Show(axis.Value);
                else if(axis.IsZ)
                    Z.Show(axis.Value);
                else if(axis.IsA)
                    A.Show(axis.Value);
                else if(axis.IsB)
                    B.Show(axis.Value);
                else if(axis.IsC)
                    C.Show(axis.Value);
            }
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        public NCFile nc;

        SinumerikCycle Cycle = null;

        double Plane_;

        int CSOnCount = 0;

        public TInp3DPoint LastPnt;

        int ExcitedAxABrake; // 0-need't output, 1-need output
        int ExcitedAxBBrake; // 0-need't output, 1-need output
        int ExcitedAxCBrake; // 0-need't output, 1-need output

        double DAM;
        double VARI;
        double VRT;

        double csC;
        double snC;

        #endregion

        #region Extentions

        public void CheckAxesBrake(int ABrake, int BBrake, int CBrake){
            int NeedAxesBrake, tInterp;

            NeedAxesBrake = 0;
            if (NeedAxesBrake > 0){ 
                if (nc.GInterp.v != nc.GInterp.v0) {
                    tInterp = (int)nc.GInterp.v;
                    nc.GInterp.v = nc.GInterp.v0;
                } else
                tInterp = -1;

                if (ABrake >= 0){
                    if (ABrake == 2) {
                        if (ExcitedAxABrake > 0) { // Brake excited before real move command
                            ExcitedAxABrake = 0;
                            nc.Block.Out();
                            nc.MABreak.v0 = double.MaxValue;
                            nc.Block.Out();
                        }
                    } else {
                        if (ABrake == 1) nc.MABreak.v = 46; // On
                        else nc.MABreak.v = 47; // Off
                        if ((ExcitedAxABrake>0) && (nc.MABreak.v != nc.MABreak.v0)) // Idle brake excite
                            ExcitedAxABrake = 0;
                        else
                            ExcitedAxABrake = nc.MABreak.v != nc.MABreak.v0 ? 1 : 0;
                    }
                    nc.MABreak.v0 = nc.MABreak.v;
                }
                if (BBrake >= 0){
                    if (BBrake == 2) {
                        if (ExcitedAxBBrake > 0) { // Brake excited before real move command
                            ExcitedAxBBrake = 0;
                            nc.Block.Out();
                            nc.MBBreak.v0 = double.MaxValue;
                            nc.Block.Out();
                        }
                    } else {
                        if (BBrake == 1) nc.MBBreak.v = 48; // On
                        else nc.MABreak.v = 49; // Off
                        if ((ExcitedAxBBrake>0) && (nc.MBBreak.v != nc.MBBreak.v0)) // Idle brake excite
                            ExcitedAxBBrake = 0;
                        else
                            ExcitedAxBBrake = nc.MBBreak.v != nc.MBBreak.v0 ? 1 : 0;
                    }
                    nc.MBBreak.v0 = nc.MBBreak.v;
                }
                if (CBrake >= 0){
                    if (CBrake == 2) {
                        if (ExcitedAxCBrake > 0) { // Brake excited before real move command
                            ExcitedAxCBrake = 0;
                            nc.Block.Out();
                            nc.MCBreak.v0 = double.MaxValue;
                            nc.Block.Out();
                        }
                    } else {
                        if (CBrake == 1) nc.MBBreak.v = 48; // On
                        else nc.MCBreak.v = 49; // Off
                        if ((ExcitedAxCBrake>0) && (nc.MCBreak.v != nc.MCBreak.v0)) // Idle brake excite
                            ExcitedAxCBrake = 0;
                        else
                            ExcitedAxCBrake = nc.MCBreak.v != nc.MCBreak.v0 ? 1 : 0;
                    }
                    nc.MCBreak.v0 = nc.MCBreak.v;
                }

                if (tInterp >= 0) nc.GInterp.v = tInterp;
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

            LastPnt = new TInp3DPoint();

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

            Cycle = new SinumerikCycle(this);
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld){
            Cycle.SetFirstStatus(true);
            CSOnCount = 0;
            nc.WriteLine($";{Transliterate(op.Comment)}");
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            Cycle.Cycle800SwitchOff();
            if (cmd.Int["PPFun(EndTechInfo).Enabled"] != 0) {
                
                if (cmd.CLDFile.Index != CLDProject.CLDFiles.FileCount - 1){    
                    nc.M.v = 1;
                    nc.M.v0 = 0;
                    nc.Block.Out();
                }

                nc.WriteLine();
            }
        }

        public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
        {
            int v_Dir = 0, v_ST;
            double v_A, v_B, v_C;

            if (cld[4] == 0) {// Select standard workpiece CS
                CSOnCount += 1;
                nc.CoordSys.v = cld[5];
                if (CSOnCount > 1){ 
                    nc.CoordSys.v0 = nc.CoordSys.v;
                    Cycle.Cycle800SwitchOff();
                }
            } else if (cld[4] == 1079) { // Local CS activation (CYCLE800)
                nc.Block.Out();

                if (cmd.Int["PositioningMode"] > 0) { // Turn, Move
                    if (cmd.Ptr["Axes(AxisBPos)"] != null) 
                        if (cmd.Flt["Axes(AxisBPos).Value"] >= 0) v_Dir = +1;
                        else v_Dir = -1;

                    if (cmd.Ptr["Axes(AxisAPos)"] != null)
                        if (cmd.Flt["Axes(AxisAPos).Value"] >= 0) v_Dir = +1;
                        else v_Dir = -1;
                } else // Stay
                    v_Dir = 0;

                v_ST = 000000;
                if (v_Dir > 0)
                    v_ST = v_ST + 200000; // Direction selection "Plus" optimized, Swivel no direction "minus" off
                else if (v_Dir < 0) 
                    v_ST = v_ST + 100000; // Direction selection "Minus" optimized, Swivel no direction "plus" off

                if (cmd.Int["IsSpatial"] > 0)
                    //CYCLE800(_FR, _TC, _ST, _MODE, _X0, _Y0, _Z0, _A, _B, _C, _X1, _Y1, _Z1, _DIR, _FR _I)
                    Cycle.Cycle800(0, "", v_ST, 0*128 + 0*64 + 1*32 + 1*16 + 1*8 + 0*4 + 0*2 + 1*1,
                        cmd.Flt["WCS.OriginPoint.X"], cmd.Flt["WCS.OriginPoint.Y"], cmd.Flt["WCS.OriginPoint.Z"],
                        cmd.Flt["WCS.RotAngles.A"], cmd.Flt["WCS.RotAngles.B"], cmd.Flt["WCS.RotAngles.C"],
                        0, 0, 0, v_Dir, 0);
                else{
                    if (cmd.Ptr["Axes(AxisAPos)"] != null) v_A = cmd.Flt["Axes(AxisAPos).Value"];
                    else v_A = 0;
                    if (cmd.Ptr["Axes(AxisBPos)"] != null) v_B = cmd.Flt["Axes(AxisBPos).Value"];
                    else v_B = 0;
                    if (cmd.Ptr["Axes(AxisCPos)"] != null) v_C = cmd.Flt["Axes(AxisCPos).Value"];
                    else v_C = 0;
                    //CYCLE800(_FR, _TC, _ST, _MODE, _X0, _Y0, _Z0, _A, _B, _C, _X1, _Y1, _Z1, _DIR, _FR _I)
                    Cycle.Cycle800(0, "", v_ST, 1*128 + 1*64 + 1*32 + 1*16 + 1*8 + 0*4 + 0*2 + 1*1,
                        cmd.Flt["WCS.OriginPoint.X"], cmd.Flt["WCS.OriginPoint.Y"], cmd.Flt["WCS.OriginPoint.Z"],
                        v_A, v_B, v_C, 0, 0, 0, v_Dir, 0);
                }
                Cycle.SetCycle800Status(true);
            } else
                throw new Exception("Unknown coordinate system");
        }
        
        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            int OldCoordSys;
            OldCoordSys = (int)nc.CoordSys.v0; nc.CoordSys.v0 = nc.CoordSys.v;
            nc.Tool.Show(cmd.TechOperation.Tool.Number);
            // nc.Tool.v = cmd.TechOperation.Tool.Number; nc.Tool.v0 = double.MaxValue;
            nc.DTool.v = Abs(cld[6]); nc.DTool.v0 = nc.DTool.v;
            if(cmd.TechOperation.Tool.Command != null){
                nc.OutText($"{cmd.TechOperation.Tool.Caption}");
                nc.OutText("M6");
            }
            
            Plane_ = ((double)cmd.Plane);
            nc.Feed.RestoreDefaultValue(false);
            nc.GInterp.RestoreDefaultValue(false); nc.GPlane.RestoreDefaultValue(false);
            nc.X.RestoreDefaultValue(false); nc.Y.RestoreDefaultValue(false); nc.Z.RestoreDefaultValue(false);
            nc.A.RestoreDefaultValue(false); nc.B.RestoreDefaultValue(false); nc.C.RestoreDefaultValue(false);
            nc.CoordSys.v0 = double.MaxValue;
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld) => nc.GPlane.v = ((double)cmd.Plane);

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) { // Spindle On
                nc.GPlane.v = Plane_;
                if (Abs(nc.GPlane.v) == Abs(nc.GPlane.v0))
                    nc.GPlane.v0 = nc.GPlane.v;
                nc.CoordSys.Show();
                nc.Block.Out();
                switch (cld[4]){
                    case 0: //RPM
                        nc.S.Show(cld[2]);
                        nc.Msp.Show(cmd.IsClockwiseDir ? 3 : 4);
                        nc.Block.Out();
                        break;
                    case 2: // css
                        throw new Exception("CSS mode not realized");
                }
            } else if (cmd.IsOff){ // Spindle Off
                nc.Msp.Show(5);
                nc.Block.Out();
            } else if (cmd.IsOrient) { /* Spindle Orient*/ }
        }
        
        public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
        {
            if (cld[2] == 23)     // Radius
                if (cld[1] == 72) // Off
                    nc.KorEcv.v = 40;
                else              // On
                    if (cld[10] == 24) nc.KorEcv.v = 42;
                    else nc.KorEcv.v = 41;
            else if (cld[2] == 9) // Length
                    if (cld[1] == 71) { // On
                        nc.DTool.v = Abs(cld[3]); nc.DTool.v0 = nc.DTool.v;//MaxReal
                    //OutBlock
                    }
        }

        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            nc.X.Hide(cld[1]); nc.Y.Hide(cld[2]); nc.Z.Hide(cld[3]);
            LastPnt.X = cld[1]; LastPnt.Y = cld[2]; LastPnt.Z = cld[3];
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            CheckAxesBrake(2, 2, 2);
            if (!Cycle.CycleOn || Cycle.Cycle_pocket) {  // Другой вывод для сверлильных циклов позиций отверстий
                if ((nc.GInterp.v > 1) && (nc.GInterp.v < 4))  nc.GInterp.v = 1;

                nc.X.v = Cycle.PolarInterp ? (cld[1] * csC - cld[2] * snC) : cmd.EP.X;  // X,Y,Z in absolutes
                nc.Y.v = Cycle.PolarInterp ? (cld[2] * csC + cld[1] * snC) : cmd.EP.Y;
                nc.Z.v = cmd.EP.Z;

                nc.OutText();                     // output in block NC programm
                LastPnt.X = nc.X.v; LastPnt.Y = nc.Y.v; LastPnt.Z = nc.Z.v; // current coordinates
            } 
            LastPnt.X = cld[1]; LastPnt.Y = cld[2]; LastPnt.Z = cld[3]; //Запоминаем координаты отвертсий
            Cycle.SetPocketStatus(false); // Выключаем метку для цикла покетов 
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            nc.Feed.v = cmd.FeedValue;
            if (cld[3] == 315)  nc.GFeed.v = 94;
            else nc.GFeed.v = 95;
            nc.GFeed.Hide();
            nc.GInterp.v = 1;   // G1
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            CheckAxesBrake(2, 2, 2);

            if ((nc.GInterp.v != 0) && (nc.GInterp.v != 1)) 
                nc.GInterp.v = 1;

            //N50 G0 B0 C0
            nc.SetAxisValues(cmd, LastPnt);
            
            nc.GInterp.UpdateState();
            if(nc.X.Changed || nc.Y.Changed || nc.Z.Changed || nc.A.Changed || nc.B.Changed || nc.C.Changed)
                nc.Block.Out();
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            //N230 G3 X-810.31 Y-8.355 I=AC(-771.052) J=AC(-0.977)

            double tempX = Cycle.PolarInterp ? (cld[5]*csC - cld[6]*snC) : cmd.EP.X;
            double tempY = Cycle.PolarInterp ? (cld[6]*csC + cld[5]*snC) : cmd.EP.Y;
            double tempZ = cmd.EP.Z;
            
            nc.GInterp.v = cld[4] * Sgn(cld[17]) > 0 ? 3 : 2; //G3/G2

            nc.X.Show(tempX); nc.Y.Show(tempY); nc.Z.Show(tempZ); // X,Y,Z in absolutes

            if ((Abs(nc.GPlane.v) == 17) && (LastPnt.Z == tempZ))
                nc.Z.Hide(tempZ);
            else if ((Abs(nc.GPlane.v)==18) && (LastPnt.Y == tempY))
                nc.Y.Hide(tempY);
            else if ((Abs(nc.GPlane.v)==19) && (LastPnt.X == tempX))
                nc.Z.Hide(tempX);

            // Если спираль, то выводим Turn (количество оборотов) явно
            if ((Abs(cmd.Plane) == 17) && (LastPnt.Z != tempZ)){
                nc.SetDefaultSpiralTurn();
                if ((LastPnt.X == tempX) && (LastPnt.Y == tempY))
                    nc.Turn.v = 1;  // полный оборот
            } 
            else if ((Abs(cmd.Plane) == 18) && (LastPnt.Y  != tempY)) {
                nc.SetDefaultSpiralTurn();
                if ((LastPnt.X == tempX) && (LastPnt.Z == tempZ))
                    nc.Turn.v = 1;  // полный оборот
            } 
            else if ((Abs(cmd.Plane) == 19) && (LastPnt.X != tempX)){
                nc.SetDefaultSpiralTurn();
                if ((LastPnt.Y  == tempY) && (LastPnt.Z == tempZ)) 
                    nc.Turn.v = 1;  // полный оборот
            };

            if (Abs(nc.GPlane.v) != 19) 
                nc.XC_.Show(Cycle.PolarInterp ? (cld[1]*csC - cld[2]*snC) : cld[1]);
            if (Abs(nc.GPlane.v) != 18) 
                nc.YC_.Show(Cycle.PolarInterp ? (cld[2]*csC + cld[1]*snC) : cld[2]);
            if (Abs(nc.GPlane.v) != 17)
                nc.ZC_.Show(cld[3]);

            nc.OutText(); 
            LastPnt.X = tempX; LastPnt.Y = tempY; LastPnt.Z = tempZ;
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            if(nc.GInterp.Changed)
                nc.Block.Show();
            else
                nc.Block.Hide();
            nc.Block.Out();
            Cycle.Cycle800SwitchOff();

            nc.SetAxisValues(cmd);

            if (nc.X.Changed || nc.Y.Changed || nc.Z.Changed || nc.A.Changed || nc.B.Changed || nc.C.Changed)
            {
                // nc.G.v = 53; nc.G.v0 = double.MaxValue;
                nc.SUPA.Show();
                // nc.SUPA.v = 1; nc.SUPA.v0 = 0;
                nc.DTool.v = 0; // Корректор на длину
                nc.Block.Out();
            }
        }

        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {     
            if(nc.GInterp.v > 0) nc.GInterp.v = 0; 
        }

        public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn)
                if (cld[2] == 1)
                    nc.MCoolant.v = 8; // жидкость
                else if (cld[2] == 2) 
                    nc.MCoolant.v = 8; // туман
                else if (cld[2] == 3)
                    nc.MCoolant.v = 8; // инструмент
                else
                    nc.MCoolant.v = 8; // что-то еще
            else
                nc.MCoolant.Show(9);
        }

        public override void OnGoHome(ICLDGoHomeCommand cmd, CLDArray cld)
        {
            if (Cycle.CycleOn)
                Cycle.SetStatus(false);
            nc.Block.Out();

            Cycle.Cycle800SwitchOff();
            nc.SetAxisValues(cmd);
            nc.SUPA.Show();
            nc.GInterp.Show(0);
            nc.DTool.v = 0; // Корректор на длину

            nc.Block.Out();
            nc.GPlane.RestoreDefaultValue(false);
        }

        public override void OnExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            int CycleNumber;       // Cycle number
            string CycleName;      // Cycle name
            string CycleGeomName;  // Имя подпрограммы-геометрии цикла
            int CDIR;              // Thread direction 2-G2, 3-G3
            double SDIR;           // Spindle rotation direction
            double CurPos;         // Current position (applicate)
            double CPA = 0;            // Absciss
            double CPO = 0;            // Ordinate
            double TempCoord;      // Auxiliary variable
            double RTP, RFP, SDIS, DP, DPR;

            if (cmd.IsOn) {
                Cycle.SetStatus(true);      // ON
                nc.GFeed.v = cld[9] == 1 ? 94 : 95;
                nc.GFeed.Hide();
                nc.Feed.v = cld[10];// Feed_@=MaxReal
                nc.Block.Out();
            }
            else if (cmd.IsOff) Cycle.SetStatus(false); // OFF
            else if (cmd.IsCall) { // CALL
                CheckAxesBrake(2, 2, 2);

                CycleNumber = 0;
                CycleName = "CYCLE";
                CycleGeomName = "";
                switch(cmd.CycleType){
                    case 473:
                    case >= 481 and <=491:
                        switch(Abs(nc.GPlane.v)){
                            case 17: // XY
                                CurPos = nc.Z.v;
                                CPA = nc.X.v;
                                CPO = nc.Y.v;
                                break;
                            case 18: // ZX
                                CurPos = nc.Y.v;
                                CPA = nc.Z.v;
                                CPO = nc.X.v;
                                break;
                            case 19: // YZ
                                CurPos = nc.X.v;
                                CPA = nc.Y.v;
                                CPO = nc.Z.v;
                                break;
                            default:
                                CurPos = nc.Z.v;
                                CPA = nc.X.v;
                                CPO = nc.Y.v;
                                throw new Exception("Undefined cycle plane");
                        }
                        if (nc.GPlane.v < 0) {
                            TempCoord = CPA;
                            CPA = CPO;
                            CPO = TempCoord;
                        }
                        // Define base levels
                        RTP = CurPos;
                        RFP = CurPos - cld[7]*Sgn(nc.GPlane.v); // CurPos - Tp
                        SDIS = cld[7] - cld[6]; // Tp - Sf
                        DP = CurPos - cld[8]*Sgn(nc.GPlane.v); // CurPos - Bt
                        DPR = cld[8] - cld[7]; // Bt - Tp
                        // CycleXX(RTP,RFP,SDIS,DP)
                        Cycle.AddPrm(RTP, 0);
                        Cycle.AddPrm(RFP, 1);
                        Cycle.AddPrm(SDIS, 2);
                        Cycle.AddPrm(DP, 3);
                        CycleNumber = 81;
                        switch(cmd.CycleType){
                            case 481: 
                            case 482:
                            case >= 485 and <=489: // Simple drilling
                                CycleNumber = cmd.CycleType - 400;
                                Cycle.AddPrm(double.MaxValue, 4);
                                if (cld[15] > 0) Cycle.AddPrm(cld[15], 5); // Delay in seconds
                                // Spindle rotation direction
                                SDIR = nc.S.v > 0 ? 3 : 4;
                                if ((cmd.CycleType == 486) || (cmd.CycleType == 488)){
                                    if (!(cld[15] > 0))
                                        Cycle.AddPrm(double.MaxValue, 5);
                                    Cycle.AddPrm(SDIR, 6);
                                }  
                                else if (cmd.CycleType == 487)
                                    Cycle.AddPrm(SDIR, 5);
                                if (cmd.CycleType == 485) {
                                    if (!(cld[15] > 0))
                                        Cycle.AddPrm(double.MaxValue, 5);
                                    Cycle.AddPrm(cld[10], 6); // WorkFeed
                                    Cycle.AddPrm(cld[14], 7); // ReturnFeed
                                } else if (cmd.CycleType == 486) {
                                    if (!(cld[15] > 0))
                                        Cycle.AddPrm(double.MaxValue, 5);
                                    Cycle.AddPrm(0, 7); // RPA
                                    Cycle.AddPrm(0, 8); // RPO
                                    Cycle.AddPrm(0, 9); // RPAP
                                    Cycle.AddPrm(0, 10); // POSS
                                    if (Cycle.Prms[6] == 0)
                                        Cycle.AddPrm(double.MaxValue, 6);
                                }
                                break;
                            case 473:
                            case 483: // Deep drilling (473-chip breaking, 483-chip removing)
                                CycleNumber = 83;
                                Cycle.AddPrm(double.MaxValue, 4);
                                Cycle.AddPrm(CurPos - (cld[7]+cld[17])*Sgn(nc.GPlane.v), 5); // FDEP = CurPos-(Tp+St)
                                Cycle.AddPrm(double.MaxValue, 6);
                                Cycle.AddPrm(cld[18], 7); // DAM - degression
                                Cycle.AddPrm(cld[15], 8); // DTB - Bottom delay
                                Cycle.AddPrm(cld[16], 9); // DTS - Top delay
                                Cycle.AddPrm(1, 10); // FRF - First feed coef
                                Cycle.AddPrm((cmd.CycleType == 473)? 0 : 1, 11); // VARI - breaking or removing
                                Cycle.AddPrm(double.MaxValue, 12);
                                Cycle.AddPrm(cld[18], 13);// _MDEP - Minimal deep step (=degression)
                                if (cmd.CycleType == 473)
                                    Cycle.AddPrm(cld[20], 14); // _VRT - LeadOut
                                else{
                                    Cycle.AddPrm(double.MaxValue, 14); 
                                    Cycle.AddPrm(cld[19], 16); // _DIS1 - Deceleration
                                }
                                Cycle.AddPrm(0, 15); // _DTD - finish delay (if 0 then = DTB)
                                break;
                            case 484: // Tapping
                                SDIR = nc.S.v > 0 ? 3 : 4;
                                if (cld[19] == 1) { // Fixed socket
                                    CycleNumber = 84;
                                    Cycle.AddPrm(double.MaxValue, 4);
                                    Cycle.AddPrm(double.MaxValue, 5);
                                    Cycle.AddPrm(SDIR, 6); // SDAC
                                    Cycle.AddPrm(double.MaxValue, 7);
                                    Cycle.AddPrm((nc.S.v > 0)? cld[17] : -cld[17], 8); // PIT
                                    Cycle.AddPrm(cld[18], 9); // POSS
                                    Cycle.AddPrm(double.MaxValue, 10);
                                    Cycle.AddPrm(double.MaxValue, 11);
                                    Cycle.AddPrm(double.MaxValue, 12);
                                    Cycle.AddPrm(1, 13); //  PTAB
                                } else { // Floating socket
                                    CycleNumber = 840;
                                    Cycle.AddPrm(double.MaxValue, 4);
                                    Cycle.AddPrm(double.MaxValue, 5);
                                    Cycle.AddPrm(0, 6); // SDR
                                    Cycle.AddPrm(SDIR, 7); // SDAC
                                    Cycle.AddPrm(11, 8); // ENC
                                    Cycle.AddPrm(double.MaxValue, 9);
                                    Cycle.AddPrm((nc.S.v > 0)? cld[17] : -cld[17], 10); // PIT
                                    Cycle.AddPrm(double.MaxValue, 11);
                                    Cycle.AddPrm(1, 12);
                                }
                                if (Cycle.IsFirstCycle) {
                                    VARI = 0;
                                    DAM = cld[2] / 5;
                                    VRT = DAM / 5;
                                    // Input "Введите параметры цикла нарезания резьбы CYCLE84:",
                                    //       "Подтип цикла VARI (0-простое, 1-ломка стружки, 2-удаление стружки)", VARI,
                                    //       "Шаг для ломки стружки (DAM)", DAM,
                                    //       "Отвод при ломке стружки (VRT)", VRT
                                }
                                if (VARI > 0) {
                                    Cycle.AddPrm(VARI, 15); //VARI
                                    Cycle.AddPrm(DAM, 16);  //DAM
                                    Cycle.AddPrm(VRT, 17);  //VRT
                                }
                                break;
                            case 490: // Thread milling
                                CycleNumber = 90;
                                Cycle.AddPrm(double.MaxValue, 4);
                                Cycle.AddPrm(cld[16], 5); // DIATH - Outer diameter
                                Cycle.AddPrm(cld[16] - cld[22] * 2, 6); // KDIAM - Inner diameter
                                Cycle.AddPrm(cld[17], 7); // PIT - thread step
                                Cycle.AddPrm(cld[10], 8); // FFR - Work feed
                                CDIR = cld[19]; // CDIR - Spiral direction
                                if ((CDIR != 2) && (CDIR != 3))
                                    if ((nc.S.v > 0) && (CDIR == 0))       CDIR = 3;
                                    else if ((nc.S.v <= 0) && (CDIR == 0)) CDIR = 2;
                                    else if ((nc.S.v > 0) && (CDIR == 1))  CDIR = 2;
                                    else if ((nc.S.v <= 0) && (CDIR == 1)) CDIR = 3;
                                Cycle.AddPrm(CDIR, 9);
                                Cycle.AddPrm(cld[18], 10); // TYPTH - 0-inner, 1-outer thread
                                Cycle.AddPrm(CPA, 11); // CPA - Center X
                                Cycle.AddPrm(CPO, 12); // CPO - Center Y
                                break;
                            case 491: // Hole pocketing
                                CycleNumber = 4;
                                CycleName = "POCKET";
                                Cycle.SetPocketStatus(true);
                                Cycle.AddPrm(0.5 * cld[16], 4);// PRAD - Radius
                                Cycle.AddPrm(CPA, 5); // PA - Center X
                                Cycle.AddPrm(CPO, 6);// PO - Center Y
                                Cycle.AddPrm(cld[20], 7); // MID - Deep step
                                Cycle.AddPrm(0, 8); // FAL - finish wall stock
                                Cycle.AddPrm(0, 9); // FALD - finish deep stock
                                Cycle.AddPrm(cld[10], 10); // FFP1 - Work feed
                                Cycle.AddPrm(cld[12], 11); // FFD - Plunge feed
                                CDIR = cld[19];
                                if (CDIR <= 1) CDIR = 1 - CDIR;
                                Cycle.AddPrm(CDIR, 12); // CDIR - Spiral direction
                                Cycle.AddPrm(21, 13); // VARI - Rough spiral machining
                                Cycle.AddPrm(cld[22], 14);// MIDA - Horizontal step
                                Cycle.AddPrm(double.MaxValue, 15);
                                Cycle.AddPrm(double.MaxValue, 16);
                                Cycle.AddPrm(0.5 * cld[18], 17); // RAD1 - Spiral radius
                                Cycle.AddPrm(cld[17], 18); // DP1 - Spiral step
                                break;
                        }
                        break;// 5D Drilling cycles
                } // end cmd.CycleType

                if (cmd.CycleType == 491){// Для карманов POCKET
                    Cycle.OutCycle(CycleName+Str(CycleNumber), CycleGeomName);
                    nc.GInterp.v0 = double.MaxValue;
                }else { // Вывод цикла MCALL для группы отверстий
                    Cycle.OutCycle("MCALL" + " " + CycleName + Str(CycleNumber), CycleGeomName);
                    Cycle.Cycle_position();  //Вывод позиций отверстий
                }
                Cycle.SetFirstStatus(false);
            }

            if (cmd.IsOff && cmd.CycleType != 491) {//Выключение цикла
                nc.WriteLine($"{nc.BlockN} MCALL");
                nc.BlockN.AddStep();
                Cycle.SetFirstStatus(true);
                Cycle.SetCycleCompareString(""); // Принудительно стираем, т.к. цикл закрыт
            } //Выключение цикла
        }

        public override void OnFini(ICLDFiniCommand cmd, CLDArray cld)
        {
            CheckAxesBrake(0, 0, 0);
            CheckAxesBrake(2, 2, 2);
            nc.Block.Out();
            nc.M.Show(30);// M30 end programm
            nc.Block.Out();
        }

        public override void OnInterpolation(ICLDInterpolationCommand cmd, CLDArray cld)
        {
            if (cmd.InterpType == 9023){// MULTIAXIS interpolation
                if (cmd.IsOn){ // Switch on
                    nc.Block.Out();
                    nc.WriteLine($"{nc.BlockN} TRAORI"); nc.BlockN.AddStep();
                    nc.CoordSys.v0 = double.MaxValue; nc.Block.Out();
                    nc.WriteLine($"{nc.BlockN} ORIWKS"); nc.BlockN.AddStep();
                    nc.WriteLine($"{nc.BlockN} ORIAXES"); nc.BlockN.AddStep();
                }else{          // Switch off
                    nc.Block.Out();
                    nc.WriteLine($"{nc.BlockN} TRAFOOF"); nc.BlockN.AddStep();
                }
            }else if (cmd.InterpType == 9021) {// Polar interpolation
                if (cmd.IsOn) { // Switch on
                    nc.Block.Out();
                    nc.WriteLine("TRANSMIT");
                    Cycle.SetPolarInterpolationStatus(true);
                    csC = Cos(nc.A.v);
                    snC = -Sin(nc.A.v);
                }else {          // Switch off
                    nc.Block.Out();
                    nc.WriteLine("TRANSMIT");
                    Cycle.SetPolarInterpolationStatus(true);
                    nc.A.RestoreDefaultValue(false);
                }
            }else if (cmd.InterpType == 9022) {// Cylindrical interpolation
                if (cmd.IsOn) { // Switch on
                    nc.Block.Out();
                    nc.WriteLine("TRACYL(" + Str(2*cld[3]) + ")");
                    Cycle.SetCilindInterpolationStatus(true);
                }else{             // Switch off
                    nc.Block.Out();
                    nc.WriteLine("TRAFOOF");
                    //Output "TMCOFF"
                    Cycle.SetCilindInterpolationStatus(false);
                    nc.A.RestoreDefaultValue(false);
                }
            }
        }

        public override void OnAxesBrake(ICLDAxesBrakeCommand cmd, CLDArray cld)
        {
            int ABrake = -1, BBrake = -1, CBrake = -1;

            foreach(CLDAxisBrake axis in cmd.Axes){
                if(axis.IsA)
                    ABrake = axis.StateIsOn ? 1 : 0;
                else if(axis.IsB)
                    BBrake = axis.StateIsOn ? 1 : 0;
                else if(axis.IsC)
                    CBrake = axis.StateIsOn ? 1 : 0;
            }

            CheckAxesBrake(ABrake, BBrake, CBrake);
        }

        public override void OnDelay(ICLDDelayCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.GPause.Show(4);
            nc.FPause.Show(cld[1]);
            nc.Block.Out();
        }

        public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.M.Show(1);// M01
            nc.Block.Out();
        }

        public override void OnStop(ICLDStopCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.M.Show(0);// M00
            nc.Block.Out();
        }

        public override void StopOnCLData(){
            base.StopOnCLData();
        }
    }
}