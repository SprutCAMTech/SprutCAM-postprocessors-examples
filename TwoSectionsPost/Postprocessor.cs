using SprutTechnology.VecMatrLib;

namespace SprutTechnology.SCPostprocessor
{
    public partial class Postprocessor: TPostprocessor
    {
        TTextNCFile file;
        INCLabel movementsSectionLabel;
        INCLabel pointsSectionLabel;
        int pointsCount = 0;

        public override void OnStartProject(ICLDProject prj)
        {
            file = new TTextNCFile();
            file.OutputFileName = @"D:\Work\Files\NewDNPosts\TwoSectionsPost\TwoSectionsDemo.txt";

            file.WriteLine("<MovementsSection>");
            movementsSectionLabel = file.CreateLabel();
            file.WriteLine("</MovementsSection>");
            file.WriteLine();
            file.WriteLine("<PointsSection>");
            pointsSectionLabel = file.CreateLabel();
            file.WriteLine("</PointsSection>");

            movementsSectionLabel.SnapToRight();
            pointsSectionLabel.SnapToRight();
        }

        public override void OnBeforeMovement(ICLDMotionCommand cmd, CLDArray cld)
        {
            pointsCount++;
            // file.WriteLine("Point" + pointsCount + ": " + (T3DPoint)cmd.EP, pointsSectionLabel);
            // file.WriteLine("MoveTo: Point" + pointsCount + " by " + cmd.CmdType, movementsSectionLabel);
            file.DefaultLabel = pointsSectionLabel;
            file.Write("    <Point" + pointsCount + ">");
            file.Write(((T3DPoint)cmd.EP).ToString());
            file.WriteLine("</Point" + pointsCount + ">");
            file.DefaultLabel = movementsSectionLabel;
            file.Write("    <MoveTo>");
            file.Write("Point" + pointsCount + " by " + cmd.CmdType);
            file.WriteLine("</MoveTo>");
        }

    }
}
