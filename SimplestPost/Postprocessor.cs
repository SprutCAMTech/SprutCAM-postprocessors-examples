using System;

namespace SprutTechnology.SCPostprocessor
{

    public partial class Postprocessor: TPostprocessor
    {
        TTextNCFile nc;

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new TTextNCFile(this);
            nc.OutputFileName = @"D:\MyProgramsOnDotNet\GCodeText.txt";
        }


        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            nc.WriteLine("G1 X" + cmd.EP.X + " Y" + cmd.EP.Y + " Z" + cmd.EP.Z);
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            nc.WriteLine("G" + cmd.Dir + " X" + cmd.EP.X + " Y" + cmd.EP.Y + " Z" + cmd.EP.Z + " R" + cmd.RIso);
        }

    }
}
