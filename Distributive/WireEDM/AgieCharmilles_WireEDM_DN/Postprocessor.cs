namespace SprutTechnology.SCPostprocessor
{

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
        double Fp1X;
        /// <summary>Previous lower point Y coordinate</summary>
        double Fp1Y;
        /// <summary>Previous lower point Z coordinate</summary>
        double Fp1Z;
        /// <summary>Previous upper point X coordinate</summary>
        double Fp2X;
        /// <summary>Previous upper point Y coordinate</summary>
        double Fp2Y;
        /// <summary>Previous upper point Z coordinate</summary>
        double Fp2Z;
        bool WireInserted;
        /// <summary>If true - need output cutting coditions</summary>
        bool ConditionsNeedOut;
        /// <summary>If true - need output compensation mode</summary>
        bool CompensNeedOut;
        /// <summary>Displacement of the local coordinate system on the global coordinate system along the X-axis</summary>
        double LCSX;
        /// <summary>Displacement of the local coordinate system on the global coordinate system along the Y-axis</summary>
        double LCSY;
        /// <summary>Displacement of the local coordinate system on the global coordinate system along the Z-axis</summary>
        double LCSZ;


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

        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {
            var outStr = nc.Block.Form();
            nc.Output(outStr + $"({cmd.CLDataS})");
        }

        public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
        {
            if (cld[2] != 23)
                return;

            if (cld[1] == 71)
            {
                nc.GCompens.v = cld[10] == 24 ? 42 : 41;
                nc.H.v = nc.H.v0 = cld[3];
            }
            else
            {
                nc.GCompens.v = 40;
            }
            nc.GCompens.v0 = nc.GCompens.v;
            CompensNeedOut = true;
        }

        public override void OnEDMMove(ICLDEDMMoveCommand cmd, CLDArray cld)
        {
            var mode = cld["Mode"];
            var ep1X = cld["Ep1X"];
            var ep1Y = cld["Ep1Y"];
            var ep1Z = cld["Ep1Z"];
            var ep2X = cld["Ep2X"];
            var ep2Y = cld["Ep2Y"];
            var ep2Z = cld["Ep2Z"];
            var span1 = cld["Span1"];
            var span2 = cld["Span2"];
            var r1 = cld["R1"];
            var r2 = cld["R2"];
            var pc1X = cld["Pc1X"];
            var pc1Y = cld["Pc1Y"];
            var pc2X = cld["Pc2X"];
            var pc2Y = cld["Pc2Y"];
            var taperAngle = (double)cld["A"];
            var rollMode = cld["RollMode"];
            var rollR1 = cld["RollR1"];
            var rollR2 = cld["RollR2"];

            // Insert and break wire commands
            if (mode != 0 && !WireInserted)
            {
                InsertWire();
            }
            if (mode == 0 && WireInserted)
            {
                var lowerPointChanged = ep1X != Fp1X || ep1Y != Fp1Y || ep1Z != Fp1Z;
                if (lowerPointChanged)
                    BreakWire();
            }

            // Cutting conditions selection
            if (ConditionsNeedOut)
            {
                nc.C.Show();
                nc.Block.Out();
                ConditionsNeedOut = false;
            }

            // Compensation turn on
            if (CompensNeedOut && nc.GCompens != 40)
            {
                nc.GCompens.Show();
                nc.H.Show();
                nc.Block.Out();
                CompensNeedOut = false;
            }

            // Motion mode turn on
            if (mode == 3)
            {
                nc.G2Contour.v = 61;
            }
            else if (mode == 4)
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
            if (CompensNeedOut && nc.GCompens == 40)
            {
                nc.GCompens.Show();
                CompensNeedOut = false;
            }

            // Coordinates output
            nc.X1.v = ep1X;
            nc.Y1.v = ep1Y;
            nc.Z1.v = ep1Z;
            switch (mode)
            {
                case 0: // Rapid
                    if (nc.X1.ValuesDiffer || nc.Y1.ValuesDiffer || nc.Z1.ValuesDiffer)
                    {
                        nc.GInterp1.v = 0;
                    }
                    break;
                case 1: // 2Axis
                case 2: // Taper
                case 3: // 2Contour
                    if (span1 == 1) // Arc
                    {
                        nc.GInterp1.v = r1 > 0d ? 3 : 2;
                        nc.I1.Show(pc1X - Fp1X);
                        nc.J1.Show(pc1Y - Fp1Y);
                    }
                    else // Cut
                    {
                        nc.GInterp1.v = 1;
                    }
                    if (mode == 2) // Taper
                    {
                        nc.GTaper.v = taperAngle > 0 ? 52 : 51;
                        nc.A.Show(Abs(taperAngle));
                    }
                    else if (mode == 3) // 2Contours
                    {
                        nc.Colon.Show();
                        nc.X2.v = ep2X;
                        nc.Y2.v = ep2Y;
                        nc.Z2.v = ep2Z;
                        if (span2 == 1) // Arc
                        {
                            nc.GInterp2.v = r2 > 0d ? 3 : 2;
                            nc.I2.Show(pc2X - Fp2X);
                            nc.J2.Show(pc2Y - Fp2Y);
                        }
                        else // Cut
                        {
                            nc.GInterp2.v = 1;
                        }
                        // CornerR
                        if ((mode == 1 || mode == 2) && rollMode > 0)
                        {
                            nc.RollR1.Show(rollR1);
                            nc.RollR2.Show(rollR2);
                        }
                    }
                    break;
                case 4: // 4Axis_UV
                    nc.GInterp1.v = 1;
                    nc.U.v = ep2X - ep1X;
                    nc.V.v = ep2Y - ep1Y;
                    nc.W.v = ep2Z - ep1Z;
                    break;
            }

            // Motion mode turn off
            if (mode != 2 && nc.GTaper != 50)
            {
                nc.GTaper.v = 50;
            }
            if (mode != 3 && nc.G2Contour != 60)
            {
                nc.G2Contour.v = 60;
            }
            if (mode != 4 && nc.GUV != 75)
            {
                nc.U.v = 0;
                nc.V.v = 0;
            }
            nc.Block.Out();
            if (mode != 4 && nc.GUV != 75)
            {
                nc.GUV.v = 75;
                nc.U.Hide();
                nc.V.Hide();
            }
            nc.Block.Out();

            // Remember current coordinates
            Fp1X = ep1X;
            Fp1Y = ep1Y;
            Fp1Z = ep1Z;
            if (mode < 3) // 2D
            {
                Fp2X = Fp1X;
                Fp2Y = Fp1Y;
                Fp2Z = Fp1Z;
            }
            else // 4D
            {
                Fp2X = ep2X;
                Fp2Y = ep2Y;
                Fp2Z = ep2Z;
            }
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            nc.C.v = nc.C.v0 = cld[2];
            ConditionsNeedOut = true;
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
            if (WireInserted)
            {
                BreakWire();
            }

            // M02
            nc.MStop.Show(02);
            nc.Block.Out();
            nc.Output("");

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
            if (cld[4] != 1079)
                return;

            nc.GCS.Show(92);
            nc.X1.Show(Fp1X - (cld[1] - LCSX));
            nc.Y1.Show(Fp1Y - (cld[2] - LCSY));
            nc.Z1.Show(Fp1Z - (cld[3] - LCSZ));
            LCSX = cld[1];
            LCSY = cld[2];
            LCSZ = cld[3];
            nc.Block.Out();

            // Current coordinates updating
            Fp1X = nc.X1;
            Fp1Y = nc.Y1;
            Fp1Z = nc.Z1;
            Fp2X = Fp1X;
            Fp2Y = Fp1Y;
            Fp2Z = Fp1Z;
        }

        public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
        {
            switch (cld[1])
            {
                case 58: // TechInfo
                    nc.Output("(Rapid level       = " + Str((double)cld[9]) + ")");
                    nc.Output("(Upper guide level = " + Str((double)cld[47]) + ")");
                    nc.Output("(Upper work level  = " + Str((double)cld[10]) + ")");
                    nc.Output("(Lower work level  = " + Str((double)cld[11]) + ")");
                    nc.Output("(Lower guide level = " + Str((double)cld[48]) + ")");
                    nc.Output("(Wire diameter     = " + Str((double)cld[27]) + ")");
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
                    if (WireInserted)
                    {
                        BreakWire();
                    }
                    nc.MSub.Show(99);
                    nc.Block.Out();
                    nc.Output("");
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
}
