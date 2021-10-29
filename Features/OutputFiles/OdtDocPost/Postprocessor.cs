using System;
using System.Text;
using System.Diagnostics;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.VecMatrLib.VML;
using System.Threading;
using System.IO;
using Independentsoft.Office.Odf;
using Independentsoft.Office.Odf.Styles;


namespace SprutTechnology.SCPostprocessor
{

    public partial class OdtDocNCFile: TNCFile, INCBlockOwner
    {
        TextDocument doc;
        Paragraph p;

        public int ProgNumber { get; set; }

        public override void OnInit()
        {
            doc = new TextDocument();

            Font arial = new Font();
            arial.Name = "Arial";
            arial.Family = "Arial";
            arial.GenericFontFamily = GenericFontFamily.Swiss;
            arial.Pitch = FontPitch.Variable;

            doc.Fonts.Add(arial);

            ParagraphStyle style1 = new ParagraphStyle("P100");
            style1.TextProperties.Font = "Arial";
            style1.TextProperties.FontSize = new Size(16, Unit.Point);
            style1.TextProperties.FontWeight = FontWeight.Bold;

            doc.AutomaticStyles.Styles.Add(style1);

            NewParagraph();
        }

        void NewParagraph()
        {
            p = new Paragraph();
            p.Style = "P100";
            doc.Body.Add(p);
        }

        void InternalWrite(string s, bool newLine) 
        {
            Owner.FilterString(ref s, this, null);
            if (Manager.OutputDisabled)
                return;
            p.Add(s);
            if (newLine)
                NewParagraph();
        }

        public void Write(string s)
        {
            InternalWrite(s, false);
        }

        public void WriteLine(string s)
        {
            InternalWrite(s + Environment.NewLine, true);
        }        

        public void Write(string s, INCLabel label) 
        {}

        public void WriteLine(string s, INCLabel label)
        {}

        public override void SaveToFile(string fileName)
        {
            // if (File.Exists(fileName))
            //     File.Delete(fileName);
            doc.Save(fileName, true);
        } 

    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition

        ///<summary>Current nc-file</summary>
        OdtDocNCFile nc;

        #endregion

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new OdtDocNCFile();
            nc.OutputFileName = @"d:\1.odt"; //Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgNumber = Settings.Params.Int["OutFiles.NCProgNumber"];

            nc.WriteLine("%");
            nc.WriteLine("O" + Str(nc.ProgNumber));

            nc.Block.Show(nc.BlockN, nc.GWCS, nc.GInterp);
            nc.Block.Out();
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
            nc.WriteLine("M30");
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            // One empty line between operations 
            nc.WriteLine("");
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
         
            if (!NCFiles.OutputDisabled) 
                Debug.Write(s);
        }
        
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

    }
}
