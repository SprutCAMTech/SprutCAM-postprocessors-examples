using System;
using System.Text;
using System.Diagnostics;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.VecMatrLib.VML;
using System.IO;

namespace SprutTechnology.SCPostprocessor
{

    public class PPFunWriter
    {
        static TTextNCFile file;

        private static void WriteOneParameter(INamedProperty p, TTextNCFile f, int indent)
        {
            if ((p.SimpleType==NamedParamType.ptComplexType) || ((p.SimpleType==NamedParamType.ptArray))) {
                f.WriteLine(StringOfChar(' ', indent*4) + "<" + p.Name + ">");
                var cp = p.GetNextChild(null);
                while (cp!=null) {
                    WriteOneParameter(cp, f, indent + 1);
                    cp = p.GetNextChild(cp);
                }
            }
            else 
                f.WriteLine(StringOfChar(' ', indent*4) + "<" + p.Name + ">" + p.ValueAsString + "</" + p.Name + ">");
            if ((p.SimpleType==NamedParamType.ptComplexType) || ((p.SimpleType==NamedParamType.ptArray))) 
                f.WriteLine(StringOfChar(' ', indent*4) + "</" + p.Name + ">");
        }


        public static void SaveFile(string FileName, ICLDTechOperation op)
        {
            // Log.Info("File name is " + FileName);
            if (file == null)
            {
                var f = new TTextNCFile();
                f.OutputFileName = FileName;
                file = f;
                file.WriteLine("<PPFun>");
            }
            WriteOneParameter(op.Props, file, 1);
        }

        public static void CloseFile()
        {
            if (file != null)
            {
                file.WriteLine("</PPFun>");
            }
        }
    }

}