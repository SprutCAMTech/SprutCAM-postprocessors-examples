using System;
using SprutTechnology.VecMatrLib;
using static SprutTechnology.STDefLib.STDef;
using static SprutTechnology.SCPostprocessor.CommonFuncs;

namespace SprutTechnology.SCPostprocessor
{
    class PointsFile: TTextNCFile
    {
        int pointsCount = 0;

        public int AddPoint(T3DPoint pnt) {
            pointsCount++;
            WriteLine("Point" + pointsCount + ": " + pnt);
            return pointsCount;
        }
    }

    class LinesFile: TTextNCFile
    {
        public void MoveToPoint(int pointIndex, CLDCmdType moveType) {
            WriteLine("MoveTo: Point" + pointIndex + " by " + moveType);
        }
    }

    public partial class Postprocessor: TPostprocessor
    {
        PointsFile points;
        LinesFile lines;

        public override void OnStartProject(ICLDProject prj)
        {
            points = new PointsFile();
            points.OutputFileName = @"D:\Work\Files\NewDNPosts\TwoFilesPost\Points.txt";
            lines = new LinesFile();
            lines.OutputFileName = @"D:\Work\Files\NewDNPosts\TwoFilesPost\Lines.txt";
        }

        public override void OnBeforeMovement(ICLDMotionCommand cmd, CLDArray cld)
        {
            int pointIndex = points.AddPoint(cmd.EP);
            lines.MoveToPoint(pointIndex, cmd.CmdType);
        }

    }
}
