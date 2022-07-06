using System;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.VecMatrLib.VML;

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

        ///<summary>Main nc-programm name (number with zeroes)</summary>
        public string ProgName {get; set;}

        ///<summary>Last point (X, Y, Z) was written to the nc-file</summary>
        public TInp3DPoint LastP {get; set;}

        ///<summary>Current plane third coordinate register Z, Y or X</summary>
        public NumericNCWord PlaneZReg;

        
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

        ///<summary>Main nc-file of first channel</summary>
        public NCFile nc1;

        ///<summary>Main nc-file of second channel</summary>
        public NCFile nc2;

        ///<summary>List of synchronization points M500-M599 to automate synchronization</summary>
        public TSyncPoints SynchPoints;

        ///<summary>G81-G89 cycle is on</summary>
        bool cycleIsOn = false;
        int[] ClampState = new int[2]{0,0};
        bool IncrementalMove = false;
        bool NeedPartEject = true;

        ///<summary>Current plane sign +1 or -1</summary>

        ///<summary>Current plane third coordinate 3, 2 or 1</summary>
        int planeZIndex = 3;

        ///<summary>Number of operation Nxxxx</summary>
        int opNCounter = 10;

        ///<summary>Number of active channel: 0 (main) or 1 (sub)</summary>
        int activeChannel = 0;
 
        ///<summary>Type of current operation (mill, lathe, etc.)</summary>
        OpType currentOperationType = OpType.Unknown;

        ///<summary>X axis scale coefficient (1 - radial, 2 - diametral)</summary>
        double xScale = 1.0;

        ///<summary>Для определения кол-ва токарных циклов в одной операции</summary>
        int CycleOpnum=0;

        #endregion

        public Postprocessor()
        {
            SynchPoints = new TSyncPoints(this);  
        }

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

        void InitNCFile(out NCFile nc, int ProgNum, bool isSecondChannel)
        {
            NumericNCWord oNum = new NumericNCWord("{0000}", 0);

            nc = new NCFile();
            nc.ProgNumber = ProgNum;
            nc.ProgName = oNum.ToString(nc.ProgNumber);
            if (isSecondChannel)
                nc.OutputFileName = Settings.Params.Str["OutFiles.NCFilePath"] + @"\O" + nc.ProgName + ".P-2";
            else 
                nc.OutputFileName = Settings.Params.Str["OutFiles.NCFilePath"] + @"\O" + nc.ProgName;
            nc.BlockN.Disable();
            nc.Block.WordsSeparator = " ";
            nc.WriteLine("%");
            nc.WriteLine("O" + nc.ProgName + " (" + Transliterate(CLDProject.ProjectName) + ")");

        } 

        int DetectChannel()
        {
            int channel;
            var Op = CurrentOperation.TypeName;
            if ((Op.Contains("TSTTurnPNP")) || (Op.Contains("TSTTakeoverMTM")))
                channel = 1;        //subspindle working uses both channels, but we assign channel 1 for the correct output of wait commands
            else
            {
                var spindleID = CurrentOperation.WorkpieceConnectorID;
                if (spindleID.Contains("MainSpindle"))
                    channel = 0;
                else
                    channel = 1;
            }
            return channel;
        }

        void SwitchActiveChannel(int newChannel)
        {
            // if (activeChannel != newChannel) {
            //     SynchPoints.Add();
            // }
            activeChannel = newChannel;
            if (activeChannel==0){
                nc = nc1;
            } else {
                nc = nc2;
            }
        }

        public override void OnStartProject(ICLDProject prj)
        {
            int ProgNum = Settings.Params.Int["OutFiles.NCProgNumber"];    
            InitNCFile(out nc1, ProgNum, false);
            InitNCFile(out nc2, ProgNum, true);
            nc = nc1;


            PrintAllTools();
            nc.WriteLine();
            nc1.WriteLine("M9" + nc1.ProgName);
            string reset = "G40 G80 G99 G18 T0";
            nc1.WriteLine(reset);
            nc2.WriteLine(reset);
            nc2.WriteLine("G310Z175T2100");
            //SynchPoints.Add();
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
            nc1.WriteLine("M36");
            nc2.WriteLine("M36");
            SynchPoints.Add();
            nc1.WriteLine("M95");
            nc1.WriteLine("/M92");
            nc1.WriteLine("M96");
            nc1.WriteLine("M97");
            nc2.WriteLine("T2100");
            nc2.WriteLine("M5");
            SynchPoints.Add();
            
            nc1.WriteLine("T0100");
            nc1.WriteLine("M30");
            nc1.WriteLine("%");
            nc2.WriteLine("M30");
            nc2.WriteLine("%");
        }
        void outPartEjection()
        {
            nc.WriteLine("G340 (eject)");
            nc.WriteLine();
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            SwitchActiveChannel(DetectChannel());
            // One empty line between operations if the operation has a new tool 
            nc.WriteLine();
            CycleOpnum=0;

            currentOperationType = (OpType)(int)cld[60];
            if (SameText(op.TypeName, "TSTPickAndPlace"))
                currentOperationType=OpType.Auxiliary;

            if (NeedPartEject)
            {
                string opt = op.TypeName;
                if (SameText(opt,"TSTPickAndPlace") || SameText(opt,"TSTTurnPNP"))
                {
                    NeedPartEject=false;
                    outPartEjection();
                }
            }

            if (currentOperationType==OpType.Lathe) {
                xScale = 2.0;
                nc.Y.v = 0;                
                nc.Y.v0 = 0;                
                nc.C.v = 0;                
                nc.C.v0 = 0;                
            } else                
                xScale = 1.0;
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
        }

        public override void OnCallNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            cldSub.Tag = nc1.ProgNumber + cldSub.SubCode;
            nc.M.Show(98);
            nc.PSubCall.Show(cldSub.Tag);
            nc.Block.Out();
            if (!cldSub.Translated)
                cldSub.Translate();
        }

        public override void OnStartNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.CycleBlockN.Show(Num(cldSub.StartCaption));
            nc.Block.Reset(nc.X,nc.Z);
            nc.GInterp.Show();
        }

        public override void OnFinishNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.WriteLine("N" + cldSub.EndCaption);
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld)
        {
            nc.GPlane.Hide(cmd.PlaneGCode);
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            int spindleGroup = 0;
            if (currentOperationType==OpType.Mill) 
                spindleGroup = 20;
            if (cmd.IsOn) {
                // Stop if spindle reverse

                if (currentOperationType==OpType.Mill) {
                    nc.WriteLine("M50");
                    nc.WriteLine("G0G28H0");
                }

                if ((cmd.IsClockwiseDir && nc.MSpindle==4) || (!cmd.IsClockwiseDir && nc.MSpindle==3)) {
                    nc.MSpindle.Show(5);
                    nc.Block.Out();
                }
                if (cmd.IsCSS) {
                    nc.WriteLine(nc.GCssRpm.ToString(50) + " " + nc.S.ToString(cmd.RPMValue));
                    nc.GCssRpm.Show(96);
                    nc.S.Show(cmd.CSSValue);
                } else {
                    nc.GCssRpm.Show(97);
                    nc.S.Show(cmd.RPMValue);
                }
                if (cmd.IsClockwiseDir)
                    nc.MSpindle.Show(spindleGroup + 3);
                else
                    nc.MSpindle.Show(spindleGroup + 4);
                nc.Block.Out();
            } else if (cmd.IsOff) {
                nc.MSpindle.v = spindleGroup + 5;
                nc.Block.Out();
                if (currentOperationType==OpType.Mill) {
                    nc.WriteLine("M51");
                }
            } else if (cmd.IsOrient) {
                nc.M.Show(19);
                nc.Block.Out();
            }
        }

        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {           
            if (cmd.IsOperationName){
                //after wait label
                var s = "N" + opNCounter + "( " + Transliterate(cmd.Caption) + " )";
                nc1.OutWithN(s);
                nc2.OutWithN(s);
                opNCounter = opNCounter + 10;
            }

            if (!(cmd.IsOperationName || cmd.IsToolName) && cmd.Index!=0) {
                nc.OutWithN("( " + Transliterate(cmd.CLDataS) + " )");
            }
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            var op = CurrentOperation;
            if (SameText(op.TypeName, "TSTBarFeeding"))
                return;

            if (op.Tool!=null && op.Tool.Command!=null) {
                nc.T.Show(op.Tool.Number);
                nc.TCor.Show(op.Tool.Command.LCorNum);
                nc.TrailingComment.v = Transliterate(op.Tool.Caption);
                nc.TrailingComment.v0 = "";
                nc.Block.WordsSeparator = "";
                nc.Block.Out();
                nc.Block.WordsSeparator = " ";
                // var s = nc.Block.Form();
                // nc.WriteLine(s + " ( " + op.Tool.Caption + " )");
                nc.Block.Reset(nc.X, nc.Y, nc.Z, nc.C, nc.GInterp, nc.F);
            }
            //base.OnLoadTool(cmd, cld);
        }

        public override void OnRadiusCompensation(ICLDCutComCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                if (cmd.IsLeftDirection)
                    nc.GRCompens.Show(41);
                else
                    nc.GRCompens.Show(42);
            } else {
                nc.GRCompens.v = 40;
            }
        }

        public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                nc.MCoolant.v = 8;
            } else {
                nc.MCoolant.v = 9;
                nc.Block.Out();
            }
        }

        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            if (cmd.IsRapid) {
                if (nc.GPolarOrCyl==12.1) {
                    nc.GInterp.v = 1;
                    nc.F.v = Settings.Params.Flt["Polar.RapidFeed"];
                } else
                    nc.GInterp.v = 0;
            } else {
                if (nc.GInterp == 0)
                    nc.GInterp.v = 1;
                nc.F.v = cmd.FeedValue;
            }
        }

        public override void OnSinglePassThread(ICLDSinglePassThreadCommand cmd, CLDArray cld)
        {
                nc.GInterp.v = 32;
                if (cmd.StepIsTPU)
                    nc.F.v = 1.0/cmd.Step;
                else
                    nc.F.v = cmd.Step;
                nc.QThreadAngle.v = cmd.StartAngle;
                nc.QThreadAngle.v0 = 0;
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            if (nc.GInterp.v==32) {
                nc.Block.Show(nc.F, nc.GInterp);
            } else if (nc.GInterp > 1)
                nc.GInterp.v = 1;
            nc.X.v = xScale*cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            if (IncrementalMove){
                if (!nc.X.Unstable)
	                nc.U.v = nc.X.v - nc.X.v0;
                if (!nc.Y.Unstable)
	                nc.V.v = nc.Y.v - nc.Y.v0;
                if (!nc.Z.Unstable)
	                nc.W.v = nc.Z.v - nc.Z.v0;
                nc.X.Hide();
                nc.Y.Hide();
                nc.Z.Hide();
            }

            if (!cycleIsOn) {
                nc.Block.Out();
            }
            nc.LastP = cmd.EP;
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            if (nc.GInterp > 1)
                nc.GInterp.v = 1;
            nc.Block.SetMarks(false);
            string XP,YP,ZP,CP;
            if (nc==nc1){
                XP = "AxisX1Pos";
                YP = "AxisY1Pos";
                ZP = "AxisZ1Pos";
                CP = "AxisC1Pos";
            }else{
                XP = "AxisX2Pos";
                YP = "AxisY2Pos";
                ZP = "AxisZ2Pos";
                CP = "AxisC2Pos";
            }
            NumericNCWord AR=null;
            NumericNCWord IR=null;
            foreach(CLDMultiMotionAxis ax in cmd.Axes) {
                AR=null;
                IR=null;
                if (SameText(ax.ID, XP)) {
                    AR = nc.X;
                    IR = nc.U;
                    nc.X.v = xScale*ax.Value;
                } else if (SameText(ax.ID, YP)) {
                    AR = nc.Y;
                    IR = nc.V;
                    nc.Y.v = ax.Value;
                } else if (SameText(ax.ID, ZP)) {
                    AR = nc.Z;
                    IR = nc.W;
                    nc.Z.v = ax.Value;
                } else if (SameText(ax.ID, CP)) { 
                    nc.C.v = ax.Value;
                }
                if (IncrementalMove && (AR!=null) && (IR!=null) && (!AR.Unstable)){
                    IR.v = AR.v - AR.v0;
                    IR.Show();
                    AR.Hide();
                }

            }
            if (!cycleIsOn) {
                nc.Block.Out();
            }
            nc.LastP = cmd.EP;
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            foreach(CLDMultiMotionAxis ax in cmd.Axes) {
                if (ax.IsX) {
                    nc.X.Show(ax.Value);
                } else if (ax.IsY) {
                    nc.Y.Show(ax.Value);
                } else if (ax.IsZ) {
                    nc.Z.Show(ax.Value);
                } else if (ax.IsC) { 
                    nc.C.Show(ax.Value);
                }
            }
            if (nc.X.Changed || nc.Y.Changed || nc.Z.Changed || nc.C.Changed) {
                nc.GHome.Show(53);
                nc.Block.Out();
            }
        }

        public bool IsLastGoHome(ICLDGoHomeCommand cmd)
        {
            return cmd.FindNextCommand(CLDCmdType.GoHome)==null;
        }

        public override void OnGoHome(ICLDGoHomeCommand cmd, CLDArray cld)
        {
            if (IsLastGoHome(cmd)) {
                if (activeChannel==0)
                    nc1.WriteLine("T0");
                else
                    nc2.WriteLine("T0 Z175");
            }
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            nc.GInterp.v = cmd.Dir;
            nc.R.Show(cmd.RIso);
            nc.X.v = xScale*cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            switch (Abs(cmd.Plane)) {
                case 17:
                    nc.X.Show();
                    nc.Y.Show();
                    break;
                case 18:
                    nc.X.Show();
                    nc.Z.Show();
                    break;
                case 19:
                    nc.Z.Show();
                    nc.Y.Show();
                    break;
            }
            nc.Block.Out();
        }

        public override void OnAxesBrake(ICLDAxesBrakeCommand cmd, CLDArray cld)
        {
            foreach(CLDAxisBrake ax in cmd.Axes) {
                if (ax.IsC1 || ax.IsC2) {
                    if (ax.StateIsOn)
                        nc.MCBrake.v = 82;
                    else
                        nc.MCBrake.v = 83;
                }
            }
            if (nc.MCBrake.Changed)
                nc.Block.Out();
        }

        void DetectHoleCyclePlane(double toolAxisZ)
        {
            if (IsZeroD(toolAxisZ, 0.001)) {
                planeZIndex = 1;
                nc.PlaneZReg = nc.X;
            } else {
                planeZIndex = 3;
                nc.PlaneZReg = nc.Z;
            }
        }

        public override void OnHoleExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                nc.Block.Out();
                cycleIsOn = true;
                nc.GCycle.Reset(80);
            } else if (cmd.IsOff) {
                if (currentOperationType==OpType.Mill){
                    nc.GCycle.v = 80;
                    nc.Block.Out();
                }
                cycleIsOn = false;
            } else if (cmd.IsCall) {
                DetectHoleCyclePlane(cld[5]);
                int sg = -cld[2+planeZIndex];
                double curPos = nc.LastP[planeZIndex];
                nc.PlaneZReg.v = curPos - cld[8]*sg;
                nc.RSafeLevel.v = curPos - cld[6]*sg;
                if (cld[9] == 0) 
                    nc.F.v = cld[10]*nc.S; 
                else 
                    nc.F.v = cld[10];
                nc.GInterp.Hide(1);
                if (currentOperationType==OpType.Lathe) {
                    switch (cmd.CycleType) {
                        case CLDConst.W5DDrill:
                        case CLDConst.W5DFace:
                            nc.GCycle.v=74;
                            nc.WriteLine(nc.GCycle+" "+nc.RSafeLevel.ToString(cld[11]));
                            nc.GCycle.Show();
                            nc.RSafeLevel.Hide();
                            nc.PlaneZReg.Show(); 
                            nc.QStep.v = cld[8];                           
                            nc.F.Show();
                            nc.Block.Out();
                        break;
                        case CLDConst.W5DChipRemoving:
                        case CLDConst.W5DChipBreaking:
                            nc.GCycle.v=74;
                            nc.WriteLine(nc.GCycle+" "+nc.RSafeLevel.ToString(cld[11]));
                            nc.GCycle.Show();
                            nc.RSafeLevel.Hide();
                            nc.PlaneZReg.Show();
                            nc.QStep.v = cld[17];                            
                            nc.F.Show();
                            nc.Block.Out();
                        break;
                        case CLDConst.W5DTap:
                            if (cld[19]==1)
                                nc.GCycle.v=84.2;
                            else
                                nc.GCycle.v=84;
                            nc.RSafeLevel.Show(cld[6]);
                            nc.PlaneZReg.Show();
                            nc.F.Show();
                            if (cld[15]!=0)
                                nc.PDrillPause.v=cld[15];
                            nc.Block.Out();    
                        break;
                        case CLDConst.W5DBore5:
                            break;
                        case CLDConst.W5DBore6:
                            break;
                        case CLDConst.W5DBore7:
                            break;
                        case CLDConst.W5DBore8:
                            break;
                        case CLDConst.W5DBore9:
                            break;
                        case CLDConst.W5DThreadMill:
                            break;
                        case CLDConst.W5DHolePocketing:
                            break;
                        case CLDConst.W5DGrooveBoring:
                            break;
                    }    
                }else{                
                    nc.GCycle.v = cmd.CycleType-400;
                    switch (cmd.CycleType) {
                        case CLDConst.W5DDrill:
                            if (nc.GCycle.Changed) {
                                nc.PlaneZReg.Show();
                                nc.RSafeLevel.Show();
                                nc.F.Show();
                            }
                            nc.Block.Out();
                            break;
                        case CLDConst.W5DFace:
                            nc.PDrillPause.v = cld[15]*1000;
                            if (nc.GCycle.Changed) {
                                nc.PlaneZReg.Show();
                                nc.RSafeLevel.Show();
                                nc.F.Show();
                                nc.PDrillPause.Show();
                            }
                            nc.Block.Out();
                            break;
                        case CLDConst.W5DChipRemoving:
                        case CLDConst.W5DChipBreaking:
                            nc.QStep.v = cld[17];
                            if (nc.GCycle.Changed) {
                                nc.PlaneZReg.Show();
                                nc.RSafeLevel.Show();
                                nc.F.Show();
                                nc.QStep.Show();
                            }
                            nc.Block.Out();
                            break;
                        case CLDConst.W5DTap:
                            break;
                        case CLDConst.W5DBore5:
                            break;
                        case CLDConst.W5DBore6:
                            break;
                        case CLDConst.W5DBore7:
                            break;
                        case CLDConst.W5DBore8:
                            break;
                        case CLDConst.W5DBore9:
                            break;
                        case CLDConst.W5DThreadMill:
                            break;
                        case CLDConst.W5DHolePocketing:
                            break;
                        case CLDConst.W5DGrooveBoring:
                            break;
                    }
                }
            }
        }
        public void outPause(double Sec, int ChannelFile=0)
        {
            NCFile n=nc;
            switch (ChannelFile){
                case 1:      
                    n=nc1;
                break;
                case 2:      
                    n=nc2;
                break;
            }    
            n.Output("G4P"+(Sec*1000).ToString());
        }
        public void outAirBlower()
        {
            nc.Output("M28 (Air)");
            outPause(0.5);
        }

        public void outClamp(bool MainSpindle, bool ClampOn)
        {
            int Mcode =10;
            int ClampIndex=0;
            string sp = "main collet";
            string st =" close";
            if (!MainSpindle) 
            {
                Mcode=20;
                sp ="sub collet";
                ClampIndex=1;
            }
            if (!ClampOn) {
                Mcode++;
                st =" open";
            };
            if (ClampState[ClampIndex]!=Mcode)
            {
                ClampState[ClampIndex] = Mcode;
                if (ClampOn) 
                   outAirBlower();
                nc.Output("M"+Mcode.ToString()+" ("+sp+st+")");
                outPause(0.2);
            }
        }
        public void OutBarFeeding(float Overhang, TInp3DPoint TouchPos, int ToolNum, int ToolCor, float SafeDist)
        {
            nc.Output("M7");
            outClamp(true,false);
            nc.WriteLine(nc.GInterp.ToString(300) + nc.Z.ToString(Overhang) + nc.X.ToString(TouchPos.X) + nc.T.ToString(ToolNum) + nc.TCor.ToString(ToolCor));
            nc.WriteLine(nc.GInterp.ToString(150) + nc.Z.ToString(0.5));
            outClamp(true,true);
            nc.WriteLine(nc.GInterp.ToString(1) + nc.GFeed.ToString(99) + nc.Z.ToString(-SafeDist));
            //nc.WriteLine(nc.GInterp.ToString(40)+ nc.S.ToString(CurrentOperation.SpindleCommand.RPMValue));
            
        }
        public void OutSubSpindleCycle(double Delta, double Feed)
        {
            SynchPoints.Add();
            nc2.WriteLine("(move Z1)");
            nc1.WriteLine(nc1.GInterp.ToString(0) + nc1.W.ToString(-Delta));
            nc1.WriteLine(nc1.GInterp.ToString(50) + nc1.W.ToString(Delta));
            SynchPoints.Add();
        }

        public override void OnPickAndPlaceExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            switch (cmd.CycleType) {
                case CLDConst.WBarFeedingCycle:
                    TInp3DPoint TouchPos = new TInp3DPoint(((float)cld[6]),((float)cld[7]),((float)cld[8]));
                    OutBarFeeding((float)cld[3],TouchPos, (int) cld[4], (int) cld[5], ((float)cld[9]));
                break;
                case CLDConst.WSubSpindleCycle:
                    double Delta = (double) cld[3]; 
                    double Feed = (double) cld[4]; 
                    OutSubSpindleCycle(Delta,Feed);
                break;
            }
        }
        public override void OnClamp(ICLDClampCommand cmd, CLDArray cld)
        {
            base.OnClamp(cmd, cld);
            bool MainSp = cmd.ClampID==1;
            outClamp(MainSp, cmd.IsOn);
        }
        
        public string GetSyncZCode(bool IsOn)
        {
            string MCode;
            if (IsOn) 
                MCode = "M221 (Sync Z1/Z2)"; 
            else 
                MCode = "M220 (Desync Z1/Z2)";
            return MCode;
        }

        public void SyncZ(bool IsOn)
        {
            string MCode;
            MCode = GetSyncZCode(IsOn);
            SynchPoints.Add();
            nc1.WriteLine(MCode);
            nc2.WriteLine(MCode);
            SynchPoints.Add();
        }
        public void SyncSpindles(bool IsOn)
        {
            if (IsOn) 
            {
              nc1.WriteLine("M56 (Sync spindle speed)");
              nc1.WriteLine("M54 (Sync phase)");
            }else 
            {
              nc1.WriteLine("M55 (desync phase)");
              nc1.WriteLine("M57 (desync speed)");
            }
        }
        public void SyncSpindlesWithWaits(bool IsOn)
        {
            SynchPoints.Add();
            SyncSpindles(IsOn);
            SynchPoints.Add();
        }
        public override void OnSyncAxes(ICLDSyncAxesCommand cmd, CLDArray cld)
        {
            if (SameText(CurrentOperation.TypeName, "TSTTakeoverMTM")) return;

            base.OnSyncAxes(cmd, cld);
            if (cmd.FirstAxisID.Contains("AxisC1") || cmd.FirstAxisID.Contains("AxisC2")){
                SyncSpindlesWithWaits(cmd.IsOn);
            }else
            if (cmd.FirstAxisID.Contains("AxisZ1") || cmd.FirstAxisID.Contains("AxisZ2")){
                SyncZ(cmd.IsOn);
            }else{
                Log.Error("Axes can not be syncronized: ("+cmd.FirstAxisID+","+cmd.SecondAxisID+")");      
            }
        }

        public override void OnTurnExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            if(cmd.IsOff)
                nc.GInterp.Reset();

            if(!cmd.IsCall)
                return;
            nc.GInterp.Hide();
            nc.Block.Out();    
            CycleOpnum+=2;
            switch (cmd.CycleType){
                case CLDConst.WLatheFinishing:
                    var sb = CLDSub[cld[3]];
                    sb.StartCaption = Str(opNCounter - 10 +CycleOpnum); 
                    sb.EndCaption = Str(opNCounter - 9+CycleOpnum);
                    var needG70 = (cld[4]==1 && cld[7]==0 && cld[8]==0);
                    if (!needG70){   
                        nc.GCycle.Hide(73);
                        nc.WriteLine(nc.GCycle+" "+nc.U.ToString(cld[6])+" "+nc.W.ToString(cld[5])+" R"+ Str(cld[4]));
                        nc.WriteLine(nc.GCycle + " P" + sb.StartCaption+" Q" + sb.EndCaption+" " + nc.U.ToString(cld[8]*2)+" "+nc.W.ToString(cld[7])+" "+nc.F);
                    }
                    if (needG70 || cld[12]==1) {
                        nc.GCycle.Hide(70);                         
                        nc.WriteLine(nc.GCycle+" P"+sb.StartCaption+" Q"+sb.EndCaption);
                    }    
                    sb.Translate();
                break;
                case CLDConst.WLatheRoughing:
                    sb = CLDSub[cld[3]];
                    sb.StartCaption = Str(opNCounter - 10 +CycleOpnum);
                    sb.EndCaption = Str(opNCounter - 9 +CycleOpnum);
                    if (cld[5]==0){
                        nc.GCycle.Hide(71);
                        nc.WriteLine(nc.GCycle+" "+nc.U.ToString(cld[4])+" R"+ Str(cld[9]));
                    }    
                    else{
                        nc.GCycle.Hide(72);
                        nc.WriteLine(nc.GCycle+" "+nc.W.ToString(cld[4])+" R"+ Str(cld[9]));
                    }    
                    nc.WriteLine(nc.GCycle + " P" + sb.StartCaption+" Q" + sb.EndCaption+" " + nc.U.ToString(cld[8]*2)+" "+nc.W.ToString(cld[7])+" "+nc.F);
             
                    if (cld[12]==1) {
                        nc.GCycle.Hide(70);                         
                        nc.WriteLine(nc.GCycle+" P"+sb.StartCaption+" Q"+sb.EndCaption);
                    }    
                    sb.Translate();
                break;
                case CLDConst.WLatheGrooving:
                    double width,depth,widthStep,depthStep;
                    if (cld[3]==0){
                        nc.GCycle.Hide(75);
                        width=cld[5]*2;
                        depth=cld[4];
                        widthStep=cld[7];
                        depthStep=cld[6];
                    }else{
                        nc.GCycle.Hide(74);
                        width=cld[4]*2;
                        depth=cld[5];
                        widthStep=cld[6];
                        depthStep=cld[7];
                    } 
                    nc.WriteLine(nc.GCycle +" " + nc.U.ToString(width) +" "+nc.W.ToString(depth) +" "+nc.P.ToString(widthStep) +" "+nc.QStep.ToString(depthStep) + " " + nc.R.ToString(cld[8])+" "+nc.F);   
                break;
                case CLDConst.WLatheThreading:
                    nc.GCycle.Hide(76);
                    string PThread;
                    double dd;
                    int i=cld[21]-1;
                    PThread=Str(cld[30]);
                    if (cld[30]<10)
                        PThread="0"+PThread;
                    dd= Round(10*(cld[14]/cld[23]));
                    if (dd<0)
                        dd=0;
                    if (dd>99)
                        dd=99;
                    if (dd<10)
                        PThread=PThread + "0" + Str(dd);
                    else
                        PThread=PThread + Str(dd);
                    if (cld[19]<10)
                        PThread=PThread + "0" + Str(cld[19]);    
                    else    
                        PThread=PThread + Str(cld[19]);
                    nc.WriteLine(nc.GCycle+" P"+PThread+" "+nc.R.ToString(cld[29]));
                    for (;i>=0;i--){
                        if (cld[5]==0)
                            nc.WriteLine(nc.GCycle+" "+nc.U.ToString(cld[9]*2-nc.X)+" "+nc.W.ToString(cld[8]-nc.Z)+" "+nc.P.ToString(cld[18])+" "+nc.QStep.ToString(cld[27])+" "+nc.F.ToString(cld[23]));
                        else
                            nc.WriteLine(nc.GCycle+" "+nc.U.ToString(cld[9]*2-nc.X)+" "+nc.W.ToString(cld[8]-nc.Z)+" "+nc.R.ToString(cld[13]-cld[9])+" "+nc.P.ToString(cld[18])+" "+nc.QStep.ToString(cld[27])+" "+nc.F.ToString(cld[23]));    
                        if (i>0){
                            nc.Z.Show(nc.Z.v + (cld[23]/cld[21]));
                            nc.GInterp.Show(1);
                            nc.Block.Out();
                        }
                    }
                break;
                case CLDConst.WLatheThreadingG92:
                    nc.GCycle.Show(92);
                    nc.Z.v=cld[8];
                    nc.X.Show(cld[7]*xScale);
                    nc.F.Show(cld[23]);
                    nc.Block.Out();
                    nc.GInterp.Reset();
                break;
            }
        }

        public override void OnStop(ICLDStopCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.M.Show(0);
            nc.Block.Out();
        }

        public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.M.Show(1);
            nc.Block.Out();
        }

        public override void OnDelay(ICLDDelayCommand cmd, CLDArray cld)
        {
            nc.GDelay.Show(4);
            nc.XDelay.Show(cmd.TimeSpan);
            nc.Block.Out();
        }

        public override void OnSyncWait(ICLDSyncWaitCommand cmd, CLDArray cld)
        {
            if (NCFiles.OutputDisabled)
                return;
            if (SameText(cmd.PointID, "Takeover2")) {
                SynchPoints.OutPrev(cmd.PointIndex);
                if (nc==nc1)
                {
                    SyncSpindles(true);
                }
            } else if (SameText(cmd.PointID, "Takeover3")) {
                SynchPoints.OutPrev(cmd.PointIndex);
                nc.WriteLine(GetSyncZCode(true));
            } else if (SameText(cmd.PointID, "Takeover4")) {
                SynchPoints.OutPrev(cmd.PointIndex);
                nc.WriteLine(GetSyncZCode(false));
                if (nc==nc1) {
                    SyncSpindles(false);
                } else {
                    IncrementalMove=true;
                }
            } else if (SameText(cmd.PointID, "Takeover5")) {
                IncrementalMove=false;
            }

            SynchPoints.Add(cmd.PointIndex);

            if ((nc==nc2) && SameText(cmd.PointID, "Takeover2")) {
                outClamp(false, false);
            }
        }

        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            nc.WriteLine(cmd.Text);
        }

        public override void OnInterpCylindrical(ICLDInterpolationCommand cmd, CLDArray cld)
        {
            
        }

        bool IsCorrectCoordinate(params double[] coords) 
        {
            bool correct = false;
            foreach (var c in coords) {
                correct = (c>-10000) && (c<10000);
                if (!correct)
                    break;
            }
            return correct;
        }

        void OutFirstPointBeforePolar(ICLDInterpolationCommand cmd)
        {
            if (IsCorrectCoordinate(nc.X.v0, nc.Z.v0))
                return;
            var mvm = cmd.FindNextCommand(CLDCmdType.Goto, 1, 20);    
            if (mvm==null) {
                Log.Error("Отсутствуют перемещения перед включением полярной интерполяции.");
                return;
            }
            ICLDMotionCommand g = mvm as ICLDMotionCommand;            
            nc.GInterp.v = 0;
            nc.X.Show(g.EP.X);
            nc.Z.Show(g.EP.Z);
            nc.Block.Out();
        }

        public override void OnInterpPolar(ICLDInterpolationCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                nc.Block.Out();
                OutFirstPointBeforePolar(cmd);
                nc.GPolarOrCyl.Show(12.1);
                nc.Y.Address = "C";
                nc.Y.Reset();
                nc.C.Hide(0);
                nc.Block.Out();
            } else {
                nc.Block.Out();
                nc.GPolarOrCyl.Show(13.1);
                nc.Y.Address = "Y";
                nc.Y.Hide(0);
                nc.Block.Out();
            }
            
        }

        public override void OnStructure(ICLDStructureCommand cmd, CLDArray cld)
        {
        }

        public override void OnFilterString(ref string s, TNCFile ncFile, INCLabel label)
        {
            SynchPoints.CheckForOutput(s);
        }
        
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

    }
}
