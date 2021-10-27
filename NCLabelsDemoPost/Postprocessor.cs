
namespace SprutTechnology.SCPostprocessor
{
    public partial class Postprocessor: TPostprocessor
    {
        public override void OnStartProject(ICLDProject prj)
        {
            TTextNCFile file = new TTextNCFile();
            file.OutputFileName = @"D:\Work\Files\NewDNPosts\NCLabelsDemoPost\DemoNCLabels.txt";

            file.WriteLine("<Document>");
            INCLabel lbl = file.CreateLabel();
            file.WriteLine("</Document>");

            lbl.SnapToRight();
            file.Write("a", lbl);
            file.Write("b", lbl);
            file.Write("c", lbl);
            file.Write("|", lbl);

            lbl.SnapToLeft();
            file.Write("d", lbl);
            file.Write("e", lbl);
            file.Write("f", lbl);
            file.Write("|", lbl);
        }
    }
}
