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

            //currentOperationType = (OpType)(int)cld[60];
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
                    nc.A.v = cmd.EN.A;
                    nc.B.v = cmd.EN.B;
                    nc.C.v = cmd.EN.C;
                }
                nc.X.v = cmd.EP.X;
                nc.Y.v = cmd.EP.Y;
                nc.Z.v = cmd.EP.Z;
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
        private int ChangeGPlane(double cld14) => cld14 switch
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
            var newGplane = ChangeGPlane(cld[14]);

            if (newGplane != 0) nc.GPlane.v = newGplane;
            else Debug.WriteLine("Wrong given a plane of processing");
        }

        //or (CUTCOM)
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
        
        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {
            base.OnRapid(cmd, cld);
        }

        //(AbsMov in postprocessor generator)
        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            //я не шарю, углы в радианах или в градусах в cld
            Func<double,double> cosecant = (c => (1 / Math.Sin(c)));
            Func<double,double> secant = (d => (1 / Math.Cos(d)));

            if (nc.GInterp.v > 1 && nc.GInterp.v < 4) nc.GInterp.v = 1;
            if (nc.GInterp.v == 33) nc.Block.Show(nc.F, nc.GInterp);
            if (nc.GPolarOrCyl.v == 1)
            {
                nc.X.v = cmd.EP.X * cosecant(nc.C.v) - cmd.EP.Y * secant(nc.C.v);
                nc.Y.v = cmd.EP.Y * cosecant(nc.C.v) + cmd.EP.X * secant(nc.C.v);
            }

            else
            {
                nc.X.v = cmd.EP.X * xScale; //xScale = 2
                nc.Y.v = cmd.EP.Y;
            }

            nc.Z.v = cmd.EP.Z;
            if(currentOperationType == OpType.Lathe )
            {
                nc.X.v = cmd.EP.Y;
            }
            
            nc.Block.Out();
            nc.LastP = cmd.EP;
            // if (nc.GInterp.v==32) {
            //     nc.Block.Show(nc.F, nc.GInterp);
            // } else if (nc.GInterp > 1)
            //     nc.GInterp.v = 1;
            // nc.X.v = xScale*cmd.EP.X;
            // nc.Y.v = cmd.EP.Y;
            // nc.Z.v = cmd.EP.Z;
            // if (!cycleIsOn) {
            //     nc.Block.Out();
            // }
            // nc.LastP = cmd.EP;
        }

        //(RAPID)
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
