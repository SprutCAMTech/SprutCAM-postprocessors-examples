using System.Collections;
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

        ///<summary>Current nc-file</summary>
        NCFile nc;
 
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
            nc.Block.Show(nc.BlockN, nc.GWCS, nc.GInterp);
            nc.Block.Out();
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
