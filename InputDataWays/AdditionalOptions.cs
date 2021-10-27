using System;
using System.Text;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.VecMatrLib.VML;

namespace SprutTechnology.SCPostprocessor
{

    public class AdditionalOptions
    {
        public bool CommentsInUpperCase { get; set; }
        public int MaxLinesCount { get; set; }
        public double CalculationTolerance { get; set; }
        public string ProgrammAuthor { get; set; }
        public InpArray<string> ListOfStuff { get; set; }
        public bool SubroutinesInSeparateFiles { get; set; }
        public string FolderForSubroutines { get; set; }

        public AdditionalOptions()
        {
            CommentsInUpperCase = false;
            MaxLinesCount = 10000;
            CalculationTolerance = 0.02;
            ListOfStuff = new InpArray<string>();
            ListOfStuff[0] = "Ivanov";
            ListOfStuff[1] = "Petrov";
            ListOfStuff[2] = "Sidorov";
            ProgrammAuthor = ListOfStuff[0];
            SubroutinesInSeparateFiles = true;
            FolderForSubroutines = Path.GetTempPath() + "MySubroutines";
        }

        public static AdditionalOptions ShowWindow()
        {
            var o = AdditionalOptions.LoadFromFile();
            var win = CreateInputBox();
            win.WindowCaption = "Additional options";
            win.StartGroup("Common");
            win.AddBooleanProp("Comments in upper case", o.CommentsInUpperCase, v => o.CommentsInUpperCase = v);
            win.AddIntegerProp("Maximal count of lines in a file", o.MaxLinesCount, v => o.MaxLinesCount = v);
            win.AddDoubleProp("Tolerance of some calculations", o.CalculationTolerance, v => o.CalculationTolerance = v);
            win.AddStringEditableProp("The programm author", o.ProgrammAuthor, v => o.ProgrammAuthor = v, 
                o.ListOfStuff.ToArray());
            win.CloseGroup();
            win.StartGroup("Subroutines");
            win.AddBooleanProp("Generate subroutines to separate files", o.SubroutinesInSeparateFiles,
                v => o.SubroutinesInSeparateFiles = v);
            win.AddFolderPathProp("Folder for subroutines", o.FolderForSubroutines, v => o.FolderForSubroutines = v);
            win.CloseGroup();
            win.Show();
            SaveToFile(o);
            return o;
        }

        public static void SaveToFile(AdditionalOptions options)
        {
            var jsonString = JsonSerializer.Serialize<AdditionalOptions>(options);
            Directory.CreateDirectory(Path.GetDirectoryName(OptionsFileName));
            File.WriteAllText(OptionsFileName, jsonString);
        }

        public static AdditionalOptions LoadFromFile()
        {
            if (File.Exists(OptionsFileName))
            {
                var jsonString = File.ReadAllText(OptionsFileName);
                return JsonSerializer.Deserialize<AdditionalOptions>(jsonString);
            }
            else
                return new AdditionalOptions();
        }

        private static string OptionsFileName
        {
            get => Path.GetTempPath() + @"MyPostAdditionalOptions\options.json";
        }

    }

}