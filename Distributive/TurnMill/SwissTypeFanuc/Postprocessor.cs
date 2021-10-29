using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using static SprutTechnology.STDefLib.STDef;
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
        TSyncPoints SynchPoints;

        ///<summary>G81-G89 cycle is on</summary>
        bool cycleIsOn = false;

        ///<summary>Current plane sign +1 or -1</summary>
        int planeSign = 1;

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
                    tools.Add(op.Tool.Number, op.Tool.Caption);
            }            
            nc.WriteLine("( Tools list )");
            NumericNCWord toolNum = new NumericNCWord("T{0000}", 0);
            for (int i=0; i<tools.Count; i++){
                toolNum.v = Convert.ToInt32(tools.GetKey(i));
                nc.WriteLine(String.Format("( {0}    {1} )", toolNum.ToString(), tools.GetByIndex(i)));
            }
        }

        void InitNCFile(ref NCFile nc, bool isSecondChannel)
        {
            NumericNCWord oNum = new NumericNCWord("{0000}", 0);

            nc = new NCFile();
            nc.ProgNumber = Settings.Params.Int["OutFiles.NCProgNumber"];
            nc.ProgName = oNum.ToString(nc.ProgNumber);
            if (isSecondChannel)
                nc.OutputFileName = Settings.Params.Str["OutFiles.NCFilePath"] + @"\O" + nc.ProgName + ".P-2";
            else 
                nc.OutputFileName = Settings.Params.Str["OutFiles.NCFilePath"] + @"\O" + nc.ProgName;
            nc.BlockN.Disable();
            nc.Block.WordsSeparator = " ";
            nc.WriteLine("%");
            nc.WriteLine("O" + nc.ProgName + " (" + CLDProject.ProjectName + ")");

        } 

        int DetectChannel()
        {
            int channel;
            var spindleID = CurrentOperation.WorkpieceConnectorID;
            if (spindleID.Contains("1"))
                channel = 0;
            else
                channel = 1;
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
            InitNCFile(ref nc1, false);
            InitNCFile(ref nc2, true);
  
            nc1.WriteLine("G99G80G40T0");
            nc1.WriteLine("M9" + nc1.ProgName);
  
            nc1.WriteLine("M7 ");
            nc1.WriteLine("M11");
            nc1.WriteLine("G4P200 ");
            nc1.WriteLine("G300Z25.X-2.T0131");
            nc1.WriteLine("G150Z9.8 ");
            nc1.WriteLine("M10");
            nc1.WriteLine("G04P200");
            nc1.WriteLine("G0G99Z-0.2 ");
            nc1.WriteLine("G40S1000M13");
            nc1.WriteLine("T0 ");
            nc1.WriteLine("M500 ");
            nc1.WriteLine("M01");

            nc2.WriteLine("G0G40G80G99");
            nc2.WriteLine("T0 ");
            nc2.WriteLine("G310Z175T2100");
            nc2.WriteLine("M500 ");

            nc = nc1;

            nc.WriteLine();

            PrintAllTools();
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
            nc1.WriteLine("M599");
            nc1.WriteLine("M30");
            nc1.WriteLine("%");
            nc2.WriteLine("M599");
            nc2.WriteLine("M30");
            nc2.WriteLine("%");
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            // One empty line between operations if the operation has a new tool 
            nc1.WriteLine();
            nc1.OutWithN("N" + opNCounter + "( " + Transliterate(op.CLDFile.Caption) + " )");

            nc2.WriteLine();
            nc2.OutWithN("N" + opNCounter + "( " + Transliterate(op.CLDFile.Caption) + " )");
            opNCounter = opNCounter + 10;

            SwitchActiveChannel(DetectChannel());

            currentOperationType = (OpType)(int)cld[60];

            if (currentOperationType==OpType.Lathe)
                xScale = 2.0;
            else                
                xScale = 1.0;

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
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine("M01");
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
            nc = new NCFile();
            nc.ProgNumber = cldSub.Tag;
            string path = Path.GetDirectoryName(nc1.OutputFileName);
            string name = Path.GetFileNameWithoutExtension(nc1.OutputFileName);
            string ext = Path.GetExtension(nc1.OutputFileName);
            nc.OutputFileName = Path.Combine(path, name + "_sub_" + Str(nc.ProgNumber) + ext);
            nc.WriteLine("%");
            nc.WriteLine("O" + Str(nc.ProgNumber));
        }

        public override void OnFinishNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            nc.M.Show(99);
            nc.Block.Out();
            nc = nc1;
            nc.Block.Reset(nc.X, nc.Y, nc.Z, nc.C, nc.F, nc.GInterp);
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld)
        {
            nc.GPlane.Hide(cmd.PlaneGCode);
            // planeSign = cmd.PlaneSign;
            // switch (cmd.Plane) {
            //     case CLDPlaneType.XY: 
            //     case CLDPlaneType.InvXY:
            //         planeZIndex = 3;
            //         nc.PlaneZReg = nc.Z;
            //         break;
            //     case CLDPlaneType.ZX: 
            //     case CLDPlaneType.InvZX:
            //         planeZIndex = 2;
            //         nc.PlaneZReg = nc.Y;
            //         break;
            //     case CLDPlaneType.YZ: 
            //     case CLDPlaneType.InvYZ:
            //         planeZIndex = 1;
            //         nc.PlaneZReg = nc.X;
            //         break;
            // }
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
            if (!(cmd.IsOperationName || cmd.IsToolName)) {
                nc.OutWithN("( " + Transliterate(cmd.CLDataS) + " )");
            }
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
            foreach(CLDMultiMotionAxis ax in cmd.Axes) {
                if (ax.IsX) {
                    nc.X.v = xScale*ax.Value;
                    nc.X.Marked = nc.X.ValuesDiffer;
                } else if (ax.IsY) {
                    nc.Y.v = ax.Value;
                    nc.Y.Marked = nc.Y.ValuesDiffer;
                } else if (ax.IsZ) {
                    nc.Z.v = ax.Value;
                    nc.Z.Marked = nc.Z.ValuesDiffer;
                } else if (ax.IsC1) { 
                    nc.C.v = ax.Value;
                    nc.C.Marked = nc.C.ValuesDiffer;
                } else if (ax.IsC2) { 
                    nc.C.v = ax.Value;
                    nc.C.Marked = nc.C.ValuesDiffer;
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
                if (ax.IsC1) {
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
                planeSign = 1;
            } else {
                planeZIndex = 3;
                nc.PlaneZReg = nc.Z;
                if (activeChannel==0)
                    planeSign = -1;
                else
                    planeSign = 1;
            }
        }

        public override void OnHoleExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                cycleIsOn = true;
                nc.GCycle.Reset(80);
            } else if (cmd.IsOff) {
                nc.GCycle.v = 80;
                nc.Block.Out();
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
            SynchPoints.Add(cmd.PointIndex);
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
