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
            block_src1, block_src2, block_dat1, block_dat2, PTP_main_axes, Vel_PTP, Acc_PTP, APO_CDIS, APO_CPT, WorkpieceHolder;

        string Current_NAME_file, outstr;

        int[] SectLen = new int[6] { 0, 0, 0, 0, 0, 0 };
        int[] AIndex = new int[6] { 0, 1, 2, 3, 4, 5 };

        string[] Sect1 = new string[1000];
        string[] Sect2 = new string[1000];
        string[] Sect3 = new string[1000];
        string[] Sect4 = new string[1000];

        ///<summary>Current nc-file</summary>
        NCFile nc;
 
        #endregion

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine();
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            nc.VEL_CP.v = cld[1] / 1000;
            nc.VEL_CP.v = nc.VEL_CP.v / 60;
            outstr = nc.VEL_CP.ToString();
            if (outstr.Length > 0) SectOutput(block_src1, outstr+"; m/sec; " + (nc.VEL_CP.v * 60 * 1000).ToString() + " mm/min");
        }

        public override void OnFini(ICLDFiniCommand cmd, CLDArray cld)
        {
            // ---.SRC----
            // ChangeNcFile Current_NAME_file + ".src"
            SectOutput(block_src1, "END");
            head_src();
            OutSection(block_src1);

            // ---.DAT----
            // ChangeNcFile Current_NAME_file + ".dat"
            SectOutput(block_dat1, "ENDDAT");
            head_dat();
            OutSection(block_dat1);
        }

        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            A1_A6(11, cld[10], cld);
            nc.A1.v0 = nc.A1;
            nc.A2.v0 = nc.A2;
            nc.A3.v0 = nc.A3;
            nc.A4.v0 = nc.A4;
            nc.A5.v0 = nc.A5;
            nc.A6.v0 = nc.A6;

            Ext_Axis(cmd);
            nc.E1.v0 = nc.E1;
            nc.E2.v0 = nc.E2;
            nc.E3.v0 = nc.E3;
            nc.E4.v0 = nc.E4;
            nc.E5.v0 = nc.E5;
            nc.E6.v0 = nc.E6;
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            if (WorkpieceHolder == 0)
            {
                nc.TOOL_DATA.v = cmd.Number;
                if (nc.TOOL_DATA.v != nc.TOOL_DATA.v0)
                {
                    if (nc.TOOL_DATA.v == 0)
                    {
                        nc.Output("$TOOL = $NULLFRAME");
                        nc.TOOL_DATA.v = 0;
                        nc.TOOL_DATA.v0 = 0;
                    }
                    else
                    {
                        SectOutput(block_src1, "$ACT_TOOL=" + nc.TOOL_DATA.ToString());
                        SectOutput(block_src1, "$TOOL=TOOL_DATA[" + nc.TOOL_DATA.ToString() + "]");
                        SectOutput(block_src1, "$LOAD=LOAD_DATA[" + nc.TOOL_DATA.ToString() + "]");
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
            nc.X.v = cld[8];  nc.Y.v = cld[9];  nc.Z.v = cld[10];
            nc.A.v = cld[11]; nc.B.v = cld[12]; nc.C.v = cld[13];
            RoundXYZABC();
            mid_end = 1;
 
            Point_Position("MidPos.MachineStateFlags","circ1", cmd);
            Ext_Axis(cmd);
            SectOutput(block_dat1, "DECL E6POS XP" + Num_E6POS.ToString() + "={X " + nc.X.ToString() + ",Y " + 
                       nc.Y.ToString() + ",Z " + nc.Z.ToString() + ",A "+ nc.A.ToString() + ",B " + nc.B.ToString() +",C " + 
                       nc.C.ToString() +",S " + State.ToString() + ",T " + Turn.ToString() + ",E1 " + nc.E1.ToString() + ",E2 " + 
                       nc.E2.ToString() + ",E3 " + nc.E3.ToString() + ",E4 " + nc.E4.ToString() + ",E5 " + nc.E5.ToString() + ",E6 " + 
                       nc.E6.ToString() + "}");
            //------------END-DAT----------------
            nc.X.v = cld[1];  nc.Y.v = cld[2];  nc.Z.v = cld[3];
            nc.A.v = cld[4]; nc.B.v = cld[5]; nc.C.v = cld[6];
            RoundXYZABC();
            mid_end = 2;
            Point_Position("EndPos.MachineStateFlags","circ2", cmd);
            Ext_Axis(cmd);

            SectOutput(block_dat1, "DECL E6POS XP" + (Num_E6POS+1).ToString() + "={X " + nc.X.ToString() + ",Y " + 
                       nc.Y.ToString() + ",Z " + nc.Z.ToString() + ",A "+ nc.A.ToString() + ",B " + nc.B.ToString() + ",C " + 
                       nc.C.ToString() + ",S " + State.ToString() + ",T " + Turn.ToString() + ",E1 " + nc.E1.ToString() + 
                       ",E2 " + nc.E2.ToString() + ",E3 " + nc.E3.ToString() + ",E4 " + nc.E4.ToString() + ",E5 " + nc.E5.ToString() + 
                       ",E6 " + nc.E6.ToString() + "}");
            SectOutput(block_dat1, "DECL FDAT FP" + (Num_E6POS + 1).ToString() + "={TOOL_NO " + nc.TOOL_DATA.ToString() + ",BASE_NO " + 
                       nc.BASE_DATA.ToString() + ",IPO_FRAME #BASE,POINT2[] "+ "34" + "XP" + Num_E6POS.ToString() + "34" +",TQ_STATE FALSE}");
            SectOutput(block_dat1, "DECL LDAT LCPDAT" + Num_CPDAT.ToString() + "={VEL " + nc.VEL_CP.ToString() + ",ACC " + 
                       (ACC_CP * 100 / DEF_ACC).ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".000,APO_FAC 50.0000,AXIS_VEL " + 
                       Vel_PTP.ToString() + ".000,AXIS_ACC " + Acc_PTP.ToString() + ".000,ORI_TYP #VAR,CIRC_TYP #BASE,JERK_FAC 50.0000,GEAR_JERK 50.0000,EXAX_IGN 0}");
            SectOutput(block_dat1, "");
            //-------------SRC----------------
            SectOutput(block_src1, ";FOLD CIRC P" + Num_E6POS.ToString() + " P" + (Num_E6POS+1).ToString() + " CONT Vel=" + nc.VEL_CP.ToString() + 
                       " m/s CPDAT" + Num_CPDAT.ToString() + " Tool[" + nc.TOOL_DATA.ToString() + "]:Tool_main Base[" + nc.BASE_DATA.ToString() + 
                       "]:Base_main;%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VCIRC, %P 1:CIRC, 2:P" + Num_E6POS.ToString() + ", 3:P" + 
                       (Num_E6POS+1).ToString() + ", 4:C_DIS C_DIS, 6:" + nc.VEL_CP.ToString() + ", 8:CPDAT" + Num_CPDAT.ToString());
            SectOutput(block_src1, "$BWDSTART=FALSE");
            SectOutput(block_src1, "LDAT_ACT=LCPDAT" + Num_CPDAT.ToString());
            SectOutput(block_src1, "FDAT_ACT=FP" + (Num_E6POS+1).ToString());
            SectOutput(block_src1, "BAS(#CP_PARAMS," + nc.VEL_CP.ToString() + ")");
            SectOutput(block_src1, "CIRC XP" + Num_E6POS.ToString() + ", XP" + (Num_E6POS+1).ToString() + " C_DIS C_DIS");
            SectOutput(block_src1, ";ENDFOLD");
            SectOutput(block_src1, "");
            //--------------------------------

            Num_E6POS = Num_E6POS + 1;

            X_prev = nc.X.v; Y_prev = nc.Y.v; Z_prev = nc.Z.v; A_prev = nc.A.v; B_prev = nc.B.v; C_prev = nc.C.v;
            E1_prev = nc.E1.v; E2_prev = nc.E2.v; E3_prev = nc.E3.v; E4_prev = nc.E4.v; E5_prev = nc.E5.v; E6_prev = nc.E6.v;

            if (Num_CPDAT >= MAX_num_line)
            {
                //---.SRC----
                // ChangeNcFile Current_NAME_file + ".src"
                SectOutput(block_src1, "END");
                head_src();
                OutSection(block_src1);
                //---.DAT----
                //ChangeNcFile Current_NAME_file + ".dat"
                SectOutput(block_dat1, "ENDDAT");
                head_dat();
                OutSection(block_dat1);
            }  
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            nc.X.v = cld[1];  nc.Y.v = cld[2];  nc.Z.v = cld[3];
            nc.A.v = cld[4]; nc.B.v = cld[5]; nc.C.v = cld[6];
            Point_Position("MachineStateFlags","line", cmd);
            
            RoundXYZABC();
            Ext_Axis(cmd);

            if (Math.Abs(nc.X.v - X_prev) > 0.001 || Math.Abs(nc.Y.v - Y_prev) > 0.001 || Math.Abs(nc.Z.v - Z_prev) > 0.001 || 
                Math.Abs(nc.A.v - A_prev) > 0.001 || Math.Abs(nc.B.v - B_prev) > 0.001 || Math.Abs(nc.C.v - C_prev) > 0.001 || 
                Math.Abs(nc.E1.v - E1_prev) > 0.001 || Math.Abs(nc.E2.v - E2_prev) > 0.001 || Math.Abs(nc.E3.v - E3_prev) > 0.001 || 
                Math.Abs(nc.E4.v - E4_prev) > 0.001 || Math.Abs(nc.E5.v - E5_prev) > 0.001 || Math.Abs(nc.E6.v - E6_prev) > 0.001)
            {
                Num_E6POS = Num_E6POS + 1;
                Num_CPDAT = Num_CPDAT + 1;
                //-------------SRC----------------
                SectOutput(block_src1, ";FOLD LIN P" + Num_E6POS.ToString() + " CONT Vel=" + nc.VEL_CP.ToString() + " m/s CPDAT" + 
                           Num_CPDAT.ToString() + " Tool[" + nc.TOOL_DATA.ToString() + "]:Tool_main Base[" + nc.BASE_DATA.ToString() + 
                           "]:Base_main;%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VLIN, %P 1:LIN, 2:P" + Num_E6POS.ToString() + 
                           ", 3:C_DIS C_DIS,5:" + nc.VEL_CP.ToString() + ",7:CPDAT" + Num_CPDAT.ToString());
                SectOutput(block_src1, "$BWDSTART=FALSE");
                SectOutput(block_src1, "LDAT_ACT=LCPDAT" + Num_CPDAT.ToString() + "");
                SectOutput(block_src1, "FDAT_ACT=FP" + Num_E6POS.ToString() + "");
                SectOutput(block_src1, "BAS(#CP_PARAMS," + nc.VEL_CP.ToString() + ")");
                SectOutput(block_src1, "LIN XP" + Num_E6POS.ToString() + " C_DIS C_DIS");
                SectOutput(block_src1, ";ENDFOLD");
                SectOutput(block_src1, "");

                //------------DAT-----------------
                SectOutput(block_dat1, "DECL E6POS XP" + Num_E6POS.ToString() + "={X " + nc.X.ToString() + ",Y " + nc.Y.ToString() + 
                           ",Z " + nc.Z.ToString() + ",A " + nc.A.ToString() + ",B " + nc.B.ToString() + ",C " + nc.C.ToString() + 
                           ",S "+ State.ToString() + ",T " + Turn.ToString()+ ",E1 " + nc.E1.ToString() + ",E2 " + nc.E2.ToString() + 
                           ",E3 "+ nc.E3.ToString() +",E4 "+ nc.E4.ToString() + ",E5 " + nc.E5.ToString() +",E6 " + nc.E6.ToString() + "}");
                SectOutput(block_dat1, "DECL FDAT FP" + Num_E6POS.ToString() + "={TOOL_NO " + nc.TOOL_DATA.ToString() + ",BASE_NO " + 
                           nc.BASE_DATA.ToString() + ",IPO_FRAME #BASE,POINT2[] " + "34" + " " + "34" + ",TQ_STATE FALSE}");
                SectOutput(block_dat1, "DECL LDAT LCPDAT" + Num_CPDAT.ToString() + "={VEL " + nc.VEL_CP.ToString() + ",ACC " + 
                           (ACC_CP * 100 / DEF_ACC).ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".00000,APO_FAC 50.0000,AXIS_VEL " + 
                           Vel_PTP.ToString() + ".000,AXIS_ACC " + Acc_PTP.ToString() + 
                           ".000,ORI_TYP #VAR,CIRC_TYP #BASE,JERK_FAC 50.0000,GEAR_JERK 50.0000,EXAX_IGN 0}");
                SectOutput(block_dat1, "");
                //--------------------------------

                X_prev = nc.X.v; Y_prev = nc.Y.v; Z_prev = nc.Z.v; A_prev = nc.A.v; B_prev = nc.B.v; C_prev = nc.C.v;
                E1_prev = nc.E1.v; E2_prev = nc.E2.v; E3_prev = nc.E3.v; E4_prev = nc.E4.v; E5_prev = nc.E5.v; E6_prev = nc.E6.v;

                if (Num_CPDAT >= MAX_num_line)
                {
                    //---.SRC----
                    // ChangeNcFile Current_NAME_file + ".src"
                    SectOutput(block_src1, "END");
                    head_src();
                    OutSection(block_src1);
                    // !---.DAT----
                    // ChangeNcFile Current_NAME_file + ".dat"
                    SectOutput(block_dat1, "ENDDAT");
                    head_dat();
                    OutSection(block_dat1);
                }
            }
        }

        public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
        {
            if (cmd.Flt["CSNumber"] >= 53) nc.BASE_DATA.v = cmd.Flt["CSNumber"] - 53;
            else nc.BASE_DATA.v = cmd.Flt["CSNumber"];
            
            if (nc.BASE_DATA.v != nc.BASE_DATA.v0)
            {
                if (cld[1] == 0 && cld[2] == 0 && cld[3] == 0 && cld[6] == 0 && cld[7] == 0 && cld[8] == 0)
                {
                    SectOutput(block_src1, "$BASE = $WORLD");
                    nc.BASE_DATA.v = 0;
                    nc.BASE_DATA.v0 = nc.BASE_DATA.v;
                }
                else SectOutput(block_src1, "$BASE=BASE_DATA[" + nc.BASE_DATA.ToString() + "]");
            }
        }

        public override void OnPartNo(ICLDPartNoCommand cmd, CLDArray cld)
        {
            MaxReal = 9999999;
            SectInit();

            Vel_PTP = 20;
            Acc_PTP = 20;
            APO_CDIS = 2;
            APO_CPT = 3;
            ACC_CP = 1;
            DEF_ACC = 2.3;

            MAX_num_line = 99999999;

            
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
                           Num_PPDAT.ToString() + " Tool[" + nc.TOOL_DATA.ToString() + "]:Tool_main Base[" + nc.BASE_DATA.ToString() + 
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
                SectOutput(block_dat1, "DECL E6AXIS XP" + Num_E6POS.ToString() + "={A1 " + nc.A1.ToString() + ",A2 " + nc.A2.ToString() + 
                           ",A3 " + nc.A3.ToString() + ",A4 " + nc.A4.ToString() + ",A5 " + nc.A5.ToString() + ",A6 " + nc.A6.ToString() + 
                           ",E1 " + nc.E1.ToString() + ",E2 " + nc.E2.ToString() + ",E3 " + nc.E3.ToString() + ",E4 " + nc.E4.ToString() + 
                           ",E5 " + nc.E5.ToString() + ",E6 " + nc.E6.ToString() + "}");
                SectOutput(block_dat1, "DECL FDAT FP" + Num_E6POS.ToString() + "={TOOL_NO " + nc.TOOL_DATA.ToString() + ",BASE_NO " + 
                           nc.BASE_DATA.ToString() + ",IPO_FRAME #BASE,POINT2[] " + "34" + " " + "34" + ",TQ_STATE FALSE}");
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
            nc.VEL_CP.v = cld[1] / 1000;
            nc.VEL_CP.v = nc.VEL_CP.v / 60;
            outstr = nc.VEL_CP.ToString();
            if (outstr.Length > 0) SectOutput(block_src1, outstr+"; m/sec; " + (nc.VEL_CP.v * 60 * 1000).ToString() + " mm/min");
        }

        public void Big_small_file()
        {
            
        }

        public void SectInit()
        {
            block_src1 = 1;
            block_src2 = 2;
            block_dat1 = 3;
            block_dat2 = 4;
            for (int i = 1; i <= 4; i++){
                ResetSection(i);
            }
        }

        public void ResetSection(int SectID)
        {
            SectLen[SectID] = 0;
        }

        public void RoundXYZABC()
        {
            if (nc.X.v > -0.001 && nc.X.v < 0) nc.X.v = 0;
            if (nc.Y.v > -0.001 && nc.Y.v < 0) nc.Y.v = 0;
            if (nc.Z.v > -0.001 && nc.Z.v < 0) nc.Z.v = 0;
            if (nc.A.v > -0.001 && nc.A.v < 0) nc.A.v = 0;
            if (nc.B.v > -0.001 && nc.B.v < 0) nc.B.v = 0;
            if (nc.C.v > -0.001 && nc.C.v < 0) nc.C.v = 0;
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
                if (cmd.Ptr["Axes(AxisA1Pos)"] is not null) nc.A1.v = cmd.Flt["Axes(AxisA1Pos).Value"];
                if (cmd.Ptr["Axes(AxisA2Pos)"] is not null) nc.A2.v = cmd.Flt["Axes(AxisA2Pos).Value"];
                if (cmd.Ptr["Axes(AxisA3Pos)"] is not null) nc.A3.v = cmd.Flt["Axes(AxisA3Pos).Value"];
                if (cmd.Ptr["Axes(AxisA4Pos)"] is not null) nc.A4.v = cmd.Flt["Axes(AxisA4Pos).Value"];
                if (cmd.Ptr["Axes(AxisA5Pos)"] is not null) nc.A5.v = cmd.Flt["Axes(AxisA5Pos).Value"];
                if (cmd.Ptr["Axes(AxisA6Pos)"] is not null) nc.A6.v = cmd.Flt["Axes(AxisA6Pos).Value"];
            }
            else if (move_type == "circ1")
            {
                if (cmd.Ptr["MidPos.Axes(AxisA1Pos)"] is not null) nc.A1.v = cmd.Flt["MidPos.Axes(AxisA1Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA2Pos)"] is not null) nc.A2.v = cmd.Flt["MidPos.Axes(AxisA2Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA3Pos)"] is not null) nc.A3.v = cmd.Flt["MidPos.Axes(AxisA3Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA4Pos)"] is not null) nc.A4.v = cmd.Flt["MidPos.Axes(AxisA4Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA5Pos)"] is not null) nc.A5.v = cmd.Flt["MidPos.Axes(AxisA5Pos).Value"];
                if (cmd.Ptr["MidPos.Axes(AxisA6Pos)"] is not null) nc.A6.v = cmd.Flt["MidPos.Axes(AxisA6Pos).Value"];
            }
            else if (move_type == "circ2")
            {
                if (cmd.Ptr["EndPos.Axes(AxisA1Pos)"] is not null) nc.A1.v = cmd.Flt["EndPos.Axes(AxisA1Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA2Pos)"] is not null) nc.A2.v = cmd.Flt["EndPos.Axes(AxisA2Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA3Pos)"] is not null) nc.A3.v = cmd.Flt["EndPos.Axes(AxisA3Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA4Pos)"] is not null) nc.A4.v = cmd.Flt["EndPos.Axes(AxisA4Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA5Pos)"] is not null) nc.A5.v = cmd.Flt["EndPos.Axes(AxisA5Pos).Value"];
                if (cmd.Ptr["EndPos.Axes(AxisA6Pos)"] is not null) nc.A6.v = cmd.Flt["EndPos.Axes(AxisA6Pos).Value"];
            }

            nc.A1.v0 = nc.A1.v; nc.A2.v0 = nc.A2.v; nc.A3.v0 = nc.A3.v; nc.A4.v0 = nc.A4.v; nc.A5.v0 = nc.A5.v; nc.A6.v0 = nc.A6.v;

            Turn = 0;
            if (nc.A1.v < 0) Turn = Turn + 1;
            if (nc.A2.v < 0) Turn = Turn + 2;
            if (nc.A3.v < 0) Turn = Turn + 4;
            if (nc.A4.v < 0) Turn = Turn + 8;
            if (nc.A5.v < 0) Turn = Turn + 16;
            if (nc.A6.v < 0) Turn = Turn + 32;
        }

        public void if_first_point()
        {
            Num_E6POS = Num_E6POS + 1;
            Num_CPDAT = Num_CPDAT + 1;
            //-------------SRC----------------
            SectOutput(block_src1, ";FOLD LIN P" + Num_E6POS.ToString() + " CONT Vel=" + nc.VEL_CP.ToString() + " m/s CPDAT" + 
                       Num_CPDAT.ToString() + " Tool[" + nc.TOOL_DATA.ToString() + "]:Tool_main Base[" + 
                       nc.BASE_DATA.ToString() + "]:Base_main;%{PE}%R 8.3.40,%MKUKATPBASIS,%CMOVE,%VLIN, %P 1:LIN, 2:P" + 
                       Num_E6POS.ToString() + ", 3:C_DIS C_DIS,5:" + nc.VEL_CP.ToString() + ",7:CPDAT" + Num_CPDAT.ToString());
            SectOutput(block_src1, "$BWDSTART=FALSE");
            SectOutput(block_src1, "LDAT_ACT=LCPDAT" + Num_CPDAT.ToString() + "");
            SectOutput(block_src1, "FDAT_ACT=FP" + Num_E6POS.ToString() + "");
            SectOutput(block_src1, "BAS(#CP_PARAMS," + nc.VEL_CP.ToString() + ")");
            SectOutput(block_src1, "LIN XP" + Num_E6POS.ToString() + " C_DIS C_DIS");
            SectOutput(block_src1, ";ENDFOLD");
            SectOutput(block_src1, "");
            //------------DAT-----------------
            SectOutput(block_dat1, "DECL E6POS XP" + Num_E6POS.ToString() + "={X " + nc.X.ToString() + ",Y " + nc.Y.ToString() + 
                       ",Z " + nc.Z.ToString() + ",A " + nc.A.ToString() + ",B " + nc.B.ToString() + ",C " + nc.C.ToString() + 
                       ",S " + State.ToString() + ",T " + Turn.ToString() + ",E1 " + nc.E1.ToString() + ",E2 " + nc.E2.ToString() + 
                       ",E3 " + nc.E3.ToString() + ",E4 " + nc.E4.ToString() + ",E5 " + nc.E5.ToString() + ",E6 " + nc.E6.ToString() + "}");
            SectOutput(block_dat1, "DECL FDAT FP" + Num_E6POS.ToString() + "={TOOL_NO " + nc.TOOL_DATA.ToString() + ",BASE_NO " + 
                       nc.BASE_DATA.ToString() + ",IPO_FRAME #BASE,POINT2[] " + "34" + " " + "34" + ",TQ_STATE FALSE}");
            SectOutput(block_dat1, "DECL LDAT LCPDAT" + Num_CPDAT.ToString() + "={VEL "+ nc.VEL_CP.ToString() + ",ACC " + 
                       (ACC_CP * 100 / DEF_ACC).ToString() + ",APO_DIST " + APO_CDIS.ToString() + ".00000,APO_FAC 50.0000,AXIS_VEL " + 
                       Vel_PTP.ToString() + ".000,AXIS_ACC " + Acc_PTP.ToString() + ".000,ORI_TYP #VAR,CIRC_TYP #BASE,JERK_FAC 50.0000,GEAR_JERK 50.0000,EXAX_IGN 0}");
            SectOutput(block_dat1, "");
        }

        public void SectOutput(int SectID, string NCString)
        {
            switch (SectID)
            {
                case 1:
                    PrivateOutLineToSect(NCString, SectID, Sect1);
                    break;
                case 2:
                    PrivateOutLineToSect(NCString, SectID, Sect2);
                    break;
                case 3:
                    PrivateOutLineToSect(NCString, SectID, Sect3);
                    break;
                case 4:
                    PrivateOutLineToSect(NCString, SectID, Sect4);
                    break;
            }
        }

        public void PrivateOutLineToSect(string NCString, int SectID, string[] Sect)
        {
            int tmp = SectLen[SectID] + 1;
            SectLen[SectID] = tmp;
            Sect[tmp] = NCString;
        }

        public void head_src()
        {
            string begin_src_file = $@"DEF {Current_NAME_file} () 
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
                    
            nc.Output(begin_src_file);
        }

        public void OutSection(int SectID)
        {
            switch (SectID)
            {
                case 1:
                    PrivateOutSect(SectID, Sect1);
                    break;
                case 2:
                    PrivateOutSect(SectID, Sect2);
                    break;
                case 3:
                    PrivateOutSect(SectID, Sect3);
                    break;
                case 4:
                    PrivateOutSect(SectID, Sect4);
                    break;
            }
        }

        public void PrivateOutSect(int SectID, string[] Sect)
        {
            int i;
             for (i = 1; i <= SectLen[SectID]; i++)
            {
                nc.WriteLine(Sect[i]);
                Sect[i] = "";
            }
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

            nc.Output(begin_dat_file);
        }

        public void A1_A6(int StartCLDIndex, int AxesCount, CLDArray cld)
        {
            int axi, cli;
            for (axi = 1; axi <= AxesCount; axi++)
            {
                cli = StartCLDIndex + (axi - 1) * 2;
                if (cld[cli] == AIndex[0]) nc.A1.v = cld[cli + 1];
                if (cld[cli] == AIndex[1]) nc.A2.v = cld[cli + 1];
                if (cld[cli] == AIndex[2]) nc.A3.v = cld[cli + 1];
                if (cld[cli] == AIndex[3]) nc.A4.v = cld[cli + 1];
                if (cld[cli] == AIndex[4]) nc.A5.v = cld[cli + 1];
                if (cld[cli] == AIndex[5]) nc.A6.v = cld[cli + 1];
            }
        }
        
        public void Ext_Axis(ICLDCommand cmd)
        {
            E1_prev = nc.E1.v; E2_prev = nc.E2.v; E3_prev = nc.E3.v; E4_prev = nc.E4.v; E5_prev = nc.E5.v; E6_prev = nc.E6.v;
            
            // ===E1====
            if (cmd.Ptr["Axes(ExtAxis1Pos)"] is not null)
            {
                nc.E1.v = cmd.Flt["Axes(ExtAxis1Pos).Value"];
                nc.E1.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis1Pos)"] is not null && mid_end == 1)
            {
                nc.E1.v = cmd.Flt["MidPos.Axes(ExtAxis1Pos).Value"];
                nc.E1.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis1Pos)"] is not null && mid_end == 2)
            {
                nc.E1.v = cmd.Flt["EndPos.Axes(ExtAxis1Pos).Value"];
                nc.E1.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E2====
            if (cmd.Ptr["Axes(ExtAxis2Pos)"] is not null)
            {
                nc.E2.v = cmd.Flt["Axes(ExtAxis2Pos).Value"];
                nc.E2.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis2Pos)"] is not null && mid_end == 1)
            {
                nc.E2.v = cmd.Flt["MidPos.Axes(ExtAxis2Pos).Value"];
                nc.E2.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis2Pos)"] is not null && mid_end == 2)
            {
                nc.E2.v = cmd.Flt["EndPos.Axes(ExtAxis2Pos).Value"];
                nc.E2.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E3====
            if (cmd.Ptr["Axes(ExtAxis3Pos)"] is not null)
            {
                nc.E3.v = cmd.Flt["Axes(ExtAxis3Pos).Value"];
                nc.E3.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis3Pos)"] is not null && mid_end == 1)
            {
                nc.E3.v = cmd.Flt["MidPos.Axes(ExtAxis3Pos).Value"];
                nc.E3.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis3Pos)"] is not null && mid_end == 2)
            {
                nc.E3.v = cmd.Flt["EndPos.Axes(ExtAxis3Pos).Value"];
                nc.E3.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E4====
            if (cmd.Ptr["Axes(ExtAxis4Pos)"] is not null)
            {
                nc.E4.v = cmd.Flt["Axes(ExtAxis4Pos).Value"];
                nc.E4.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis4Pos)"] is not null && mid_end == 1)
            {
                nc.E4.v = cmd.Flt["MidPos.Axes(ExtAxis4Pos).Value"];
                nc.E4.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis4Pos)"] is not null && mid_end == 2)
            {
                nc.E4.v = cmd.Flt["EndPos.Axes(ExtAxis4Pos).Value"];
                nc.E4.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E5====
            if (cmd.Ptr["Axes(ExtAxis5Pos)"] is not null)
            {
                nc.E5.v = cmd.Flt["Axes(ExtAxis5Pos).Value"];
                nc.E5.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis5Pos)"] is not null && mid_end == 1)
            {
                nc.E5.v = cmd.Flt["MidPos.Axes(ExtAxis5Pos).Value"];
                nc.E5.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis5Pos)"] is not null && mid_end == 2)
            {
                nc.E5.v = cmd.Flt["EndPos.Axes(ExtAxis5Pos).Value"];
                nc.E5.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            // ===E6====
            if (cmd.Ptr["Axes(ExtAxis6Pos)"] is not null)
            {
                nc.E6.v = cmd.Flt["Axes(ExtAxis6Pos).Value"];
                nc.E6.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["MidPos.Axes(ExtAxis6Pos)"] is not null && mid_end == 1)
            {
                nc.E6.v = cmd.Flt["MidPos.Axes(ExtAxis6Pos).Value"];
                nc.E6.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }

            if (cmd.Ptr["EndPos.Axes(ExtAxis6Pos)"] is not null && mid_end == 2)
            {
                nc.E6.v = cmd.Flt["EndPos.Axes(ExtAxis6Pos).Value"];
                nc.E6.v0 = MaxReal;
                PTP_main_axes = PTP_main_axes + 1;
            }
        }


    }
}