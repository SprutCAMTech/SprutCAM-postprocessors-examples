using System;
using System.Text;
using System.Diagnostics;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.VecMatrLib.VML;
using System.Collections.Generic;

namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        public string ProgName;

        public override void OnInit()
        {
        //     this.TextEncoding = Encoding.GetEncoding("windows-1251");
        }

        public void OutText(string text)
        {
            Text.Show(text);
            TextBlock.Out();
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition

        ///<summary>Current nc-file</summary>
        public NCFile nc;
        private HeidenhainCycle cycle;
        
        ///<summary>Number of a first tool</summary>
        int firstToolNumber = -1; 

        double CurrentWCS = 54;

        #endregion

        void PrintAllTools()
        {
            SortedList<int, string> list = new SortedList<int, string>();
            
            for (int i=0; i<CLDProject.Operations.Count; i++) {
                var op = CLDProject.Operations[i];
                if ((op.Tool != null) && (op.Tool.Command != null))
                    list.TryAdd(op.Tool.Number, Transliterate(op.Tool.Caption).ToUpper());
            }

            nc.OutText($"; ----- TOOLS LIST ----- ;");
            foreach (var tl in list) {
                nc.OutText($"; T{tl.Key} {tl.Value} ;");
            }
            nc.OutText($"; ----- TOOLS LIST ----- ;");
        }

        void PrintBlkForm()
        {
            if (CLDProject.Operations.Count<1)
                return;
            var op = CLDProject.Operations[0];
            var cmd = op.PPFunCommand;
            nc.OutText($"BLK FORM 0.1 Z {nc.X.ToString(cmd.CLD[13])} {nc.Y.ToString(cmd.CLD[14])} {nc.Z.ToString(cmd.CLD[15])}");
            nc.OutText($"BLK FORM 0.2 {nc.X.ToString(cmd.CLD[16])} {nc.Y.ToString(cmd.CLD[17])} {nc.Z.ToString(cmd.CLD[18])}");
        }

        public override void OnStartProject(ICLDProject prj)
        {
            cycle = new HeidenhainCycle(this);
            
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgName = Settings.Params.Str["OutFiles.NCProgName"];
            if (String.IsNullOrEmpty(nc.ProgName))
                nc.ProgName = prj.ProjectName;

            nc.OutText($"BEGIN PGM {nc.ProgName} MM");
            nc.OutText($";                       ;");
            nc.OutText($"; GENERATED BY SprutCAM ;");
            nc.OutText($"; DATE: {CurDate()}      ;");
            nc.OutText($"; TIME: {CurTime()}        ;");
            nc.OutText($";                       ;");

            PrintAllTools();
            PrintBlkForm();

        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.OutText("M30");
            CLDSub.Translate();

            nc.Block.Out();
            nc.OutText($"END PGM {nc.ProgName} MM");
        }

        public override void OnStartNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            //433 LBL101
            cldSub.Tag = 100 + cldSub.SubCode;
            nc.WriteLine();
            nc.OutText($"LBL{cldSub.Tag}");
        }

        public override void OnFinishNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            // 454 LBL0
            nc.Block.Out();
            nc.OutText("LBL0");
        }

        public override void OnCallNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            // 25 ;Top plane
            // 26 CALL LBL101
            cldSub.Tag = 100 + cldSub.SubCode;
            nc.OutText($"CALL LBL{cldSub.Tag}");
            nc.Block.Reset(nc.X, nc.Y, nc.Z, nc.Feed, nc.MoveType, nc.RCompens, nc.S, nc.MCoolant, nc.MSpindle);
            NCFiles.DisableOutput();
            cldSub.Translate(false);
            NCFiles.EnableOutput();
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine();
            nc.OutText("; " + Transliterate(op.Comment));

            // 17 TOOL CALL 1 Z S159 DL+0 DR+0
            // 18 TOOL DEF 2
            // 19 L Z+0 FMAX M91            
            if (op.Tool.Command!=null) {
                string plane = "Z";
                if (op.Tool.Command.Plane==CLDPlaneType.ZX)
                    plane = "Y";
                else if (op.Tool.Command.Plane==CLDPlaneType.YZ)
                    plane = "X";
                double s = 0;
                if (op.SpindleCommand!=null)
                    s = Abs(op.SpindleCommand.RPMValue);
                nc.OutText($"TOOL CALL {op.Tool.Number} {plane} {nc.S.ToString(s)} DL+0 DR+0");
                int nextTool = -1;
                var ntl = op.Tool.NextTool;
                while (ntl!=null) {
                    if (ntl.Command != null)
                        nextTool = ntl.Number;
                    if ((nextTool>=0) && (nextTool != op.Tool.Number))
                        break;
                    ntl = ntl.NextTool;
                }
                if (nextTool<0)
                    nextTool = firstToolNumber;
                if (nextTool>0)
                    nc.OutText($"TOOL DEF {nextTool}");
                if (firstToolNumber<0)
                    firstToolNumber = op.Tool.Number;
                nc.OutText("L Z+0 FMAX M91");
            }

        }

        public override void OnInterp5x(ICLDInterpolationCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                nc.OutText("M128");
            } else {
                nc.OutText("M129");
            }
        }

        public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            // Output "CYCL DEF 247 DATUM SETTING ~"
            // Output "   Q339="+Str(DatumNumber)+" ;DATUM NUMBER"
            bool shouldOut = Settings.Params.Bol["OutFiles.OutWCS"] || (CurrentWCS != cmd.CSNumber);
            CurrentWCS = cmd.CSNumber;
            if (shouldOut) {
                nc.OutText($"CYCL DEF 247 DATUM SETTING ~");
                nc.OutText($"   Q339={CurrentWCS} ;DATUM NUMBER");
            }   
            nc.Block.Reset(nc.X, nc.Y, nc.Z);
        }

        public override void OnLocalCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            if (Settings.Params.Bol["OutFiles.UseCycle19"]) {
                if (cmd.IsOn) {
                    nc.OutText($"CYCL DEF 19.0 WORKING PLANE");
                    nc.OutText($"CYCL DEF 19.1 A{nc.Number.ToString(cmd.Flt["WCS.RotAngles.A"])}");
                    nc.OutText($"              B{nc.Number.ToString(cmd.Flt["WCS.RotAngles.B"])}");
                    nc.OutText($"              C{nc.Number.ToString(cmd.Flt["WCS.RotAngles.C"])}");
                } else {
                    nc.OutText($"CYCL DEF 19.0 WORKING PLANE");
                    nc.OutText($"CYCL DEF 19.1 A+0 B+0 C+0");
                }
            } else {

            }
        }

        public override void OnRadiusCompensation(ICLDCutComCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                if (cmd.IsRightDirection)
                    nc.RCompens.Hide("R");
                else
                    nc.RCompens.Hide("L");
            } else
                nc.RCompens.Hide("0");
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn) {
                if (cmd.IsClockwiseDir)
                    nc.MSpindle.Hide(3);
                else
                    nc.MSpindle.Hide(4);
            } else if (cmd.IsOff) {
                nc.MSpindle.Show(5);
                nc.Block.Out();
            } else if (cmd.IsOrient) {
                nc.OutText($"CYCL DEF 13.0 ORIENTATION");
                nc.OutText($"CYCL DEF 13.1 ANGLE " + nc.Number.ToString(cld[2]));
                nc.OutText($"L M19");
            }
        }

        public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
        {
            if (cmd.IsOn)
                nc.MCoolant.Hide(8);
            else {
                nc.MCoolant.Show(9);
                nc.Block.Out();
            }
        }

        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            if (cmd.IsRapid)
                nc.Feed.Hide("MAX");
            else 
                nc.Feed.Hide(Round(cmd.FeedValue).ToString());
        }

        public void OutLineMove(bool force = false)
        {
            if (nc.X.Changed || nc.Y.Changed || nc.Z.Changed || nc.A.Changed || nc.B.Changed || nc.C.Changed) {
                nc.MoveType.Show("L");
                if (SameText(nc.Feed.v, "MAX"))
                    nc.Feed.Show();
                else
                    nc.Feed.UpdateState();
                nc.MCoolant.UpdateState();    
                nc.RCompens.Show();
                nc.MSpindle.UpdateState();
                nc.Block.Out();            
            } else if (force)
                nc.Block.Out();            
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            if (!cycle.Started)
                OutLineMove();
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            // 21 L B+0 C+0 R0 FMAX M3
            // 22 L X+40.99 Y+270.997 R0 FMAX
            // 24 L Z+0 R0 F200 M8
            foreach (CLDMultiMotionAxis ax in cmd.Axes) {
                if (ax.IsX)
                    nc.X.v = ax.Value;
                else if (ax.IsY)
                    nc.Y.v = ax.Value;
                else if (ax.IsZ)
                    nc.Z.v = ax.Value;
                else if (ax.IsA)
                    nc.A.v = ax.Value;
                else if (ax.IsB)
                    nc.B.v = ax.Value;
                else if (ax.IsC)
                    nc.C.v = ax.Value;
            }
            if (!cycle.Started)
                OutLineMove();
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            foreach (CLDMultiMotionAxis ax in cmd.Axes) {
                if (ax.IsX)
                    nc.X.Show(ax.Value);
                else if (ax.IsY)
                    nc.Y.Show(ax.Value);
                else if (ax.IsZ)
                    nc.Z.Show(ax.Value);
                else if (ax.IsA)
                    nc.A.Show(ax.Value);
                else if (ax.IsB)
                    nc.B.Show(ax.Value);
                else if (ax.IsC)
                    nc.C.Show(ax.Value);
            }
            nc.MoveType.Show("L");
            nc.MPhysic.Show();
            nc.Feed.Show("MAX");
            nc.Block.Out();
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            if (IsEqD(cmd.Ang, 360, Zero) || (!IsZeroD(cmd.HelixAng, Zero))) {
                // Center output
                // 82 CC X+149.994 Y-229.994
                // 83 CP IPA+360 IZ+0 DR+ F200
                switch (Abs(cmd.Plane))  
                {   
                    case 17:
                        nc.OutText($"CC {nc.X.ToString(cmd.Center.X)} {nc.Y.ToString(cmd.Center.Y)}");
                        nc.IZ.Show(cmd.EP.Z - cmd.SP.Z);    
                        break;
                    case 18:
                        nc.OutText($"CC {nc.X.ToString(cmd.Center.X)} {nc.Z.ToString(cmd.Center.Z)}");
                        nc.IZ.Show(cmd.EP.Y - cmd.SP.Y);    
                        break;
                    case 19:
                        nc.OutText($"CC {nc.Y.ToString(cmd.Center.Y)} {nc.Z.ToString(cmd.Center.Z)}");
                        nc.IZ.Show(cmd.EP.X - cmd.SP.X);    
                        break;
                }
                if (cmd.R>0)
                    nc.IPA.Show(cmd.Ang);
                else
                    nc.IPA.Show(-cmd.Ang);
                nc.MoveType.Show("CP");
            } else {
                // Radius output
                // 84 CR X+112.22 Y-233.85 R+36.363 DR+  
                nc.X.Hide(cmd.EP.X);
                nc.Y.Hide(cmd.EP.Y);
                nc.Z.Hide(cmd.EP.Z);
                switch (Abs(cmd.Plane))  
                {   
                    case 17:
                        nc.Block.Show(nc.X, nc.Y);
                        break;
                    case 18:
                        nc.Block.Show(nc.Z, nc.X);
                        break;
                    case 19:
                        nc.Block.Show(nc.Z, nc.Y);
                        break;
                }
                nc.R.Show(cmd.RIso);
                nc.MoveType.Show("CR");
            }
            if (cmd.R>0)
                nc.DR.Show("+");
            else
                nc.DR.Show("-");
            nc.Feed.UpdateState();
            nc.MCoolant.UpdateState();    
            nc.MSpindle.UpdateState();
            if (!SameText(nc.RCompens.v, "0"))
                nc.RCompens.Show();
            nc.Block.Out();
        }

        public override void OnHoleExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            if (cmd.IsCall) {
                switch (cld[2]) {
                    case 481:
                    case 482:  // W5DDrill, W5DFace
                        cycle.CycleDef(200, "Drilling");                        
                        cycle.AddQ(200, cld[7]-cld[6], "SETUP CLEARANCE");
                        cycle.AddQ(201, cld[7]-cld[8], "Depth");
                        cycle.AddQ(206, cld[10], "Feed rate for plunging");
                        cycle.AddQ(202, cld[8]-cld[7], "Plunging depth");
                        cycle.AddQ(210, 0, "Dwell time at top");
                        cycle.AddQ(203, nc.Z.v-cld[7], "Surface coordinate");
                        cycle.AddQ(204, cld[7], "2nd setup clearance");
                        cycle.AddQ(211, cld[15], "Dwell time at depth");
                        cycle.Call();
                        break;
                    case 483: // W5DChipRemoving
                        cycle.CycleDef(200, "Drilling");
                        cycle.AddQ(200, cld[7]-cld[6], "SETUP CLEARANCE");
                        cycle.AddQ(201, cld[7]-cld[8], "Depth");
                        cycle.AddQ(206, cld[10], "Feed rate for plunging");
                        cycle.AddQ(202, cld[17], "Plunging depth");
                        cycle.AddQ(210, cld[16], "Dwell time at top");
                        cycle.AddQ(203, nc.Z.v-cld[7], "Surface coordinate");
                        cycle.AddQ(204, cld[7], "2nd setup clearance");
                        cycle.AddQ(211, cld[15], "Dwell time at depth");
                        cycle.Call();
                        break;
                    case 473: // W5DChipBreaking
                        cycle.CycleDef(205, "UNIVERSAL PECKING~");
                        cycle.AddQ(200, cld[7]-cld[6], "SETUP CLEARANCE");
                        cycle.AddQ(201, cld[7]-cld[8], "DEPTH");
                        cycle.AddQ(206, cld[10], "FEED RATE FOR PLNGNG");
                        cycle.AddQ(202, cld[17], "PLUNGING DEPTH");
                        cycle.AddQ(203, nc.Z.v-cld[7], "SURFACE COORDINATE");
                        cycle.AddQ(204, cld[7], "2ND SETUP CLEARANCE");
                        cycle.AddQ(212, cld[18], "DECREMENT");
                        if (cld[18]!=0) {
                            var MinDepth = CurrentOperation.Props.Flt["ChipBreaking.DepthDegression.MinStepPercent"];
                            MinDepth = 0.01*MinDepth*cld[17];
                            cycle.AddQ(205, MinDepth, "MIN. PLUNGING DEPTH");
                        } else
                            cycle.AddQ(205, 0, "MIN. PLUNGING DEPTH");
                        cycle.AddQ(258, 0.2, "UPPER ADV STOP DIST");
                        cycle.AddQ(259, 0.2, "LOWER ADV STOP DIST");
                        cycle.AddQ(257, cld[8]-cld[7], "DEPTH FOR CHIP BRKNG");
                        cycle.AddQ(256, cld[19], "DIST. FOR CHIP BRKNG");
                        cycle.AddQ(211, cld[15], "DWELL TIME AT DEPTH");
                        cycle.AddQ(379, 0, "STARTING POINT");
                        cycle.AddQ(253, cld[12], "F PRE-POSITIONING");
                        cycle.Call();
                        break;
                }                
            } else if (cmd.IsOff) {
                cycle.Clear();
            }
        }

        public override void OnDelay(ICLDDelayCommand cmd, CLDArray cld)
        {
            nc.OutText($"CYCL DEF 9.0 DWELL TIME");
            nc.OutText($"CYCL DEF 9.1 DWELL {nc.Number.ToString(cmd.TimeSpan)}");
        }

        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            nc.WriteLine(cmd.Text);
        }

        public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
        {
            nc.OutText("M1");
        }

        public override void OnStop(ICLDStopCommand cmd, CLDArray cld)
        {
            nc.OutText("M0");
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

    }
}
