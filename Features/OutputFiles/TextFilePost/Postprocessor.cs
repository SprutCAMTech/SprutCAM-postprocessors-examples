using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using System.Text;

namespace SprutTechnology.SCPostprocessor
{
    public partial class Postprocessor: TPostprocessor
    {
        string fileName = @"D:\Work\Files\NewDNPosts\TextFilePost\TextFile.txt";
        TTextNCFile file;

        public override void OnStartProject(ICLDProject prj)
        {
            file = new TTextNCFile(this);
            file.OutputFileName = fileName;
            file.TextEncoding = new UTF8Encoding(false);
            //file.TextEncoding = Encoding.GetEncoding("windows-1251");
        }
        
        public override void OnBeforeCommandHandle(ICLDCommand cmd, CLDArray cld) 
        {
            file.WriteLine(cmd.Caption);
        }

        public override void OnFinishProject(ICLDProject prj)
        {

        }

        // public override void OnStructure(ICLDStructureCommand cmd, CLDArray cld)
        // {
        //     if (SameText(cmd.NodeType, "Approach")) {
        //         if (cmd.IsOpen)
        //             NCFiles.DisableOutput();
        //         else
        //             NCFiles.EnableOutput();
        //     }
        // }

        // public override void OnFilterString(ref string s, TTextNCFile ncFile, INCLabel label)
        // {
        //     s = s.ToLower();
        // }

        // public override void OnExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        // {
        //     if (cmd.CycleType==481) {
        //         Log.Error("G81 cycle does not support");
        //         BreakTranslation();
        //     }
        // }

    }    
}
