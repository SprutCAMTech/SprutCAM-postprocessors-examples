using System.IO;
using System.Text.Json;

namespace SprutTechnology.SCPostprocessor
{
    public partial class Postprocessor: TPostprocessor
    {
        string fileName = @"D:\Work\Files\NewDNPosts\JSONPost\TestJSON.json";
        Utf8JsonWriter file;

        public override void OnStartProject(ICLDProject prj)
        {
            FileStream stream = new FileStream(fileName, FileMode.Create);
            JsonWriterOptions options = new JsonWriterOptions();
            options.Indented = true;
            file = new Utf8JsonWriter(stream, options);
            file.WriteStartObject();
            file.WriteStartArray("CLDataCommands");
        }
        
        public override void OnBeforeCommandHandle(ICLDCommand cmd, CLDArray cld) 
        {
            file.WriteStringValue(cmd.Caption);
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            file.WriteEndArray();
            file.WriteEndObject();
            file.Flush();
            NCFiles.AddExternalFile(fileName, this);
        }

    }
}
