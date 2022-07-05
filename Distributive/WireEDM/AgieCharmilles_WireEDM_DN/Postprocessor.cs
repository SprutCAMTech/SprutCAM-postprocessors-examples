namespace SprutTechnology.SCPostprocessor;

public partial class NCFile : TTextNCFile
{
    // Declare variables specific to a particular file here, as shown below
    // int FileNumber;
}

public partial class Postprocessor : TPostprocessor
{
    #region Common variables definition
    // Declare here global variables that apply to the entire postprocessor.

    ///<summary>Current nc-file</summary>
    NCFile nc;

    T3DPoint previousLowerPoint = new T3DPoint();
    T3DPoint previousUpperPoint = new T3DPoint();
    T3DPoint localCoordinateSystem = new T3DPoint();
    bool wireInserted;
    /// <summary>If true - need output cutting conditions</summary>
    bool conditionsNeedOut;
    /// <summary>If true - need output compensation mode</summary>
    bool compensNeedOut;


    #endregion

    public override void OnStartProject(ICLDProject prj)
    {
        nc = new NCFile();
        nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];

        nc.ProgN.Show(Settings.Params.Int["OutFiles.NCProgNumber"]);
        nc.Block.Out();

        nc.GAbsInc.Show(90);
        nc.GCS.Show(92);
        nc.X1.Show();
        nc.Y1.Show();
        nc.Block.Out();
    }

    public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
    {
        var outStr = nc.Block.Form();
        nc.WriteLine(outStr + $"({cmd.CLDataS})");
    }

    public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
    {
        if (cmd.IsLength)
            return;

        if (cmd.IsOn)
        {
            nc.GCompens.v = cmd.IsRightDirection ? 42 : 41;
            nc.H.Hide(cmd.CorrectorNumber);
        }
        else
        {
            nc.GCompens.v = 40;
        }
        nc.GCompens.Hide();
        compensNeedOut = true;
    }

    public override void OnEDMMove(ICLDEDMMoveCommand cmd, CLDArray cld)
    {
        // Insert and break wire commands
        if (!cmd.IsRapidMove && !wireInserted)
        {
            InsertWire();
        }
        if (cmd.IsRapidMove && wireInserted)
        {
            if (cmd.Lower.EP != previousLowerPoint)
                BreakWire();
        }

        // Cutting conditions selection
        if (conditionsNeedOut)
        {
            nc.C.Show();
            nc.Block.Out();
            conditionsNeedOut = false;
        }

        // Compensation turn on
        if (compensNeedOut && nc.GCompens != 40)
        {
            nc.GCompens.Show();
            nc.H.Show();
            nc.Block.Out();
            compensNeedOut = false;
        }

        // Motion mode turn on
        if (cmd.IsMultiProf4DMove)
        {
            nc.G2Contour.v = 61;
        }
        else if (cmd.IsCutsOnly4DMove)
        {
            nc.GUV.v = 74;
        }
        if (nc.G2Contour.ValuesDiffer || nc.GUV.ValuesDiffer)
        {
            nc.Block.Out();
            nc.GInterp1.RestoreDefaultValue(false);
            nc.GInterp2.RestoreDefaultValue(false);
        }

        // Compensation turn off
        if (compensNeedOut && nc.GCompens == 40)
        {
            nc.GCompens.Show();
            compensNeedOut = false;
        }

        // Coordinates output
        nc.X1.v = cmd.Lower.EP.X;
        nc.Y1.v = cmd.Lower.EP.Y;
        nc.Z1.v = cmd.Lower.EP.Z;
        switch (cmd.MotionMode)
        {
            case CLDEDMMotionMode.Rapid:
                if (nc.X1.ValuesDiffer || nc.Y1.ValuesDiffer || nc.Z1.ValuesDiffer)
                {
                    nc.GInterp1.v = 0;
                }
                break;
            case CLDEDMMotionMode.Plain2D:
            case CLDEDMMotionMode.Taper2D:
            case CLDEDMMotionMode.MultiProf4D:
                if (cmd.Lower.IsArc)
                {
                    nc.GInterp1.v = cmd.Lower.ArcR > 0d ? 3 : 2;
                    nc.I1.Show(cmd.Lower.Center.X - previousLowerPoint.X);
                    nc.J1.Show(cmd.Lower.Center.Y - previousLowerPoint.Y);
                }
                else // Cut
                {
                    nc.GInterp1.v = 1;
                }
                if (cmd.IsTaper2DMove)
                {
                    nc.GTaper.v = cmd.TaperAngle > 0 ? 52 : 51;
                    nc.A.Show(Abs(cmd.TaperAngle));
                }
                else if (cmd.IsMultiProf4DMove)
                {
                    nc.Colon.Show();
                    nc.X2.v = cmd.Upper.EP.X;
                    nc.Y2.v = cmd.Upper.EP.Y;
                    nc.Z2.v = cmd.Upper.EP.Z;
                    if (cmd.Upper.IsArc)
                    {
                        nc.GInterp2.v = cmd.Upper.ArcR > 0d ? 3 : 2;
                        nc.I2.Show(cmd.Upper.Center.X - previousUpperPoint.X);
                        nc.J2.Show(cmd.Upper.Center.Y - previousUpperPoint.Y);
                    }
                    else // Cut
                    {
                        nc.GInterp2.v = 1;
                    }
                    // CornerR
                    if ((cmd.IsPlain2DMove || cmd.IsTaper2DMove) && cmd.RollMode != EDMRollMode.Off)
                    {
                        nc.RollR1.Show(cmd.Lower.RollR);
                        nc.RollR2.Show(cmd.Upper.RollR);
                    }
                }
                break;
            case CLDEDMMotionMode.CutsOnly4D:
                nc.GInterp1.v = 1;
                var d = (T3DPoint)cmd.Upper.EP - cmd.Lower.EP;
                nc.U.v = d.X;
                nc.V.v = d.Y;
                nc.W.v = d.Z;
                break;
        }

        // Motion mode turn off
        if (!cmd.IsTaper2DMove && nc.GTaper != 50)
        {
            nc.GTaper.v = 50;
        }
        if (!cmd.IsMultiProf4DMove && nc.G2Contour != 60)
        {
            nc.G2Contour.v = 60;
        }
        if (!cmd.IsCutsOnly4DMove && nc.GUV != 75)
        {
            nc.U.v = 0;
            nc.V.v = 0;
        }
        nc.Block.Out();
        if (!cmd.IsCutsOnly4DMove && nc.GUV != 75)
        {
            nc.GUV.v = 75;
            nc.U.Hide();
            nc.V.Hide();
        }
        nc.Block.Out();

        // Remember current coordinates
        previousLowerPoint = cmd.Lower.EP;
        if (!cmd.IsMultiProf4DMove && !cmd.IsCutsOnly4DMove) // 2D
        {
            previousUpperPoint = previousLowerPoint;
        }
        else // 4D
        {
            previousUpperPoint = cmd.Upper.EP;
        }
    }

    public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
    {
        nc.C.Hide(cmd.FeedCode);
        conditionsNeedOut = true;
    }

    public override void OnFinishProject(ICLDProject prj)
    {
        nc.Block.Out();
        if (wireInserted)
        {
            BreakWire();
        }

        // M02
        nc.MStop.Show(02);
        nc.Block.Out();
        nc.WriteLine();

        // TODO: NCSub.Output
    }

    public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.MStop.Show(01);
        nc.Block.Out();
    }

    public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
    {
        if (!cmd.IsLocalCS)
            return;

        nc.GCS.Show(92);
        var p1 = previousLowerPoint - (cmd.MCS.P - localCoordinateSystem);
        nc.X1.Show(p1.X);
        nc.Y1.Show(p1.Y);
        nc.Z1.Show(p1.Z);
        localCoordinateSystem = cmd.MCS.P;
        nc.Block.Out();

        // Current coordinates updating
        previousLowerPoint = (nc.X1, nc.Y1, nc.Z1);
        previousUpperPoint = previousLowerPoint;
    }

    public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
    {
        switch (cmd.SubCode)
        {
            case 58: // TechInfo
                nc.WriteLine("(Rapid level       = " + Str((double)cld[9]) + ")");
                nc.WriteLine("(Upper guide level = " + Str((double)cld[47]) + ")");
                nc.WriteLine("(Upper work level  = " + Str((double)cld[10]) + ")");
                nc.WriteLine("(Lower work level  = " + Str((double)cld[11]) + ")");
                nc.WriteLine("(Lower guide level = " + Str((double)cld[48]) + ")");
                nc.WriteLine("(Wire diameter     = " + Str((double)cld[27]) + ")");
                break;

            case 56: // WEDMConditions
                nc.H.Show(cld[4]); // Offset code
                nc.HValue.Show(cld[5]); // Offset value
                nc.Block.Out();
                break;

            case 50: // STARTSUB
                nc.BlockN.Show(nc.ProgN + cld[2]);
                nc.Block.Out();
                break;

            case 51: // ENDSUB
                if (wireInserted)
                {
                    BreakWire();
                }
                nc.MSub.Show(99);
                nc.Block.Out();
                nc.WriteLine();
                break;

            case 52: // CALLSUB
                nc.MSub.Show(98);
                nc.SubN.Show(nc.ProgN + cld[2]);
                nc.Block.Out();
                // Idle sub call
                // TODO: NCSub.Output(cld[2], 0)
                break;
        }
    }

    public override void OnStop(ICLDStopCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.MStop.Show(00);
        nc.Block.Out();
    }

    public override void StopOnCLData()
    {
        // Do nothing, just to be possible to use CLData breakpoints
    }
}
