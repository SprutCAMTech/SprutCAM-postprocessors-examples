namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile : TTextNCFile
    {
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;
    }

    public partial class Postprocessor : TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        NCFile nc;

        double MaxReal = 999999;
        /// <summary>Previous lower point X coordinate</summary>
        double Fp1X;
        /// <summary>Previous lower point Y coordinate</summary>
        double Fp1Y;
        /// <summary>Previous lower point Z coordinate</summary>
        double Fp1Z;
        /// <summary>Previous upper point X coordinate</summary>
        double Fp2X;
        /// <summary>Previous upper point Y coordinate</summary>
        double Fp2Y;
        /// <summary>Previous upper point Z coordinate</summary>
        double Fp2Z;
        /// <summary>1 - wire inserted, 0 - wire breaked</summary>
        int WireInserted;
        /// <summary>1 - need output cutting coditions</summary>
        int ConditionsNeedOut;
        /// <summary>1 - need output compensation mode</summary>
        int CompensNeedOut;
        /// <summary>Displacement of the local coordinate system on the global coordinate system along the X-axis</summary>
        double LCSX;
        /// <summary>Displacement of the local coordinate system on the global coordinate system along the Y-axis</summary>
        double LCSY;
        /// <summary>Displacement of the local coordinate system on the global coordinate system along the Z-axis</summary>
        double LCSZ;


        #endregion

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];

            var win = CreateInputBox();
            win.AddIntegerProp("Input program number", 1, value => nc.ProgN.v = value);
            win.Show();
            nc.ProgN.v0 = MaxReal;
            nc.Block.Out();

            nc.GAbsInc.v = 90;
            nc.GAbsInc.v0 = MaxReal;
            nc.GCS.v = 92;
            nc.GCS.v0 = MaxReal;
            nc.X1.v0 = MaxReal;
            nc.Y1.v0 = MaxReal;
            nc.Block.Out();
        }

        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {
            var outStr = nc.Block.Form();
            nc.Output(outStr + $"({cmd.CLDataS})");
        }

        public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
        {
            if (cld[2] != 23)
                return;

            if (cld[1] == 71)
            {
                nc.GCompens.v = cld[10] == 24 ? 42 : 41;
                nc.H.v = nc.H.v0 = cld[3];
            }
            else
            {
                nc.GCompens.v = 40;
            }
            nc.GCompens.v0 = nc.GCompens.v;
            CompensNeedOut = 1;
        }

        public override void StopOnCLData()
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }
    }
}
