namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;
    }

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        
        double MaxReal, ACC_CP, DEF_ACC, X_prev, Y_prev, Z_prev, A_prev, B_prev, C_prev, 
               E1_prev, E2_prev, E3_prev, E4_prev, E5_prev, E6_prev;

        int Num_E6POS, Num_E6AXIS, Num_CPDAT, Num_PPDAT, MAX_num_line, Current_num_file, big_small_file, mid_end, State, Turn, 
            block_src1, block_dat1, PTP_main_axes, Vel_PTP, Acc_PTP, APO_CDIS, APO_CPT, WorkpieceHolder;

        string Current_NAME_file, outstr;

        int[] AIndex = new int[6] { 0, 1, 2, 3, 4, 5 };

        ///<summary>Current src-file</summary>
        NCFile src;
        ///<summary>Current dat-file</summary>
        NCFile dat;
 
        #endregion

        public override void OnStartProject(ICLDProject prj)
        {

        }

        public override void OnFinishProject(ICLDProject prj)
        {
            
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            src.WriteLine();
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            src.VEL_CP.v = cld[1] / 1000;
            src.VEL_CP.v = Round(src.VEL_CP.v / 60, 3);
            outstr = src.VEL_CP.ToString();
            if (outstr.Length > 0) SectOutput(block_src1, outstr+"; m/sec; " + Str(src.VEL_CP.v * 60 * 1000) + " mm/min");
        }

        public override void OnFini(ICLDFiniCommand cmd, CLDArray cld)
        {
            // ---.SRC----
            // ChangeNcFile Current_NAME_file + ".src"
            SectOutput(block_src1, "END");
            head_src();

            // ---.DAT----
            // ChangeNcFile Current_NAME_file + ".dat"
            SectOutput(block_dat1, "ENDDAT");
            head_dat();
        }

        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            A1_A6(11, cld[10], cld);
            src.A1.v0 = src.A1;
            src.A2.v0 = src.A2;
            src.A3.v0 = src.A3;
            src.A4.v0 = src.A4;
            src.A5.v0 = src.A5;
            src.A6.v0 = src.A6;

            Ext_Axis(cmd);
            src.E1.v0 = src.E1;
            src.E2.v0 = src.E2;
            src.E3.v0 = src.E3;
            src.E4.v0 = src.E4;
            src.E5.v0 = src.E5;
            src.E6.v0 = src.E6;
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            if (WorkpieceHolder == 0)
            {
                src.TOOL_DATA.v = cmd.Number;
                if (src.TOOL_DATA.v != src.TOOL_DATA.v0)
                {
                    if (src.TOOL_DATA.v == 0)
                    {
                        src.Output("$TOOL = $NULLFRAME");
                        src.TOOL_DATA.v = 0;
                        src.TOOL_DATA.v0 = 0;
                    }
                    else
                    {
                        SectOutput(block_src1, "$ACT_TOOL=" + src.TOOL_DATA.ToString());
                        SectOutput(block_src1, "$TOOL=TOOL_DATA[" + src.TOOL_DATA.ToString() + "]");
                        SectOutput(block_src1, "$LOAD=LOAD_DATA[" + src.TOOL_DATA.ToString() + "]");
                    }
                }
            }
        }

        public override void OnMultiArc(ICLDMultiArcCommand cmd, CLDArray cld)
        {
            if (Num_E6POS == 0) if_first_point();
            Num_CPDAT = Num_CPDAT + 1;
            Num_E6POS = Num_E6POS + 1;

            //------------MID-DAT----------------
            src.X.v = cld[8];  src.Y.v = cld[9];  src.Z.v = cld[10];
            src.A.v = cld[11]; src.B.v = cld[12]; src.C.v = cld[13];
            RoundXYZABC();
            mid_end = 1;
 
            Point_Position("MidPos.MachineStateFlags","circ1", cmd);
            Ext_Axis(cmd);
            SectOutput(block_dat1, "DECL E6POS XP" + Num_E6POS.ToString() + "={X " + src.X.ToString() + ",Y " + 
                       src.Y.ToString() + ",Z " + src.Z.ToString() + ",A "+ src.A.ToString() + ",B " + src.B.ToString() +",C " + 
                       src.C.ToString() +",S " + State.ToString() + ",T " + Turn.ToString() + ",E1 " + src.E1.ToString() + ",E2 " + 
                       src.E2.ToString() + ",E3 " + src.E3.ToString() + ",E4 " + src.E4.ToString() + ",E5 " + src.E5.ToString() + ",E6 " + 
                       src.E6.ToString() + "}");
            //------------END-DAT----------------
            src.X.v = cld[1];  src.Y.v = cld[2];  src.Z.v = cld[3];
            src.A.v = cld[4]; src.B.v = cld[5]; src.C.v = cld[6];
            RoundXYZABC();
            mid_end = 2;
            Point_Position("EndPos.MachineStateFlags","circ2", cmd);
            Ext_Axis(cmd);

            SectOutput(block_dat1, "DECL E6POS XP" + (Num_E6POS+1).ToString() + "={X " + src.X.ToString() + ",Y " + 
                       src.Y.ToString() + ",Z " + src.Z.ToString() + ",A "+ src.A.ToString() + ",B " + src.B.ToString() + ",C " + 
                       src.C.ToString() + ",S " + State.ToString() + ",T " + Turn.ToString() + ",E1 " + src.E1.ToString() + 
                       ",E2 " + src.E2.ToString() + ",E3 " + src.E3.ToString() + ",E4 " + src.E4.ToString() + ",E5 " + src.E5.ToString() + 
                       ",E6 " + src.E6.ToString() + "}");
            SectOutput(block_dat1, "DECL FDAT FP" + (Num_E6POS + 1).ToString() + "={TOOL_NO " + Str(src.TOOL_DATA) + ",BASE_NO " + 
                       Str(src.BASE_DATA) + ",IPO_FRAME #BASE,POINT2[] "+ "34" + "XP" + Num_E6POS.ToString() + "34" +",TQ_STATE FALSE}");
            SectOutput(block_dat1, "DECL LDAT LCPDAT" + Num_CPDAT.ToString() + "={VEL " + Str(src.VEL_CP.v) + ",ACC " + 
                       (ACC_CP * 100 / DEF_ACC).ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".000,APO_FAC 50.0000,AXIS_VEL " + 
                       Vel_PTP.ToString() + ".000,AXIS_ACC " + Acc_PTP.ToString() + ".000,ORI_TYP #VAR,CIRC_TYP #BASE,JERK_FAC 50.0000,GEAR_JERK 50.0000,EXAX_IGN 0}");
            SectOutput(block_dat1, "");
            //-------------SRC----------------
            SectOutput(block_src1, ";FOLD CIRC P" + Num_E6POS.ToString() + " P" + (Num_E6POS+1).ToString() + " CONT Vel=" + Str(src.VEL_CP.v) + 
                       " m/s CPDAT" + Num_CPDAT.ToString() + " Tool[" + Str(src.TOOL_DATA) + "]:Tool_main Base[" + Str(src.BASE_DATA) + 
                       "]:Base_main;%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VCIRC, %P 1:CIRC, 2:P" + Num_E6POS.ToString() + ", 3:P" + 
                       (Num_E6POS+1).ToString() + ", 4:C_DIS C_DIS, 6:" + Str(src.VEL_CP.v) + ", 8:CPDAT" + Num_CPDAT.ToString());
            SectOutput(block_src1, "$BWDSTART=FALSE");
            SectOutput(block_src1, "LDAT_ACT=LCPDAT" + Num_CPDAT.ToString());
            SectOutput(block_src1, "FDAT_ACT=FP" + (Num_E6POS+1).ToString());
            SectOutput(block_src1, "BAS(#CP_PARAMS," + Str(src.VEL_CP.v) + ")");
            SectOutput(block_src1, "CIRC XP" + Num_E6POS.ToString() + ", XP" + (Num_E6POS+1).ToString() + " C_DIS C_DIS");
            SectOutput(block_src1, ";ENDFOLD");
            SectOutput(block_src1, "");
            //--------------------------------

            Num_E6POS = Num_E6POS + 1;

            X_prev = src.X.v; Y_prev = src.Y.v; Z_prev = src.Z.v; A_prev = src.A.v; B_prev = src.B.v; C_prev = src.C.v;
            E1_prev = src.E1.v; E2_prev = src.E2.v; E3_prev = src.E3.v; E4_prev = src.E4.v; E5_prev = src.E5.v; E6_prev = src.E6.v;

            if (Num_CPDAT >= MAX_num_line)
            {
                //---.SRC----
                // ChangeNcFile Current_NAME_file + ".src"
                SectOutput(block_src1, "END");
                head_src();
                //---.DAT----
                //ChangeNcFile Current_NAME_file + ".dat"
                SectOutput(block_dat1, "ENDDAT");
                head_dat();
            }  
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            src.X.v = cld[1];  src.Y.v = cld[2];  src.Z.v = cld[3];
            src.A.v = cld[4]; src.B.v = cld[5]; src.C.v = cld[6];
            Point_Position("MachineStateFlags","line", cmd);
            
            RoundXYZABC();
            Ext_Axis(cmd);

            if (Math.Abs(src.X.v - X_prev) > 0.001 || Math.Abs(src.Y.v - Y_prev) > 0.001 || Math.Abs(src.Z.v - Z_prev) > 0.001 || 
                Math.Abs(src.A.v - A_prev) > 0.001 || Math.Abs(src.B.v - B_prev) > 0.001 || Math.Abs(src.C.v - C_prev) > 0.001 || 
                Math.Abs(src.E1.v - E1_prev) > 0.001 || Math.Abs(src.E2.v - E2_prev) > 0.001 || Math.Abs(src.E3.v - E3_prev) > 0.001 || 
                Math.Abs(src.E4.v - E4_prev) > 0.001 || Math.Abs(src.E5.v - E5_prev) > 0.001 || Math.Abs(src.E6.v - E6_prev) > 0.001)
            {
                Num_E6POS = Num_E6POS + 1;
                Num_CPDAT = Num_CPDAT + 1;
                //-------------SRC----------------
                SectOutput(block_src1, ";FOLD LIN P" + Num_E6POS.ToString() + " CONT Vel=" + Str(src.VEL_CP.v) + " m/s CPDAT" + 
                           Num_CPDAT.ToString() + " Tool[" + Str(src.TOOL_DATA) + "]:Tool_main Base[" + Str(src.BASE_DATA) + 
                           "]:Base_main;%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VLIN, %P 1:LIN, 2:P" + Num_E6POS.ToString() + 
                           ", 3:C_DIS C_DIS,5:" + Str(src.VEL_CP.v) + ",7:CPDAT" + Num_CPDAT.ToString());
                SectOutput(block_src1, "$BWDSTART=FALSE");
                SectOutput(block_src1, "LDAT_ACT=LCPDAT" + Num_CPDAT.ToString() + "");
                SectOutput(block_src1, "FDAT_ACT=FP" + Num_E6POS.ToString() + "");
                SectOutput(block_src1, "BAS(#CP_PARAMS," + Str(src.VEL_CP.v) + ")");
                SectOutput(block_src1, "LIN XP" + Num_E6POS.ToString() + " C_DIS C_DIS");
                SectOutput(block_src1, ";ENDFOLD");
                SectOutput(block_src1, "");

                //------------DAT-----------------
                SectOutput(block_dat1, "DECL E6POS XP" + Num_E6POS.ToString() + "={X " + src.X.ToString() + ",Y " + src.Y.ToString() + 
                           ",Z " + src.Z.ToString() + ",A " + src.A.ToString() + ",B " + src.B.ToString() + ",C " + src.C.ToString() + 
                           ",S "+ State.ToString() + ",T " + Turn.ToString()+ ",E1 " + src.E1.ToString() + ",E2 " + src.E2.ToString() + 
                           ",E3 "+ src.E3.ToString() +",E4 "+ src.E4.ToString() + ",E5 " + src.E5.ToString() +",E6 " + src.E6.ToString() + "}");
                SectOutput(block_dat1, "DECL FDAT FP" + Num_E6POS.ToString() + "={TOOL_NO " + src.TOOL_DATA.ToString() + ",BASE_NO " + 
                           src.BASE_DATA.ToString() + ",IPO_FRAME #BASE,POINT2[] " + "34" + " " + "34" + ",TQ_STATE FALSE}");
                SectOutput(block_dat1, "DECL LDAT LCPDAT" + Num_CPDAT.ToString() + "={VEL " + Str(src.VEL_CP.v) + ",ACC " + 
                           (ACC_CP * 100 / DEF_ACC).ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".00000,APO_FAC 50.0000,AXIS_VEL " + 
                           Vel_PTP.ToString() + ".000,AXIS_ACC " + Acc_PTP.ToString() + 
                           ".000,ORI_TYP #VAR,CIRC_TYP #BASE,JERK_FAC 50.0000,GEAR_JERK 50.0000,EXAX_IGN 0}");
                SectOutput(block_dat1, "");
                //--------------------------------

                X_prev = src.X.v; Y_prev = src.Y.v; Z_prev = src.Z.v; A_prev = src.A.v; B_prev = src.B.v; C_prev = src.C.v;
                E1_prev = src.E1.v; E2_prev = src.E2.v; E3_prev = src.E3.v; E4_prev = src.E4.v; E5_prev = src.E5.v; E6_prev = src.E6.v;

                if (Num_CPDAT >= MAX_num_line)
                {
                    //---.SRC----
                    // ChangeNcFile Current_NAME_file + ".src"
                    SectOutput(block_src1, "END");
                    head_src();
                    // !---.DAT----
                    // ChangeNcFile Current_NAME_file + ".dat"
                    SectOutput(block_dat1, "ENDDAT");
                    head_dat();
                }
            }
        }

        public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
        {
            if (cmd.Flt["CSNumber"] >= 53) src.BASE_DATA.v = cmd.Flt["CSNumber"] - 53;
            else src.BASE_DATA.v = cmd.Flt["CSNumber"];
            
            if (src.BASE_DATA.v != src.BASE_DATA.v0)
            {
                if (cld[1] == 0 && cld[2] == 0 && cld[3] == 0 && cld[6] == 0 && cld[7] == 0 && cld[8] == 0)
                {
                    SectOutput(block_src1, "$BASE = $WORLD");
                    src.BASE_DATA.v = 0;
                    src.BASE_DATA.v0 = src.BASE_DATA.v;
                }
                else SectOutput(block_src1, "$BASE=BASE_DATA[" + src.BASE_DATA.ToString() + "]");
            }
        }

        public override void OnPartNo(ICLDPartNoCommand cmd, CLDArray cld)
        {
            MaxReal = 9999999;

            Vel_PTP = 20;
            Acc_PTP = 20;
            APO_CDIS = 2;
            APO_CPT = 3;
            ACC_CP = 1;
            DEF_ACC = 2.3;
            
            // От использования следующих двух переменных в принципе можно избавится
            block_src1 = 1;
            block_dat1 = 3;

            MAX_num_line = Settings.Params.Int["OutFiles.MaxLinesCount"];

            var prj = CLDProject;
            string dir = Settings.Params.Str["OutFiles.NCFilesOutDir"];
            if (String.IsNullOrEmpty(dir))
                dir = prj.FilePath;
            string progName = Settings.Params.Str["OutFiles.ProgName"];
            if (String.IsNullOrEmpty(progName))
                progName = prj.ProjectName;
            src = new NCFile();
            dat = new NCFile();
            src.OutputFileName =  Path.Combine(dir, progName + ".src");
            dat.OutputFileName =  Path.Combine(dir, progName + ".dat");
            Current_NAME_file = progName.ToUpper();


            // Big_small_file();
            // if (Big_small_file == 1)
            // {
            //     Current_NAME_file = UPCASE(NCName$) + current_num_file.ToString();
            // }
            // else Current_NAME_file = UPCASE(NCName$);
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            PTP_main_axes = 0;

            if (cmd.Ptr["Axes(AxisA1Pos)"] != null) PTP_main_axes = PTP_main_axes + 1;
            if (cmd.Ptr["Axes(AxisA2Pos)"] != null) PTP_main_axes = PTP_main_axes + 1;
            if (cmd.Ptr["Axes(AxisA3Pos)"] != null) PTP_main_axes = PTP_main_axes + 1;
            if (cmd.Ptr["Axes(AxisA4Pos)"] != null) PTP_main_axes = PTP_main_axes + 1;
            if (cmd.Ptr["Axes(AxisA5Pos)"] != null) PTP_main_axes = PTP_main_axes + 1;
            if (cmd.Ptr["Axes(AxisA6Pos)"] != null) PTP_main_axes = PTP_main_axes + 1;
            A1_A6(11, cld[10], cld);

            Ext_Axis(cmd);

            if (PTP_main_axes > 0)
            {
                //-------------SRC----------------
                Num_E6POS = Num_E6POS + 1;
                Num_PPDAT = Num_PPDAT + 1;

                SectOutput(block_src1, ";FOLD PTP P" + Num_E6POS.ToString() + " Vel=" + Vel_PTP.ToString() + " % PDAT" + 
                           Num_PPDAT.ToString() + " Tool[" + Str(src.TOOL_DATA) + "]:Tool_main Base[" + Str(src.BASE_DATA) + 
                           "]:Base_main;%{PE};%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VPTP,%P 1:PTP, 2:P" + 
                           Num_E6POS.ToString() + ", 3:, 5:" + Vel_PTP.ToString() + ", 7:PDAT" + Num_PPDAT.ToString());
                SectOutput(block_src1, "$BWDSTART=FALSE");
                SectOutput(block_src1, "PDAT_ACT=PPDAT" + Num_PPDAT.ToString());
                SectOutput(block_src1, "FDAT_ACT=FP" + Num_E6POS.ToString());
                SectOutput(block_src1, "BAS(#PTP_PARAMS," + Vel_PTP.ToString() + ")");
                SectOutput(block_src1, "PTP XP" + Num_E6POS.ToString());
                SectOutput(block_src1, ";ENDFOLD");
                SectOutput(block_src1, "");

                //------------DAT-----------------
                SectOutput(block_dat1, "DECL E6AXIS XP" + Num_E6POS.ToString() + "={A1 " + src.A1.ToString() + ",A2 " + src.A2.ToString() + 
                           ",A3 " + src.A3.ToString() + ",A4 " + src.A4.ToString() + ",A5 " + src.A5.ToString() + ",A6 " + src.A6.ToString() + 
                           ",E1 " + src.E1.ToString() + ",E2 " + src.E2.ToString() + ",E3 " + src.E3.ToString() + ",E4 " + src.E4.ToString() + 
                           ",E5 " + src.E5.ToString() + ",E6 " + src.E6.ToString() + "}");
                SectOutput(block_dat1, "DECL FDAT FP" + Num_E6POS.ToString() + "={TOOL_NO " + Str(src.TOOL_DATA) + ",BASE_NO " + 
                           Str(src.BASE_DATA) + ",IPO_FRAME #BASE,POINT2[] " + "34" + " " + "34" + ",TQ_STATE FALSE}");
                SectOutput(block_dat1, "DECL PDAT PPDAT" + Num_PPDAT.ToString() + "={VEL " + Vel_PTP.ToString() + ",ACC " + 
                           Acc_PTP.ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".0,APO_MODE #CDIS,GEAR_JERK 50.0000,EXAX_IGN 0}");
                SectOutput(block_dat1, "");
            }
        }

        public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
        {
            if (cld[1] == 58)
            {
                if (cmd.Str["PPFun(TechInfo).Tool.RevolverID"].IndexOf("ExtTool") != 0) WorkpieceHolder = 1;
                if (cmd.Str["PPFun(TechInfo).Tool.RevolverID"].IndexOf("TableTool") != 0) WorkpieceHolder = 1;
            }
        }

        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {
            src.VEL_CP.v = cld[1] / 1000;
            src.VEL_CP.v = Round(src.VEL_CP.v / 60, 3);
            outstr = src.VEL_CP.ToString();
            if (outstr.Length > 0) SectOutput(block_src1, outstr+"; m/sec; " + Str(src.VEL_CP.v * 60 * 1000) + " mm/min");
        }

        public void Big_small_file()
        {
            
        }

        public void RoundXYZABC()
        {
            if (src.X.v > -0.001 && src.X.v < 0) src.X.v = 0;
            if (src.Y.v > -0.001 && src.Y.v < 0) src.Y.v = 0;
            if (src.Z.v > -0.001 && src.Z.v < 0) src.Z.v = 0;
            if (src.A.v > -0.001 && src.A.v < 0) src.A.v = 0;
            if (src.B.v > -0.001 && src.B.v < 0) src.B.v = 0;
            if (src.C.v > -0.001 && src.C.v < 0) src.C.v = 0;
        }

        public void Point_Position(string prm, string move_type, ICLDCommand cmd)
        {
            int ElbowFlag, fBase, fElbow, fWrist;
            ElbowFlag = cmd.Int[prm];
            
            //-----State------
            if (ElbowFlag == 1 || ElbowFlag == 3 || ElbowFlag == 5) fWrist = 1;
            else fWrist = 0;
            if (ElbowFlag == 2 || ElbowFlag == 3 || ElbowFlag == 6 || ElbowFlag == 7) fElbow = 0;
            else fElbow = 1;
            if (ElbowFlag >= 4) fBase = 1;
            else fBase = 0;
            State = fBase + 2*fElbow + 4*fWrist;

            //----Turn------
            if (move_type == "line" || move_type == "ptp")
            {
                if (cmd.Ptr["Axes(AxisA1Pos)"] is not null) src.A1.v = cmd.Flt["Axes(AxisA1Pos).Value"];
                if (cmd.Ptr["Axes(AxisA2Pos)"] is not null) src.A2.v = cmd.Flt["Axes(AxisA2Pos).Value"];
                if (cmd.Ptr["Axes(AxisA3Pos)"] is not null) src.A3.v = cmd.Flt["Axes(AxisA3Pos).Value"];
                if (cmd.Ptr["Axes(AxisA4Pos)"] is not null) src.A4.v = cmd.Flt["Axes(AxisA4Pos).Value"];
                if (cmd.Ptr["Axes(AxisA5Pos)"] is not null) src.A5.v = cmd.Flt["Axes(AxisA5Pos).Value"];
                if (cmd.Ptr["Axes(AxisA6Pos)"] is not null) src.A6.v = cmd.Flt["Axes(AxisA6Pos).Value"];
            }
            else if (move_type == "circ1")
            {
                if (cmd.Ptr["MidPos.Axes(AxisA1Pos)"] is not null) src.A1.v = cmd.Flt["MidPos.Axes(AxisA1Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA2Pos)"] is not null) src.A2.v = cmd.Flt["MidPos.Axes(AxisA2Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA3Pos)"] is not null) src.A3.v = cmd.Flt["MidPos.Axes(AxisA3Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA4Pos)"] is not null) src.A4.v = cmd.Flt["MidPos.Axes(AxisA4Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA5Pos)"] is not null) src.A5.v = cmd.Flt["MidPos.Axes(AxisA5Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA6Pos)"] is not null) src.A6.v = cmd.Flt["MidPos.Axes(AxisA6Pos).Value"];
            }
            else if (move_type == "circ2")
            {
                if (cmd.Ptr["EndPos.Axes(AxisA1Pos)"] is not null) src.A1.v = cmd.Flt["EndPos.Axes(AxisA1Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA2Pos)"] is not null) src.A2.v = cmd.Flt["EndPos.Axes(AxisA2Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA3Pos)"] is not null) src.A3.v = cmd.Flt["EndPos.Axes(AxisA3Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA4Pos)"] is not null) src.A4.v = cmd.Flt["EndPos.Axes(AxisA4Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA5Pos)"] is not null) src.A5.v = cmd.Flt["EndPos.Axes(AxisA5Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA6Pos)"] is not null) src.A6.v = cmd.Flt["EndPos.Axes(AxisA6Pos).Value"];
            }

            src.A1.v0 = src.A1.v; src.A2.v0 = src.A2.v; src.A3.v0 = src.A3.v; src.A4.v0 = src.A4.v; src.A5.v0 = src.A5.v; src.A6.v0 = src.A6.v;

            Turn = 0;
            if (src.A1.v < 0) Turn = Turn + 1;
            if (src.A2.v < 0) Turn = Turn + 2;
            if (src.A3.v < 0) Turn = Turn + 4;
            if (src.A4.v < 0) Turn = Turn + 8;
            if (src.A5.v < 0) Turn = Turn + 16;
            if (src.A6.v < 0) Turn = Turn + 32;
        }

        public void if_first_point()
        {
            Num_E6POS = Num_E6POS + 1;
            Num_CPDAT = Num_CPDAT + 1;
            //-------------SRC----------------
            SectOutput(block_src1, ";FOLD LIN P" + Num_E6POS.ToString() + " CONT Vel=" + Str(src.VEL_CP.v) + " m/s CPDAT" + 
                       Num_CPDAT.ToString() + " Tool[" + src.TOOL_DATA.ToString() + "]:Tool_main Base[" + 
                       src.BASE_DATA.ToString() + "]:Base_main;%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VLIN, %P 1:LIN, 2:P" + 
                       Num_E6POS.ToString() + ", 3:C_DIS C_DIS,5:" + Str(src.VEL_CP.v) + ",7:CPDAT" + Num_CPDAT.ToString());
            SectOutput(block_src1, "$BWDSTART=FALSE");
            SectOutput(block_src1, "LDAT_ACT=LCPDAT" + Num_CPDAT.ToString() + "");
            SectOutput(block_src1, "FDAT_ACT=FP" + Num_E6POS.ToString() + "");
            SectOutput(block_src1, "BAS(#CP_PARAMS," + Str(src.VEL_CP.v) + ")");
            SectOutput(block_src1, "LIN XP" + Num_E6POS.ToString() + " C_DIS C_DIS");
            SectOutput(block_src1, ";ENDFOLD");
            SectOutput(block_src1, "");
            //------------DAT-----------------
            SectOutput(block_dat1, "DECL E6POS XP" + Num_E6POS.ToString() + "={X " + src.X.ToString() + ",Y " + src.Y.ToString() + 
                       ",Z " + src.Z.ToString() + ",A " + src.A.ToString() + ",B " + src.B.ToString() + ",C " + src.C.ToString() + 
                       ",S " + State.ToString() + ",T " + Turn.ToString() + ",E1 " + src.E1.ToString() + ",E2 " + src.E2.ToString() + 
                       ",E3 " + src.E3.ToString() + ",E4 " + src.E4.ToString() + ",E5 " + src.E5.ToString() + ",E6 " + src.E6.ToString() + "}");
            SectOutput(block_dat1, "DECL FDAT FP" + Num_E6POS.ToString() + "={TOOL_NO " + src.TOOL_DATA.ToString() + ",BASE_NO " + 
                       src.BASE_DATA.ToString() + ",IPO_FRAME #BASE,POINT2[] " + "34" + " " + "34" + ",TQ_STATE FALSE}");
            SectOutput(block_dat1, "DECL LDAT LCPDAT" + Num_CPDAT.ToString() + "={VEL "+ Str(src.VEL_CP.v) + ",ACC " + 
                       (ACC_CP * 100 / DEF_ACC).ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".00000,APO_FAC 50.0000,AXIS_VEL " + 
                       Vel_PTP.ToString() + ".000,AXIS_ACC " + Acc_PTP.ToString() + ".000,ORI_TYP #VAR,CIRC_TYP #BASE,JERK_FAC 50.0000,GEAR_JERK 50.0000,EXAX_IGN 0}");
            SectOutput(block_dat1, "");
        }

        public void SectOutput(int SectID, string NCString)
        {
            switch (SectID)
            {
                case 1:
                    src.WriteLine(NCString);
                    break;
                case 2:
                    throw new Exception("Output to section 2 not implemented");
                case 3:
                    dat.WriteLine(NCString);
                    break;
                case 4:
                    throw new Exception("Output to section 4 not implemented");
            }
        }

        public void head_src()
        {
            string begin_src_file = $@"DEF {Current_NAME_file}() 
decl int i
GLOBAL INTERRUPT DECL 3 WHEN $STOPMESS==TRUE DO IR_STOPM ( )
INTERRUPT ON 3
;global Interrupt Decl 10 when ParStopAndRetract == 1 Do InterruptProg10()
;ParStopAndRetract=0
;INTERRUPT ON 10
BAS (#INITMOV,0)""
;dry_run=1""
FOR I=1 TO 6
   $VEL_AXIS[I]="" {Vel_PTP.ToString()}
   $ACC_AXIS[I]="" {Acc_PTP.ToString()}
ENDFOR
$ADVANCE=5
;--SET LIN AND ARC MOTION VARIABLES--
$VEL.CP=0.05 ; m/s
$ACC.CP="" {ACC_CP.ToString()} ; m/s^2
;REAL DEF_ACC_CP="" {DEF_ACC.ToString()}
$VEL.ORI1=200 ; grad/s
$VEL.ORI2=200 ; grad/s
$ACC.ORI1=200 ; grad/s^2
$ACC.ORI2=200 ; grad/s^2
; --approximation--
$APO.CDIS = "" {APO_CDIS.ToString()} "".0 ; mm, C_DIS
$APO.CPTP = "" {APO_CPT.ToString()} "" ; %, C_PTP
$APO.CVEL = 50 ; %, c_vel
; SET POSITIONING CRITERIA
$ORI_TYPE = #VAR; #VAR, #CONSTANT, #JOINT
$CIRC_TYPE = #base; #PATH, #BASE""";
                    
            src.Output(begin_src_file, src.FileStart);
        }

        public void head_dat()
        {
            string begin_dat_file = $@"&ACCESS RVP
&REL 43
&PARAM EDITMASK = *
&PARAM TEMPLATE = C:\\KRC\\Roboter\\Template\vorgabe
DEFDAT {Current_NAME_file}
;FOLD EXTERNAL DECLARATIONS;%{{PE}}%MKUKATPBASIS,%CEXT,%VCOMMON,%P
;FOLD BASISTECH EXT;%{{PE}}%MKUKATPBASIS,%CEXT,%VEXT,%P
EXT  BAS (BAS_COMMAND  :IN,REAL  :IN )
DECL INT SUCCESS
;ENDFOLD (BASISTECH EXT)
;FOLD USER EXT;%{{E}}%MKUKATPUSER,%CEXT,%VEXT,%P
;Make your modifications here

;ENDFOLD (USER EXT)
;ENDFOLD (EXTERNAL DECLARATIONS)
;DECL BASIS_SUGG_T LAST_BASIS={{POINT1[] ""P53                     "",POINT2[] ""P53                     "",CP_PARAMS[] ""CPDAT41                 "",PTP_PARAMS[] ""PDAT4                   "",CONT[] ""C_DIS C_DIS             "",CP_VEL[] ""0.02                    "",PTP_VEL[] ""50                      "",SYNC_PARAMS[] ""SYNCDAT                 "",SPL_NAME[] ""S0                      "",A_PARAMS[] ""ADAT0                   ""}}";

            dat.Output(begin_dat_file, dat.FileStart);
        }

        public void A1_A6(int StartCLDIndex, int AxesCount, CLDArray cld)
        {
            int axi, cli;
            for (axi = 1; axi <= AxesCount; axi++)
            {
                cli = StartCLDIndex + (axi - 1) * 2;
                if (cld[cli] == AIndex[0]) src.A1.v = cld[cli + 1];
                if (cld[cli] == AIndex[1]) src.A2.v = cld[cli + 1];
                if (cld[cli] == AIndex[2]) src.A3.v = cld[cli + 1];
                if (cld[cli] == AIndex[3]) src.A4.v = cld[cli + 1];
                if (cld[cli] == AIndex[4]) src.A5.v = cld[cli + 1];
                if (cld[cli] == AIndex[5]) src.A6.v = cld[cli + 1];
            }
        }
        
        public void Ext_Axis(ICLDCommand cmd)
        {
            E1_prev = src.E1.v; E2_prev = src.E2.v; E3_prev = src.E3.v; E4_prev = src.E4.v; E5_prev = src.E5.v; E6_prev = src.E6.v;
            
            // ===E1====
            if (cmd.Ptr["Axes(ExtAxis1Pos)"] is not null)
            {
                src.E1.v = cmd.Flt["Axes(ExtAxis1Pos).Value"];
                src.E1.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis1Pos)"] is not null && mid_end == 1)
            {
                src.E1.v = cmd.Flt["MidPos.Axes(ExtAxis1Pos).Value"];
                src.E1.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis1Pos)"] is not null && mid_end == 2)
            {
                src.E1.v = cmd.Flt["EndPos.Axes(ExtAxis1Pos).Value"];
                src.E1.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E2====
            if (cmd.Ptr["Axes(ExtAxis2Pos)"] is not null)
            {
                src.E2.v = cmd.Flt["Axes(ExtAxis2Pos).Value"];
                src.E2.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis2Pos)"] is not null && mid_end == 1)
            {
                src.E2.v = cmd.Flt["MidPos.Axes(ExtAxis2Pos).Value"];
                src.E2.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis2Pos)"] is not null && mid_end == 2)
            {
                src.E2.v = cmd.Flt["EndPos.Axes(ExtAxis2Pos).Value"];
                src.E2.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E3====
            if (cmd.Ptr["Axes(ExtAxis3Pos)"] is not null)
            {
                src.E3.v = cmd.Flt["Axes(ExtAxis3Pos).Value"];
                src.E3.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis3Pos)"] is not null && mid_end == 1)
            {
                src.E3.v = cmd.Flt["MidPos.Axes(ExtAxis3Pos).Value"];
                src.E3.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis3Pos)"] is not null && mid_end == 2)
            {
                src.E3.v = cmd.Flt["EndPos.Axes(ExtAxis3Pos).Value"];
                src.E3.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E4====
            if (cmd.Ptr["Axes(ExtAxis4Pos)"] is not null)
            {
                src.E4.v = cmd.Flt["Axes(ExtAxis4Pos).Value"];
                src.E4.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis4Pos)"] is not null && mid_end == 1)
            {
                src.E4.v = cmd.Flt["MidPos.Axes(ExtAxis4Pos).Value"];
                src.E4.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis4Pos)"] is not null && mid_end == 2)
            {
                src.E4.v = cmd.Flt["EndPos.Axes(ExtAxis4Pos).Value"];
                src.E4.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E5====
            if (cmd.Ptr["Axes(ExtAxis5Pos)"] is not null)
            {
                src.E5.v = cmd.Flt["Axes(ExtAxis5Pos).Value"];
                src.E5.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis5Pos)"] is not null && mid_end == 1)
            {
                src.E5.v = cmd.Flt["MidPos.Axes(ExtAxis5Pos).Value"];
                src.E5.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis5Pos)"] is not null && mid_end == 2)
            {
                src.E5.v = cmd.Flt["EndPos.Axes(ExtAxis5Pos).Value"];
                src.E5.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E6====
            if (cmd.Ptr["Axes(ExtAxis6Pos)"] is not null)
            {
                src.E6.v = cmd.Flt["Axes(ExtAxis6Pos).Value"];
                src.E6.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis6Pos)"] is not null && mid_end == 1)
            {
                src.E6.v = cmd.Flt["MidPos.Axes(ExtAxis6Pos).Value"];
                src.E6.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis6Pos)"] is not null && mid_end == 2)
            {
                src.E6.v = cmd.Flt["EndPos.Axes(ExtAxis6Pos).Value"];
                src.E6.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }
        }


    }
}