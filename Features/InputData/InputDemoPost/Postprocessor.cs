using System;
using System.Text;
using System.Diagnostics;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.VecMatrLib.VML;

namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        ///<summary>Main nc-programm number</summary>
        public int ProgNumber {get; set;}

        public override void OnInit()
        {
        //     this.TextEncoding = Encoding.GetEncoding("windows-1251");
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition

        ///<summary>Current nc-file</summary>
        NCFile nc;
 
        #endregion

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgNumber = Settings.Params.Int["OutFiles.NCProgNumber"];

            nc.WriteLine("%");
            nc.WriteLine("O" + Str(nc.ProgNumber));

            nc.WriteLine("( Author = " + Settings.Params.Str["InputParams.ProjectAuthor"] + " )");
            nc.WriteLine("( Project = " + Settings.Params.Str["InputParams.ProjectID"] + " )");

            if (Settings.Params.Bol["InputParams.ShowMySettings"])
                ShowWin();

            nc.Block.Show(nc.BlockN, nc.GWCS, nc.GInterp);
            nc.Block.Out();
        }

        void ShowWin() {
            // Simplest case
            // double p1 = 10.5;
            // InputBox("Parameter 1", ref p1);

            // Complex case
            var win = CreateInputBox();
            win.WindowCaption = "Define settings";
            win.StartGroup("Group 1");
            double p1 = 0;
            win.AddDoubleProp("Parameter 1", 10.5, v=>p1=v);
            string p2 = "";
            win.AddStringProp("Parameter 2", "str 1", v=>p2=v, "str 1", "str 2", "str 3");
            win.CloseGroup();
            bool needBrake = false;
            win.AddBooleanProp("Break translation", needBrake, v=>needBrake=v);
            win.Show();
            if (needBrake) {
                Log.Warning("Cycle doesn't support - translation terminated");
                // nc.WriteLine("Early terminated...");
                BreakTranslation();
            }

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
        }  

        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {
            nc.WriteLine("( " + cmd.Text + " )");
        }

        public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            nc.GWCS.v = cmd.CSNumber;
        }

        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            if (cmd.IsRapid) {
                nc.GInterp.v = 0;
            } else {
                if (nc.GInterp == 0)
                    nc.GInterp.v = 1;
                nc.F.v = cmd.FeedValue;
            }
        }

        public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
        {
            
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            if (nc.GInterp > 1)
                nc.GInterp.v = 1;
            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            nc.Block.Out();
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            if (nc.GInterp > 1)
                nc.GInterp.v = 1;
            foreach(CLDMultiMotionAxis ax in cmd.Axes) {
                if (ax.IsX) 
                    nc.X.v = ax.Value;
                else if (ax.IsY) 
                    nc.Y.v = ax.Value;
                else if (ax.IsZ) 
                    nc.Z.v = ax.Value;
            }
            nc.Block.Out();
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            nc.GInterp.v = cmd.Dir;
            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            nc.R.Show(cmd.RIso);
            nc.Block.Out();
        }

        public override void OnFinalizeNCFiles(TNCFilesManager ncFiles) {
            if (!Settings.Params.Bol["InputParams.StartExtUtil"])
                return;
            string args = "";
            for (int i=0; i<ncFiles.FileCount; i++) {
                // File.Copy(ncFiles[i].OutputFileName, @"//RobotCNC/file.txt");
                args = args + " \"" + ncFiles[i].OutputFileName + "\"";
            }

            Process.Start(@"c:\Program Files (x86)\Notepad++\notepad++.exe", args);
        }
        
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

    }
}
