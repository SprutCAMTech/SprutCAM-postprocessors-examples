namespace SprutTechnology.SCPostprocessor
{

    // A wrapper class for resulting CLData file. You can add any required properties inside.
    public partial class ExtCLDataFile: TTextNCFile
    {
        // Index of CLData file
        public int FileNumber;
    }

    public partial class Postprocessor: TPostprocessor
    {
        ///<summary>Current CLData-file</summary>
        ExtCLDataFile curFile;

        ///<summary>Header CLData-file</summary>
        ExtCLDataFile headerFile;

        public override void OnStartProject(ICLDProject prj)
        {
            StartNewCLDataFile(Settings.Params.Bol["OutFiles.SplitByOperations"]);
            headerFile = curFile;
            // Write project header here
            curFile.WriteLine($"$$ -------------------------------------------");
            curFile.WriteLine($"$$         Generated by SprutCAM");
            curFile.WriteLine($"$$         Date: {CurISODate()}");
            curFile.WriteLine($"$$ -------------------------------------------");
            curFile.WriteLine($"$$ SprutCAM project name: '{prj.ProjectName}'");
            curFile.WriteLine($"$$ SprutCAM project file: '{prj.FilePath}'");
            curFile.WriteLine($"$$ SprutCAM project machine name: '{prj.Machine.MachineName}'");
            curFile.WriteLine($"$$ Database file name: '{Settings.Params.Str["OutFiles.DBFFile"]}'");
            curFile.WriteLine($"$$ External postprocessor name: '{Settings.Params.Str["OutFiles.ExtPostName"]}'");
            curFile.WriteLine($"$$ Postprocessor revision: {Settings.Params.Int["OutFiles.Revision"]}");
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            // You can also finalize header information here
            headerFile.WriteLine($"$$ Total CLData files count: {curFile.FileNumber+1}");
            headerFile.WriteLine($"$$ CLData files located at: {curFile.OutputFileName}");
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            if (Settings.Params.Bol["OutFiles.SplitByOperations"])
                StartNewCLDataFile(true);
            curFile.WriteLine($"$$ Tech operation: '{op.Comment}'");
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            curFile.WriteLine();
        }

        public override void OnFinalizeNCFiles(TNCFilesManager ncFiles)
        {
            if (Settings.Params.Bol["ExtUtil.StartExtUtil"]) {
                var extUtilFileName = Settings.Params.Str["ExtUtil.ExtUtilFileName"];
                if (File.Exists(extUtilFileName)) {
                    string args = "";
                    for (int i=0; i<ncFiles.FileCount; i++) {
                        args = args + " " + ncFiles[i].OutputFileName;
                    }
                    Process.Start(extUtilFileName, args);
                } else {
                    Log.Error($"Cannot start external utility '{extUtilFileName}'");
                }
            }            
        }

        public override void OnAfterCommandHandle(ICLDCommand cmd, CLDArray cld)
        {
            if (!IsIgnoringCommand(cmd))
                curFile.WriteLine(cmd.Caption);
        }

        private bool isInsideBodySection = false;
        private bool IsIgnoringCommand(ICLDCommand cmd)
        {
            switch (cmd.CmdType) {
                case CLDCmdType.PartNo:
                case CLDCmdType.Goto:
                case CLDCmdType.Fedrat:
                    return true;
                case CLDCmdType.PPFun:
                    int ppfunType = cmd.CLD[1];
                    return ((ppfunType==58) || ppfunType==59);
                case CLDCmdType.Structure:
                    var scmd = cmd as ICLDStructureCommand;
                    bool isBodySectionCmd = SameText(scmd.NodeType, "BodySection");
                    if (isBodySectionCmd)
                        isInsideBodySection = scmd.IsOpen;
                    return !isInsideBodySection || isBodySectionCmd; 
                default: 
                    return false;
            }
        }

        private bool DeleteExistingFileOrFolder(string path)
        {
            bool result = false;
            if (File.Exists(path)) {
                try {
                    File.Delete(path);
                    result = true;
                } catch {}
            } 
            else if (Directory.Exists(path)) {
                try {
                    Directory.Delete(path, true);
                    result = true;
                } catch {}
            }
            return result;
        }

        private string resCLDataFolder;
        private string ResCLDataFolder {
            get {
                if (String.IsNullOrEmpty(resCLDataFolder)) {
                    resCLDataFolder = Settings.Params.Str["OutFiles.APTFileName"];
                    if (String.IsNullOrEmpty(resCLDataFolder)) 
                        resCLDataFolder = Path.GetTempPath() + Path.GetRandomFileName();
                }
                return resCLDataFolder;
            }
        }

        private string MakeNewCLDataFileName(int index)
        {
            string result = Path.Combine(ResCLDataFolder, $"CLDFile{index}.aptsource");
            return result;
        }

        private void StartNewCLDataFile(bool generateNameAutomatically)
        {           
            var newFile = new ExtCLDataFile();
            newFile.FileNumber = 0;
            if (curFile != null)
                newFile.FileNumber = curFile.FileNumber + 1;
            else
                DeleteExistingFileOrFolder(ResCLDataFolder);
            if (generateNameAutomatically)
                newFile.OutputFileName = MakeNewCLDataFileName(newFile.FileNumber);
            else {
                newFile.OutputFileName = Settings.Params.Str["OutFiles.APTFileName"];
                if (String.IsNullOrEmpty(newFile.OutputFileName)) 
                    newFile.OutputFileName = MakeNewCLDataFileName(newFile.FileNumber);
            }
            curFile = newFile;
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            curFile.WriteLine($"GOTO   / {curFile.Coord.ToString(cmd.EP.X)},   {curFile.Coord.ToString(cmd.EP.Y)},   {curFile.Coord.ToString(cmd.EP.Z)}");
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            if (cmd.FeedUnits==CLDFeedUnits.MPM)
                curFile.WriteLine($"FEDRAT / {curFile.Coord.ToString(cmd.FeedValue)}, MMPM");
            else
                curFile.WriteLine($"FEDRAT / {curFile.Coord.ToString(cmd.FeedValue)}, MMPR");
        }

        // Uncomment line below (Ctrl + "/"), go to the end of "On" word and press Ctrl+Space to add a new CLData command handler
        // override On

    }
}
