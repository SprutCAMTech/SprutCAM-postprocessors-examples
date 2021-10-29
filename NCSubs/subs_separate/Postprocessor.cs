using System;
using System.Text;
using System.Diagnostics;
using System.IO;
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

        ///<summary>Main nc-file</summary>
        NCFile ncMain;
 
        #endregion

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            ncMain = nc;
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgNumber = Settings.Params.Int["OutFiles.NCProgNumber"];

            nc.WriteLine("%");
            nc.WriteLine("O" + Str(nc.ProgNumber));

            nc.Block.Show(nc.BlockN, nc.GWCS, nc.GInterp);
            nc.Block.Out();
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
            nc.Output("M30");
        }

        public override void OnCallNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            cldSub.Tag = ncMain.ProgNumber + cldSub.SubCode;
            nc.WriteLine($"M98 P{cldSub.Tag}");
            if (!cldSub.Translated) {
                cldSub.Translate();
            } else {
                NCFiles.DisableOutput();
                cldSub.Translate();
                NCFiles.EnableOutput();
            }
        }

        public override void OnStartNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc = new NCFile();
            nc.ProgNumber = cldSub.Tag;
            string path = Path.GetDirectoryName(ncMain.OutputFileName);
            string name = Path.GetFileNameWithoutExtension(ncMain.OutputFileName);
            string ext = Path.GetExtension(ncMain.OutputFileName);
            nc.OutputFileName = Path.Combine(path, name + "_sub_" + Str(nc.ProgNumber) + ext);
            nc.WriteLine("%");
            nc.WriteLine("O" + Str(nc.ProgNumber));
        }

        public override void OnFinishNCSub(ICLDSub cldSub, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine("M99");
            nc = ncMain;
            nc.Block.Reset(nc.X, nc.Y, nc.Z, nc.F, nc.GInterp);
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            // One empty line between operations 
            nc.WriteLine();
        }  

        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {
            nc.WriteLine("( " + cmd.CLDataS + " )");
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
