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

    /// <summary>Previous lower point X coordinate</summary>
    double fp1X;
    /// <summary>Previous lower point Y coordinate</summary>
    double fp1Y;
    /// <summary>Previous lower point Z coordinate</summary>
    double fp1Z;
    /// <summary>Previous upper point X coordinate</summary>
    double fp2X;
    /// <summary>Previous upper point Y coordinate</summary>
    double fp2Y;
    /// <summary>Previous upper point Z coordinate</summary>
    double fp2Z;
    bool wireInserted;
    /// <summary>If true - need output cutting coditions</summary>
    bool conditionsNeedOut;
    /// <summary>If true - need output compensation mode</summary>
    bool compensNeedOut;
    /// <summary>Displacement of the local coordinate system on the global coordinate system along the X-axis</summary>
    double lcsx;
    /// <summary>Displacement of the local coordinate system on the global coordinate system along the Y-axis</summary>
    double lcsy;
    /// <summary>Displacement of the local coordinate system on the global coordinate system along the Z-axis</summary>
    double lcsz;


    #endregion

    public override void OnStartProject(ICLDProject prj)
    {
        nc = new NCFile();
        nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];

        var win = CreateInputBox();
        win.AddIntegerProp("Input program number", 1, value => nc.ProgN.Show(value));
        win.Show();
        nc.Block.Out();

        nc.GAbsInc.Show(90);
        nc.GCS.Show(92);
        nc.X1.Show();
        nc.Y1.Show();
        nc.Block.Out();
    }

    public override void OnComment(ICLDCommentCommand cmd, CLDArray _)
    {
        var outStr = nc.Block.Form();
        nc.WriteLine(outStr + $"({cmd.CLDataS})");
    }

    public override void OnCutCom(ICLDCutComCommand cmd, CLDArray _)
    {
        if (cmd.IsLength)
            return;

        if (cmd.IsOn)
        {
            nc.GCompens.v = cmd.IsRightDirection ? 42 : 41;
            nc.H.v = nc.H.v0 = cmd.CorrectorNumber;
        }
        else
        {
            nc.GCompens.v = 40;
        }
        nc.GCompens.v0 = nc.GCompens.v;
        compensNeedOut = true;
    }

    public override void OnEDMMove(ICLDEDMMoveCommand cmd, CLDArray _)
    {
        // Insert and break wire commands
        if (!cmd.IsRapidMove && !wireInserted)
        {
            InsertWire();
        }
        if (cmd.IsRapidMove && wireInserted)
        {
            var lowerPointChanged = cmd.Lower.EP.X != fp1X || cmd.Lower.EP.Y != fp1Y || cmd.Lower.EP.Z != fp1Z;
            if (lowerPointChanged)
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
            nc.GInterp1.v = nc.GInterp1.v0 = 0;
            nc.GInterp2.v = nc.GInterp2.v0 = 0;
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
                    nc.I1.Show(cmd.Lower.Center.X - fp1X);
                    nc.J1.Show(cmd.Lower.Center.Y - fp1Y);
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
                        nc.I2.Show(cmd.Upper.Center.X - fp2X);
                        nc.J2.Show(cmd.Upper.Center.Y - fp2Y);
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
                nc.U.v = cmd.Upper.EP.X - cmd.Lower.EP.X;
                nc.V.v = cmd.Upper.EP.Y - cmd.Lower.EP.Y;
                nc.W.v = cmd.Upper.EP.Z - cmd.Lower.EP.Z;
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
        fp1X = cmd.Lower.EP.X;
        fp1Y = cmd.Lower.EP.Y;
        fp1Z = cmd.Lower.EP.Z;
        if (!cmd.IsMultiProf4DMove && !cmd.IsCutsOnly4DMove) // 2D
        {
            fp2X = fp1X;
            fp2Y = fp1Y;
            fp2Z = fp1Z;
        }
        else // 4D
        {
            fp2X = cmd.Upper.EP.X;
            fp2Y = cmd.Upper.EP.Y;
            fp2Z = cmd.Upper.EP.Z;
        }
    }

    public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray _)
    {
        nc.C.v = nc.C.v0 = cmd.FeedCode;
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

    public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray _)
    {
        nc.Block.Out();
        nc.MStop.Show(01);
        nc.Block.Out();
    }

    public override void OnOrigin(ICLDOriginCommand cmd, CLDArray _)
    {
        if (!cmd.IsLocalCS)
            return;

        nc.GCS.Show(92);
        nc.X1.Show(fp1X - (cmd.MCS.P.X - lcsx));
        nc.Y1.Show(fp1Y - (cmd.MCS.P.Y - lcsy));
        nc.Z1.Show(fp1Z - (cmd.MCS.P.Z - lcsz));
        lcsx = cmd.MCS.P.X;
        lcsy = cmd.MCS.P.Y;
        lcsz = cmd.MCS.P.Z;
        nc.Block.Out();

        // Current coordinates updating
        fp1X = nc.X1;
        fp1Y = nc.Y1;
        fp1Z = nc.Z1;
        fp2X = fp1X;
        fp2Y = fp1Y;
        fp2Z = fp1Z;
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

    public override void OnStop(ICLDStopCommand cmd, CLDArray _)
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
