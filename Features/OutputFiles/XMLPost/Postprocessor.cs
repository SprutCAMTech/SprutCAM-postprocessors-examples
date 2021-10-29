using System.Xml;

namespace SprutTechnology.SCPostprocessor
{

    public partial class Postprocessor: TPostprocessor
    {
        string fileName = @"D:\Work\Files\NewDNPosts\OutFilesDemoPost\TestXMLOutput.xml";
        XmlDocument doc;

        public override void OnStartProject(ICLDProject prj)
        {
            doc = new XmlDocument();
            var nd = doc.CreateElement("CLDataCommands");
            doc.AppendChild(nd);
        }
        
        public override void OnBeforeCommandHandle(ICLDCommand cmd, CLDArray cld) 
        {
            var nd = doc.CreateElement(cmd.Name);
            doc.DocumentElement.AppendChild(nd);
            nd.InnerText = cmd.Caption;
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            doc.Save(fileName);
            NCFiles.AddExternalFile(fileName, this);
        }

    }
}
