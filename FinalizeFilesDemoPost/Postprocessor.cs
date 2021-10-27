using System.Diagnostics;
using System.IO;

namespace SprutTechnology.SCPostprocessor
{
    public partial class Postprocessor: TPostprocessor
    {   
        TTextNCFile file;
        int fileCount;

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld) {
            fileCount++;
            file = new TTextNCFile();
            file.OutputFileName = @"D:\Work\Files\NewDNPosts\FinalizeFilesDemoPost\bin\FinalizeDemo"+fileCount+".txt";

            file.WriteLine("<Document>");
        }
        
        public override void OnBeforeCommandHandle(ICLDCommand cmd, CLDArray cld) {
            file?.WriteLine(cmd.Caption);
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld) {
            file.WriteLine("</Document>");
            file = null;
        }

        public override void OnFinalizeNCFiles(TNCFilesManager ncFiles) {
            string args = "";
            for (int i=0; i<ncFiles.FileCount; i++) {
                // File.Copy(ncFiles[i].OutputFileName, @"//RobotCNC/file.txt");
                args = args + " " + ncFiles[i].OutputFileName;
            }

            Process.Start(@"c:\Program Files\Notepad++\notepad++.exe", args);
        }

    }
}
