using System.Collections;
namespace SprutTechnology.SCPostprocessor
{
    enum OpType {
        Unknown,
        Mill,
        Lathe,
        Auxiliary,
        WireEDM
    }

    public partial class NCFile: TTextNCFile
    {
        ///<summary>Main nc-programm number</summary>
        public int ProgNumber {get; set;}

        ///<summary>Last point (X, Y, Z) was written to the nc-file</summary>
        public TInp3DPoint LastP {get; set;}

        public double LastC = 99999;

        public override void OnInit()
        {
        //     this.TextEncoding = Encoding.GetEncoding("windows-1251");
        }

        public void OutWithN(params string[] s) {
            string outS = "";
            if (!BlockN.Disabled) {
                outS = BlockN.ToString(BlockN);
                BlockN.v = BlockN + 1;
            }
            for (int i=0; i<s.Length; i++) {
                if (!String.IsNullOrEmpty(outS)) 
                   outS += Block.WordsSeparator;
                outS += s[i];
            }
            WriteLine(outS);
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition

        ///<summary>Current nc-file. It could be main or sub.</summary>
        public NCFile nc;

        ///<summary>G81-G89 cycle is on</summary>
        bool cycleIsOn = false;

        ///<summary>X axis scale coefficient (1 - radial, 2 - diametral)</summary>
        double xScale = 1.0;

        ///<summary>Type of current operation (mill, lathe, etc.)</summary>
        OpType currentOperationType = OpType.Unknown;

        ///<summary>Type of active lathe spindle (main, counter)</summary>
        int activeLatheSpindle;

        double IsFirstC = 1;

        ///<summary>Variable of active plane from LOADTL</summary>
        double Plane_;

        
        public TInp3DPoint FromP_ {get; set;}

        ///<summary>Current point (X, Y, Z)</summary>
        public TInp3DPoint PT_ {get; set;}

        #endregion

        void PrintAllTools(){
            SortedList tools = new SortedList();
            for (int i=0; i<CLDProject.Operations.Count; i++){
                var op = CLDProject.Operations[i];
                if (op.Tool==null || op.Tool.Command==null)
                    continue;
                if (!tools.ContainsKey(op.Tool.Number))
                    tools.Add(op.Tool.Number, Transliterate(op.Tool.Caption));
            }            
            nc.WriteLine("( Tools list )");
            NumericNCWord toolNum = new NumericNCWord("T{0000}", 0);
            for (int i=0; i<tools.Count; i++){
                toolNum.v = Convert.ToInt32(tools.GetKey(i));
                nc.WriteLine(String.Format("( {0}    {1} )", toolNum.ToString(), tools.GetByIndex(i)));
            }
        }

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgNumber = Settings.Params.Int["OutFiles.NCProgNumber"];

            nc.WriteLine("%");
            nc.WriteLine("O" + Str(nc.ProgNumber));

            PrintAllTools();
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
            nc.Output("M30");
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            // One empty line between operations 
            nc.WriteLine();
            
            currentOperationType = (OpType)(int)cld[60];
            xScale = currentOperationType == OpType.Lathe ? 2 : 1;
        }  

        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {
            nc.WriteLine("( " + cmd.CLDataS + " )");
        }

        public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
        {
            if (cld[4] == 0) nc.GWCS.v = cmd.CSNumber;
            else if(cld[4] == 1079)
            {
                nc.Block.Show(nc.BlockN, nc.GWCS);
                nc.Block.Out();

                if(Abs(cmd.EN.A) > 0.0001 | Abs(cmd.EN.B) > 0.0001 | Abs(cmd.EN.C) > 0.0001)
                {
                    nc.RotA.v = cmd.EN.A; nc.RotA.v0 = 0;
                    nc.RotB.v = cmd.EN.B; nc.RotB.v0 = 0;
                    nc.RotC.v = cmd.EN.C; nc.RotC.v0 = 0;
                    nc.Block.Form();
                }
                nc.X.v = cmd.EP.X; nc.X.v0 = 0;
                nc.Y.v = cmd.EP.Y; nc.Y.v0 = 0; 
                nc.Z.v = cmd.EP.Z; nc.Z.v0 = 0;
                nc.Block.Form();
            }
            else Debug.Write("Unknown coordinate system");
        }

        //(ORIGIN part)
        public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            //nc.GWCS.v = cmd.CSNumber;
            // nc.Block.Show(nc.BlockN, nc.GWCS);
            // nc.Block.Out();
            base.OnWorkpieceCS(cmd, cld);
        }
        
        // GPlane, Переключение рабочих плоскостей (XY, XZ, YZ)
        private double ChangeGPlane(double cld14) => cld14 switch
        {
            33 => 17,
            41 => 18,
            37 => 19,
            133 => -17,
            141 => -18,
            137 => -19,
            _ => 0
        };

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            nc.Block.Show(nc.BlockN, nc.GWCS);
            nc.Block.Out();

