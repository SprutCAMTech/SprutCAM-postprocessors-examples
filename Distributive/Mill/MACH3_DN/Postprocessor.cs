namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        public string ProgName;
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;
    }

    

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        NCFile nc;

        #endregion

        const string SPPName = "MACH3_DN";
  
         public void OutToolList()
        {
            var diametr = CLDProject.Operations[0].Tool.Command.CLD[5];
            for(int i = 0; i < CLDProject.Operations.Count;i++)
            {
                var curtool =  CLDProject.Operations[i];
                if(curtool.Enabled)
                {
                    diametr = curtool.Tool.Command.CLD[5];
                }
                nc.Output($"(Tool) ({curtool.Tool.Number}) (Diametr) ({diametr}.) ({curtool.Tool.Caption}) (Operation) ({curtool.Comment}))");
                
            }
        }
        
        public override void OnStartProject(ICLDProject prj)
        {
            

            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            //nc.ProgName = Settings.Params.Str["OutFiles.NCProgName"];
            InputBox("Input the name of programs", ref nc.ProgName);
            if (String.IsNullOrEmpty(nc.ProgName))
                nc.ProgName = "";
            nc.Text.Show($"{nc.ProgName}");
            nc.WriteLine("%");
            nc.WriteLine("O" + nc.ProgName);
            
            nc.WriteLine();

            nc.WriteLine("( Postprocessor: " + SPPName + " )");
            nc.WriteLine("( Generated by SprutCAM )");
            nc.WriteLine("( DATE: " + CurDate() + " )");
            nc.WriteLine("( TIME: " + CurTime() + " )");

            OutToolList();

            nc.GInterp.v = 100;
            nc.Plane.v = 17;
            nc.KorEcv.v = 40;
            nc.KorDL.v = 49;
            nc.Cycle.v  = 80;
            nc.ABS_INC.v = 90;
            nc.COORDSYS.v = 54;
            nc.SmoothMv.v = 64;
            nc.CancelScale.v = 50;
            nc.Block.Out();

            string Flip = "N";
            InputBox("Flip 4th Axis Project Say Y\\N (Case Sensative): ", ref Flip);
            if(Flip.ToUpper() == "Y")
            {
                nc.Flip.v = 1;
            }
            nc.Block.Out();
        }


        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            nc.GInterp.v = 1;
            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            nc.Block.Out();
        }

        public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
        {
            switch(cld[1])
            {
                case 58 :
                {
                    int unit = 0;
                    if(cld[20] == 0)
                    {
                        unit = 21;
                    }
                    else 
                    {
                        unit = 20;
                    }
                    if(unit == 21)
                    {
                        nc.Units.v = unit;
                        nc.Text.v = "(Metric)";
                        nc.TextBlock.Out();
                    }
                    else
                    {
                        nc.Units.v = unit;
                        nc.Text.v = "(Inch)";
                        nc.TextBlock.Out();
                    }
                    break;
                }
                default:
                    break;
            }
        }

        public override void OnFinishProject(ICLDProject prj)
        {

            nc.Write("%");
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine("(" + op.Comment + ")");
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine();
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        // Uncomment line below (Ctrl + "/"), go to the end of "On" word and press Ctrl+Space to add a new CLData command handler
        // override On

    }
}
