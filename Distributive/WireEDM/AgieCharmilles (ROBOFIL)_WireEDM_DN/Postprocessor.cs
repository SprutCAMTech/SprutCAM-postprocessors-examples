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

    T2DPoint previousLowerPoint;
    T2DPoint previousUpperPoint;
    /// <summary>
    /// If true - wire inserted, else - wire broken
    /// </summary>
    bool wireInserted;
    /// <summary>
    /// If true - need output cutting conditions for SubProc
    /// </summary>
    bool conditionsNeedOutSub = true;
    /// <summary>
    /// If true - need output cutting conditions
    /// </summary>
    bool conditionsNeedOut;
    /// <summary>
    /// If true - need output compensation mode
    /// </summary>
    bool compensationNeedOut;
    CoordinateSystem localCoordinateSystem = new();
    double upperLevel;
    double lowerLevel;
    T2DPoint origin;
    int teci;
    int wiri;
    int technologyNumber;
    int oldTechnologyNumber;
    double tolerance = 0.0001;
    int wireDiameter;
    bool isMainProgram = true;
    int programNumber;

    ///<summary>Current nc-file</summary>
    NCFile nc;

    #endregion

    public override void OnStartProject(ICLDProject prj)
    {
        nc = new NCFile();
        nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];

        programNumber = Settings.Params.Int["OutFiles.NCProgNumber"];
        nc.WriteLine("%");
        nc.WriteLine($"O{Str(programNumber)}({prj.ProjectName})");

        nc.G_AbsInc.Show(90);
        nc.Block.Out();
    }

    public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
    {
        nc.Output($"{nc.N_RowNumber} ({cmd.CLDataS})");
        nc.N_RowNumber.Increase();
    }

    public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
    {
        if (cmd.IsLength)
            return;

        if (cmd.IsOn)
        {
            var compensation = cmd.IsRightDirection ? 42 : 41;
            nc.G_Compensation.Hide(compensation);
        }
        else
        {
            nc.G_Compensation.Hide(40);
        }
        compensationNeedOut = true;
    }

    public override void OnEDMMove(ICLDEDMMoveCommand cmd, CLDArray cld)
    {
        if (!localCoordinateSystem.SetDefined)
        {
            localCoordinateSystem.SetDefined = true;
            nc.G_CoordinateSystem.Show(92);
            nc.X_Lower.Show(origin.X);
            nc.Y_Lower.Show(origin.Y);
            nc.I_UpperPlane.Show(upperLevel);
            nc.J_LowerPlane.Show(lowerLevel);
            nc.Block.Out();
        }

        var rollModeChanged = false;
        if (cmd.RollMode != 0 && nc.G_RollMode != 48)
        {
            nc.G_RollMode.Show(48);
            rollModeChanged = true;
        }
        else if (cmd.RollMode == EDMRollMode.Off && nc.G_RollMode != 49)
        {
            nc.G_RollMode.Show(49);
        }

        // Insert and break wire commands
        if (cmd.MotionMode != CLDEDMMotionMode.Rapid && !wireInserted && isMainProgram)
        {
            InsertWire();
        }
        if (cmd.MotionMode == CLDEDMMotionMode.Rapid && wireInserted && isMainProgram && cmd.Lower.EP.To2DPoint() != previousLowerPoint)
        {
            BreakWire();
        }

        // Output pass technology number
        if (cmd.MotionMode != CLDEDMMotionMode.Rapid && wireInserted && oldTechnologyNumber != technologyNumber)
        {
            oldTechnologyNumber = technologyNumber;
            nc.S_TechnologyCode.Show(technologyNumber);
            nc.Block.Out();
        }

        // Start conical machining mode
        if (cmd.MotionMode == CLDEDMMotionMode.Taper2D && nc.G_TaperMode != 60)
        {
            nc.G_TaperMode.Show(60);
            nc.Block.Out();
        }

        // Compensation turn on
        if (compensationNeedOut && nc.G_Compensation != 40)
        {
            nc.G_Compensation.Show();
            compensationNeedOut = false;
        }

        // Motion mode turn on
        if (cmd.MotionMode == CLDEDMMotionMode.MultiProf4D)
        {
            nc.G_2Contour.v = 51;
        }
        if (cmd.MotionMode == CLDEDMMotionMode.CutsOnly4D)
        {
            nc.G_UV.v = 74;
        }

        // Compensation turn off
        if (compensationNeedOut && nc.G_Compensation == 40)
        {
            nc.G_Compensation.Show();
            compensationNeedOut = false;
        }

        // Coordinates output
        nc.X_Lower.v = cmd.Lower.EP.X;
        nc.Y_Lower.v = cmd.Lower.EP.Y;
        var outG = 0d;
        switch (cmd.MotionMode)
        {
            case CLDEDMMotionMode.Rapid:
                if (Abs(nc.X_Lower - nc.X_Lower.v0) > tolerance || Abs(nc.Y_Lower - nc.Y_Lower.v0) > tolerance)
                {
                    nc.G_LowerInterpolation.v = 0;
                }
                break;

            case CLDEDMMotionMode.Plain2D:
            case CLDEDMMotionMode.Taper2D:
            case CLDEDMMotionMode.MultiProf4D:
            case CLDEDMMotionMode.CutsOnly4D:
                if (Abs(nc.X_Lower - nc.X_Lower.v0) > tolerance || Abs(nc.Y_Lower - nc.Y_Lower.v0) > tolerance)
                {
                    if (cmd.Lower.IsArc)
                    {
                        var r1 = cmd.Lower.ArcR > 0 ? 3 : 2;
                        nc.G_LowerInterpolation.Show(r1);
                        var p1 = cmd.Lower.Center - previousLowerPoint;
                        nc.I_LowerPcX.Show(p1.X);
                        nc.J_LowerPcY.Show(p1.Y);
                    }
                    else
                    {
                        nc.G_LowerInterpolation.Show(1);
                    }
                    outG = nc.G_LowerInterpolation;
                }
                if (cmd.MotionMode == CLDEDMMotionMode.Taper2D)
                {
                    nc.G_Taper.v = cmd.TaperAngle > 0 ? 52 : 51;
                    nc.T_Angle.v = Abs(cmd.TaperAngle);
                }
                else if (cmd.MotionMode == CLDEDMMotionMode.MultiProf4D)
                {
                    nc.U_UpperX.Show(cmd.Upper.EP.X);
                    nc.V_UpperY.Show(cmd.Upper.EP.Y);
                    if (cmd.Upper.IsArc)
                    {
                        if (cmd.Upper.ArcR > 0)
                        {
                            if (outG is 0 or not 3)
                            {
                                nc.G_UpperInterpolation.v = 3;
                            }
                        }
                        else
                        {
                            if (outG is 0 or not 2)
                            {
                                nc.G_UpperInterpolation.v = 2;
                            }
                        }
                        var p2 = cmd.Upper.Center - previousUpperPoint;
                        nc.K_UpperPcX.Show(p2.X);
                        nc.L_UpperPcY.Show(p2.Y);
                    }
                    else
                    {
                        if (Abs(nc.U_UpperX - nc.U_UpperX.v0) > tolerance || Abs(nc.V_UpperY - nc.V_UpperY.v0) > tolerance)
                        {
                            if (outG is 0 or not 1)
                            {
                                nc.G_UpperInterpolation.Show(1);
                            }
                        }
                    }
                }
                if (cmd.MotionMode is CLDEDMMotionMode.Plain2D or CLDEDMMotionMode.Taper2D && cmd.RollMode != EDMRollMode.Off)
                {
                    nc.R_LowerRoll.v = cmd.Lower.RollR;
                    if (rollModeChanged)
                    {
                        nc.R_LowerRoll.Show();
                        rollModeChanged = false;
                    }
                }
                break;
        }

        // Motion mode turn off
        if (cmd.MotionMode != CLDEDMMotionMode.Taper2D && nc.G_Taper != 50)
        {
            nc.G_Taper.v = 50;
        }
        if (cmd.MotionMode != CLDEDMMotionMode.CutsOnly4D && nc.G_UV != 75)
        {
            nc.U_UpperX.v = 0;
            nc.V_UpperY.v = 0;
        }
        nc.Block.Out();
        if (cmd.MotionMode != CLDEDMMotionMode.CutsOnly4D && nc.G_UV != 75)
        {
            nc.G_UV.v = 75;
            nc.U_UpperX.Hide();
            nc.V_UpperY.Hide();
        }
        nc.Block.Out();

        // Remember current coordinates
        previousLowerPoint = cmd.Lower.EP.To2DPoint();
        if (cmd.MotionMode is not CLDEDMMotionMode.MultiProf4D and not CLDEDMMotionMode.CutsOnly4D)
        {
            previousUpperPoint = previousLowerPoint;
        }
        else
        {
            previousUpperPoint = cmd.Upper.EP.To2DPoint();
        }
    }

    public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
    {
        if (conditionsNeedOutSub)
        {
            conditionsNeedOut = true;
        }
        technologyNumber = cmd.FeedCode;
    }

    public override void OnFinishProject(ICLDProject prj)
    {
        nc.Block.Out();
        if (wireInserted)
        {
            BreakWire();
        }

        nc.M_Stop.Show(30);
        nc.Block.Out();

        nc.M_Stop.Show(02);
        nc.Block.Out();
        nc.WriteLine();

        CLDSub.Translate();
    }

    public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
    {
        nc.Block.Out();
        nc.M_Stop.Show(01);
        nc.Block.Out();
    }

    public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
    {
        if (cmd.IsLocalCS)
        {
            nc.G_CoordinateSystem.Show(92);
            var p1 = previousLowerPoint - (cmd.MCS.P.To2DPoint() - localCoordinateSystem.Displacement);
            nc.X_Lower.Show(p1.X);
            nc.Y_Lower.Show(p1.Y);
            localCoordinateSystem.Displacement = cmd.MCS.P.To2DPoint();
            nc.Block.Out();

            // Current coordinates updating
            previousLowerPoint = new(nc.X_Lower, nc.Y_Lower);
            previousUpperPoint = previousLowerPoint;
        }
        else
        {
            localCoordinateSystem.SetDefined = false;
            origin = cmd.MCS.P.To2DPoint();
        }
    }

    public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
    {
        switch (cmd.SubCode)
        {
            case 50: // STARTSUB
                isMainProgram = false;
                nc.WriteLine("O" + Str(programNumber) + Str((double)cld[2]));
                break;

            case 51: // ENDSUB
                isMainProgram = false;
                nc.M_Sub.Show(99);
                nc.Block.Out();
                nc.WriteLine();
                break;

            case 52: // CALLSUB
                if (conditionsNeedOutSub)
                {
                    conditionsNeedOutSub = false;
                    conditionsNeedOut = true;
                    InsertWire();
                }
                isMainProgram = true;
                nc.M_Sub.Show(98);
                var outStr = nc.Block.Form();
                nc.WriteLine(outStr + "P" + Str(programNumber) + Str((double)cld[2]) + "L1");
                CLDSub.Translate(cld[2], false);
                break;

            case 56: // WEDMConditions
                isMainProgram = true;
                if (cld[2] == 1)
                {
                    if (cld.Count >= 100)
                    {
                        wiri = cld[100]; // Passes file
                    }
                    if (cld.Count >= 101)
                    {
                        teci = cld[101]; // Workpiece material
                    }
                    if (cld.Count >= 102)
                    {
                        wireDiameter = cld[102]; // Wire diameter
                    }
                }
                break;

            case 58: // TECHINFO
                isMainProgram = true;
                nc.M_ResetMachiningTime.Show(31);
                nc.Block.Out();
                if (cld[20] == 0) // millimeters
                {
                    nc.G_MeasurementUnits.Show(21);
                    nc.Block.Out();
                }
                else if (cld[20] == 1) // inches
                {
                    nc.G_MeasurementUnits.Show(20);
                    nc.Block.Out();
                }
                upperLevel = cld[10];
                lowerLevel = cld[11];
                break;
        }
    }

    private void InsertWire()
    {
        nc.Output("(Insert wire)");
        wireInserted = true;
        nc.M_LoadWire.Show(60);
        nc.Block.Out();

        // Cutting conditions selection
        if (!conditionsNeedOut)
            return;

        nc.G_Technology.Show(11);
        if (wiri > 0 && teci > 0)
        {
            var swir = wiri switch // Base technological table
            {
                1 => "LR",
                2 => "LS",
                3 => "LT",
                4 => "SR",
                5 => "SS",
                6 => "ST",
                7 => "XR",
                8 => "XS",
                9 => "XT",
                10 => "WR",
                11 => "WS",
                12 => "WT",
                _ => throw new ArgumentOutOfRangeException(nameof(wiri))
            };
            var s = Str(wireDiameter);
            if (Length(s) == 1)
            {
                s += "0";
            }
            swir += s; // Add Wire Diameter

            var stec = swir + teci switch // Workpiece material code
            {
                1 => "A",
                2 => "C",
                3 => "D",
                4 => "F",
                5 => "L",
                6 => "W",
                _ => throw new ArgumentOutOfRangeException(nameof(teci))
            };

            var outStr = nc.Block.Form();
            nc.Output($"{outStr} (WIR,{swir})");
            nc.G_Technology.Show(11);
            outStr = nc.Block.Form();
            nc.Output($"{outStr} (TEC,{stec})");
        }
        conditionsNeedOut = false;
    }

    private void BreakWire()
    {
        nc.Output("(Break wire)");
        wireInserted = false;
        nc.M_LoadWire.Show(50);
        nc.Block.Out();
    }
}