            nc.T.v = cmd.TechOperation.Tool.Number;
            nc.Block.Show(nc.BlockN, nc.T);
            nc.Block.Out();

            //Переключение рабочих плоскостей (XY, XZ, YZ)
            var newGplane = ChangeGPlane((double)cmd.Plane);
            if (newGplane != 0) Plane_ = newGplane;
            else Debug.WriteLine("Wrong given a plane of processing");

            //nc.Block.Show(nc.F, nc.GInterp, nc.S, nc.S2, nc.S3);
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld)
        {
            var newGplane = ChangeGPlane((double)cmd.Plane);
            if (newGplane != 0) Plane_ = newGplane;
            else Debug.WriteLine("Wrong given a plane of processing");
        }

        public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) 
            {
                if (cmd.IsLeftDirection) nc.GRCompens.v = 41;
                else nc.GRCompens.v = 42;
            } 

            else 
            {
                nc.GRCompens.v = 40;
            }

            nc.GRCompens.Hide();
        }

        //Задаются координаты исходной точки
        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            nc.X.v = currentOperationType == OpType.Lathe ? cld[1] * 2 : cld[1];
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;

            nc.X.Hide();
            nc.Y.Hide();
            nc.Z.Hide();

            //FROMX_ = X
            //XT_ = X
            FromP_ = new TInp3DPoint(nc.X.v,nc.Y.v,nc.Z.v);
            PT_ = new TInp3DPoint(nc.X.v,nc.Y.v,nc.Z.v);
        }

        private void DetectSpindle(ICLDSelWorkpieceCommand cmd)
        {
            var ts = cmd.CLDataS.ToUpper();
            if (ts.IndexOf("COUNT") > 0 || ts.IndexOf("SUB") > 0 || ts.IndexOf("SECOND") > 0)
            {
                activeLatheSpindle = 2;                    
            }

            else activeLatheSpindle = 1;
        }

        //выбор активной державки заготовки          
        public override void OnSelWorkpiece(ICLDSelWorkpieceCommand cmd, CLDArray cld)
        {
            DetectSpindle(cmd);
            currentOperationType = OpType.Lathe;
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            if (cld[1] == 71)
            {
                if (currentOperationType is OpType.Mill)
                {
                    nc.GPlane.v = Plane_; 
                    if (Abs(nc.GPlane.v) == Abs(nc.GPlane.v0)) nc.GPlane.v0 = nc.GPlane.v;
                    nc.Block.Out();

                    if (activeLatheSpindle == 1)
                    {
                        nc.LastC = 0;
                        nc.C.v = 0; //подключение оси  C
                    }
                    else
                    {
                        nc.C2.v = 0;
                    }
                    nc.Block.Out();
                    nc.SetMS.v = 3; //Активация приводного инструмента 
                    nc.Block.Out();

                    //check the spindle rotation mode (RPM or CSS) cld[4].
                    switch (cmd.SpeedMode)
                    {
                        case CLDSpindleSpeedMode.Unknown: 
                            nc.GCssRpm.v = 97;
                            nc.GCssRpm.Show();
                            nc.S3.v = cmd.RPMValue; //Rotation rate
                            nc.MSp3.v = cmd.RPMValue > 0 ? 3 : 4;

                            nc.MSp3.Show();
                            nc.Block.Out();
                            break;
                        case CLDSpindleSpeedMode.CSS:
                            Debug.WriteLine("Режим CSS во фрезерной обработке не реализован");
                            break;
                    }
                }

                else
                {
                    nc.GPlane.v = 18;
                    nc.Block.Out();

                    if (activeLatheSpindle == 1)
                    {
                        nc.SetMS.v = 1;
                    }

                    else if (activeLatheSpindle == 2)
                    {
                        nc.SetMS.v = 2;
                    }
                    nc.Block.Out();

                    switch (cmd.SpeedMode)
                    {
                        case CLDSpindleSpeedMode.Unknown: 
                            nc.GCssRpm.v = 97;
                            nc.GCssRpm.Show();
                            
                            if (activeLatheSpindle == 1)
                            {
                                nc.S.v = cmd.RPMValue;
                                nc.MSp.v = cmd.RPMValue > 0 ? 4 : 3;

                                nc.Block.Show(nc.S, nc.MSp);
                            }

                            else
                            {
                                nc.S2.v = cmd.RPMValue;
                                nc.MSp2.v = cmd.RPMValue > 0 ? 4 : 3;
                                
                                nc.Block.Show(nc.S2, nc.MSp2);
                            }

                            nc.Block.Out();
                            break;
                        case CLDSpindleSpeedMode.CSS:
                            nc.Lims.v = cmd.RPMValue;
                            nc.Lims.Show();
                            nc.Block.Out();

                            nc.GCssRpm.v = 96; //G96 - by default => needs to change the state
                            nc.GCssRpm.Show();

                            if (activeLatheSpindle == 1)
                            {
                                nc.S.v = cmd.CSSValue; //cld[5]
                                nc.MSp.v = cmd.CSSValue > 0 ? 4 : 3;

                                nc.Block.Show(nc.S, nc.MSp);
                            }

                            else
                            {
                                nc.S2.v = cmd.CSSValue;
                                nc.MSp2.v = cmd.CSSValue > 0 ? 4 : 3;

                                nc.Block.Show(nc.S2, nc.MSp2);
                            }
                            nc.Block.Out();
                            break;
                    }
                }
            }

            else if (cld[1] == 72)
            {
                if(currentOperationType == OpType.Mill)
                {
                    nc.SetMS.v = 3;
                    nc.Block.Out();
                }

                else
                {
                    if (activeLatheSpindle == 1)
                    {
                        nc.SetMS.v = 1;
                        nc.Block.Out();
                        nc.MSp.v = 5;
                        nc.MSp.Show();
                    }
                    else
                    {
                        nc.SetMS.v = 2;
                        nc.Block.Out();
                        nc.MSp2.v = 5;
                    }
                }
                nc.Block.Out();
            }

            else if (cld[1] == 246) return; //Spindle Orient
        }

        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {
            if (nc.GInterp.v > 0){
                nc.GInterp.v = 0;
            } 
            //ThreadAngle
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            if (nc.GInterp.v > 1){
                nc.GInterp.v = 1;
                nc.GInterp.v0 = nc.GInterp.v;
            }
                
            foreach(CLDMultiMotionAxis ax in cmd.Axes) 
            {
                if (ax.ID == "AxisZ2Pos") {
                    nc.Z2.v = ax.Value;
                    if (nc.Z2.v0 != nc.Z2.v) nc.Block.Out();
                }
                else if (ax.IsX) {
                    nc.X.v = ax.Value * xScale;
                }
                    
                else if (ax.IsY) {
                    nc.Y.v = ax.Value;
                    if (currentOperationType == OpType.Lathe) nc.Y.v0 = nc.Y.v;
                }
                else if (ax.IsZ) {
                    nc.Z.v = ax.Value;
                }
                if (currentOperationType != OpType.Lathe)
                {
                    if (ax.IsC)
                    {
                        if (IsFirstC == 1){
                            nc.C.v = ax.Value;
                            IsFirstC = 0;
                        }

                        else{
                            nc.C.v = cmd.Flt["Axes(AxisCPos).Value"];
                            if (Abs(nc.C.v - nc.LastC) < 180){
                                nc.C.v = ax.Value;
                                if(nc.C.v == nc.LastC) nc.C.v = nc.C.v0;
                            }
                            else{
                                nc.C_.v = nc.C.v - nc.LastC;
                                nc.C.v0 = nc.C.v;
                            }
                        }

                        nc.LastC = cmd.Flt["Axes(AxisCPos).Value"];
                    }

                    else if(ax.IsC2) nc.C2.v = ax.Value;
                }
                
                if (ax.ID == "JawDiameter"){
                    nc.MChuck.v = ax.Value > 220 ? 25 : 26;
                }

                else if (ax.ID == "JawDiameter2"){
                    nc.MChuck2.v = ax.Value > 220 ? 25 : 26;
                }
            }

            nc.Block.Out();  
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            if (nc.GInterp.v > 1 && nc.GInterp.v < 4) nc.GInterp.v = 1;
            if (nc.GInterp.v == 33) nc.Block.Show(nc.GInterp);
            if (nc.GPolarOrCyl.v == 1)
            {
                nc.X.v = cmd.EP.X * Cos(nc.RotC.v) - cmd.EP.Y * Sin(nc.RotC.v);
                nc.Y.v = cmd.EP.Y * Cos(nc.RotC.v) + cmd.EP.X * Sin(nc.RotC.v);
            }

            else
            {
                nc.X.v = cmd.EP.X * xScale; //xScale = 2
                nc.Y.v = cmd.EP.Y;
            }

            nc.Z.v = cmd.EP.Z;
            if(currentOperationType == OpType.Lathe )
            {
                nc.Y.v0 = nc.Y.v;
            }
            
            nc.Block.Out();
            nc.LastP = cmd.EP;
        }

        public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
        {
            nc.MCoolant.v = cmd.IsOn ? 8 : 9;
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            nc.F.v = cmd.FeedValue;

            if (nc.F.v < 10000)
            {
                nc.GFeed.v = cld[3] == 315 ? 94 : 95;
                nc.GInterp.v = 1;
            }
            else
            {
                nc.GInterp.v = 0;
                nc.F.v0 = nc.F.v;
                //ThreadStartAngel
            }
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            nc.GInterp.v = cmd.R * Sgn(nc.GPlane.v) > 0 ? 3 : 2;

            if (nc.GPolarOrCyl.v == 1)
            {
                nc.X.v = cmd.EP.X * Cos(nc.RotC.v) - cmd.EP.Y * Sin(nc.RotC.v);
                nc.Y.v = cmd.EP.Y * Cos(nc.RotC.v) + cmd.EP.X * Sin(nc.RotC.v);
            }

            else
            {
                nc.X.v = cmd.EP.X * xScale; //xScale = 2
                nc.Y.v = cmd.EP.Y;
            }

            nc.X.Show();
            nc.Y.Show();
            nc.Z.v = cmd.EP.Z;
            nc.Z.Show();

            if (currentOperationType == OpType.Lathe) nc.Y.v0 = nc.Y.v; //don't output in lathe 

            if (Abs(nc.GPlane.v) == 17 && nc.LastP.Z == nc.Z.v) nc.Z.v0 = nc.Z.v;
            else if (Abs(nc.GPlane.v) == 17 && nc.LastP.Y == nc.Y.v) nc.Y.v0 = nc.Y.v;
            else if (Abs(nc.GPlane.v) == 17 && nc.LastP.X == nc.X.v) nc.X.v0 = nc.X.v;

            //Если спираль, то выводим Turn (количество оборотов) явно
            if(Abs(nc.GPlane.v) == 17 && PT_.Z != nc.Z.v)
            {
                nc.Turn.v = 0;
                nc.Turn.Show();

                if(PT_.X == nc.X.v && PT_.Y == nc.Y.v){
                    nc.Turn.v = 1;
                }
            }

            else if (Abs(nc.GPlane.v) == 18 && PT_.Y != nc.Y.v)
            {
                nc.Turn.v = 0;
                nc.Turn.Show();

                if(PT_.X == nc.X.v && PT_.Z == nc.Z.v){
                    nc.Turn.v = 1;
                }
            }
            
            else if (Abs(nc.GPlane.v) == 19 && PT_.X != nc.X.v)
            {
                nc.Turn.v = 0;
                nc.Turn.Show();

                if(PT_.Y == nc.Y.v && PT_.Z == nc.Z.v){
                    nc.Turn.v = 1;
                }
            }

            if(Abs(nc.GPlane.v) != 19){
                if(nc.GPolarOrCyl.v == 1){
                    nc.XC_.v = cmd.EP.X * Cos(nc.RotC.v) - cmd.EP.Y * Sin(nc.RotC.v);
                }
                else nc.XC_.v = cmd.EP.X * xScale;
                nc.XC_.Show();
            }

            if(Abs(nc.GPlane.v) != 18){
                if(nc.GPolarOrCyl.v == 1){
                    nc.YC_.v = cmd.EP.Y * Cos(nc.RotC.v) - cmd.EP.X * Sin(nc.RotC.v);
                }
                else nc.YC_.v = cmd.EP.Y;
                nc.YC_.Show();
            }

            if(Abs(nc.GPlane.v) != 17){
                nc.ZC_.v = cmd.EP.Z;
                nc.ZC_.Show();
            }

            nc.Block.Out();

            //current coordinates
            PT_ = new TInp3DPoint(nc.X.v,nc.Y.v,nc.Z.v);
        }

        public override void OnAxesBrake(ICLDAxesBrakeCommand cmd, CLDArray cld)
        {
            foreach(CLDAxisBrake ax in cmd.Axes) 
            {
                if (ax.IsC) 
                {
                    if (ax.StateIsOn)
                    {
                        if(nc.MTorm.v != 10) //&& nc.C.v == MaxReal 
                        {
                            nc.C.v = 0;
                            nc.Block.Out();
                        }

                        nc.MTorm.v = 10;
                    }
                        
                    else nc.MTorm.v = 11;
                }

                else if (ax.IsC2) 
                {
                    if (ax.StateIsOn)
                    {
                        if(nc.MTorm2.v != 10) //&& nc.C.v == MaxReal 
                        {
                            nc.C2.v = 0;
                            nc.Block.Out();
                        }

                        nc.MTorm2.v = 10;
                    }
                        
                    else nc.MTorm2.v = 11;
                }
            }

            nc.Block.Out();
        }

        public override void OnDelay(ICLDDelayCommand cmd, CLDArray cld)
        {
            nc.Block.Out();

            nc.GPause.v = 4;
            nc.FPause.v = cmd.TimeSpan;
            nc.Block.Show(nc.GPause, nc.FPause);

            nc.Block.Out();
        }
        
        public override void OnFilterString(ref string s, TNCFile ncFile, INCLabel label)
        {

            // if (!NCFiles.OutputDisabled) 
            //     Debug.Write(s);
        }
        
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

    }
}
