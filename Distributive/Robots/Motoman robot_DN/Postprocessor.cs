namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile : TTextNCFile
    {
        public string ProgName;

        public override void OnInit()
        {
        }

        public void OutText(string text)
        {
            Text.Show(text);
            TextBlock.Out();
        }
    }

    public partial class Postprocessor : TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.
        double CurX, CurY, CurZ, CurRx, CurRy, CurRz, pulse_S, pulse_L, pulse_U, pulse_R, pulse_B, pulse_T, deg_L, deg_U, deg_B,
        pulse_E1, pulse_E2, pulse_E3, pulse_E4, pulse_E5, pulse_E6, num_CS, num_tool, E1_prev, sp_common, S_cur, L_cur, U_cur, R_cur, B_cur, T_cur;

        int MaxReal, conf_l, conf_m, conf_n, conf_o, conf_p, conf_q, conf_l_prev, conf_m_prev, conf_n_prev, conf_o_prev, conf_p_prev, conf_q_prev,
        num_tool_prev, num_CS_prev, num_POINT = -1, num_JOINT, num_MOV, Speed, MAX_line, first_MOV_line, NextMOV, block_1, block_2, block_J, block_EC, type_Feed,
        Joint_Line_cur, Joint_Line_prev, Rapid_percent = 30, current_num_file, Mode_prev_MOVE, WorkpieceHolder, A3_zero_in_horiz, A2_homepos, A5_homepos,
        dont_ask_again, Synch_Ext_Axis, Ext_axis_active, circ_Ext_axis, pulse_user, approach_return, first_from_J, WorkingMode = 0;

        int[] AIndex = new int[6] { 0, 1, 2, 3, 4, 5 };
        int[] SectLen = new int[4] { 0, 0, 0, 0 };

        string current_format_data, current_format_time, Current_NAME_file, Current_model, tmp_outstr, outstr, CLData;
        string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
        List<string> Sect1 = new List<string>();
        List<string> Sect2 = new List<string>();
        List<string> Sect3 = new List<string>();
        List<string> Sect4 = new List<string>();
        string[] ALL_NAME = new string[100];
        ///<summary>Current nc-file</summary>
        NCFile nc;

        #endregion

        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            nc.ProgName = Settings.Params.Str["OutFiles.NCProgName"];
            if (String.IsNullOrEmpty(nc.ProgName))
                nc.ProgName = prj.ProjectName;
        }

        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Block.Out();
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine("  Start of operation: " + op.Comment);
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            nc.WriteLine();
        }

        public override void StopOnCLData()
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }
        //Начало работы
        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            type_Feed = cmd.FeedCode;
            nc.FeedV.v = cld[0] / 60;
        }
        public override void OnFini(ICLDFiniCommand cmd, CLDArray cld)
        {
            if (WorkingMode != 1)
                EndOfFile();
        }
        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            A1_A6(11, cmd.AxesCount, cld);

            ChangeS_TPrevOnCur();

            if (cmd.Ptr["Axes(ExtAxis1Pos)"] != null)
            {
                nc.E1.v = cmd.Flt["Axes(ExtAxis1Pos).Value"];
            }
            if (cmd.Ptr["Axes(ExtAxis2Pos)"] != null)
            {
                nc.E2.v = cmd.Flt["Axes(ExtAxis2Pos).Value"];
            }
            if (cmd.Ptr["Axes(ExtAxis3Pos)"] != null)
            {
                nc.E3.v = cmd.Flt["Axes(ExtAxis3Pos).Value"];
            }
            if (cmd.Ptr["Axes(ExtAxis4Pos)"] != null)
            {
                nc.E4.v = cmd.Flt["Axes(ExtAxis4Pos).Value"];
            }
            if (cmd.Ptr["Axes(ExtAxis5Pos)"] != null)
            {
                nc.E5.v = cmd.Flt["Axes(ExtAxis5Pos).Value"];
            }
            if (cmd.Ptr["Axes(ExtAxis6Pos)"] != null)
            {
                nc.E6.v = cmd.Flt["Axes(ExtAxis6Pos).Value"];
            }

            ChangeE1_E6PrevOnCur();

            if (first_from_J == 0)
            {
                SectOutput(block_1, "///POSTYPE USER");
                SectOutput(block_1, "///RECTAN");
                pulse_user = 1;

                CurX = cld[1]; CurY = cld[2]; CurZ = cld[3];
                CurRx = cld[4]; CurRy = cld[5]; CurRz = cld[6];

                ConfigPosition(cmd, "MachineStateFlags");
                num_POINT = num_POINT + 1;

                ChangeX_RzPrevOnMax();
                nc.C_num.v0 = MaxReal;

                outstr =CreateMaskC();

                SectOutput(block_1, outstr);

                E1_E6(1, cmd);

                nc.C_num2.v0 = MaxReal;
                nc.FeedVJ.v0 = MaxReal;

                nc.C_num2.v = num_POINT;
                nc.FeedVJ.v = Rapid_percent;

                outstr = "MOVJ " + nc.C_num2 + " " + nc.FeedVJ;

                ReplaceOutstr();

                SectOutput(block_2, outstr + tmp_outstr);
                first_from_J = 1;
            }
        }
        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            nc.WriteLine(cmd.CLDataS);
        }
        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            if (WorkpieceHolder == 0)
                num_tool = cmd.Number;
            if (WorkpieceHolder == 1)
                num_CS = cmd.Number;
        }
        public override void OnMultiArc(ICLDMultiArcCommand cmd, CLDArray cld)
        {
            Joint_Line_cur = 1;
            if (Joint_Line_cur != Joint_Line_prev)
            {
                SectOutput(block_1, "///POSTYPE USER");
                SectOutput(block_1, "///RECTAN");
                pulse_user = 1;
            }
            Joint_Line_prev = Joint_Line_cur;

            if (Mode_prev_MOVE == 2)
            {
                Output_C_L("L");
                E1_E6_prev();
                Output_C_L("C");
                E1_E6_prev();
            }

            A1_A6(19 + cmd.EndP.AxesCount * 2, cmd.MidP.AxesCount, cld);
            CurX = cld[8]; CurY = cld[9]; CurZ = cld[10];
            CurRx = cld[11]; CurRx = cld[12]; CurRx = cld[13]; //В документации написано только про MPNX и т.д. Предполагаю, что MPRX то же самое 
            Output_C_L("C");
            E1_E6(2, cmd);

            A1_A6(19, cmd.EndP.AxesCount, cld);
            CurX = cld[1]; CurY = cld[2]; CurZ = cld[3];
            CurRx = cld[4]; CurRx = cld[5]; CurRx = cld[6];
            Output_C_L("C");
            E1_E6(3, cmd);

            Mode_prev_MOVE = 2;
            CheckNextMov(cmd);
            if (NextMOV == 1)
            {
                Output_C_L("L");
                E1_E6(3, cmd);
            }
            circ_Ext_axis = 0;
        }
        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            circ_Ext_axis = 0;

            Joint_Line_cur = 1;
            if (Joint_Line_cur != Joint_Line_prev)
            {
                SectOutput(block_1, "///POSTYPE USER");
                SectOutput(block_1, "///RECTAN");
                pulse_user = 1;
            }
            Joint_Line_cur = Joint_Line_prev;
            if (first_MOV_line == 0) first_MOV_line = 1;

            A1_A6(11, cld[10], cld);
            CurX = cld[1]; CurY = cld[2]; CurZ = cld[3];
            CurRx = cld[4]; CurRy = cld[5]; CurRz = cld[6];

            ConfigPosition(cmd, "MachineStateFlags");
            CheckNextMov(cmd);
            num_POINT = num_POINT + 1;
            ChangeX_RzPrevOnMax();

            outstr = CreateMaskC();
            SectOutput(block_1, outstr);

            E1_E6(1, cmd);

            nc.C_num2.v0 = MaxReal; nc.FeedV.v0 = MaxReal;

            if (WorkpieceHolder == 0)
            {
                if (NextMOV != 2)
                {
                    if (type_Feed == -1)
                    {
                        nc.C_num2.v = num_POINT;
                        nc.FeedV.v = nc.FeedVJ.v;
                        outstr = "MOVL " + nc.C_num2 + nc.FeedV;
                    }
                    else
                    {
                        nc.C_num2.v = num_POINT;
                        outstr = "MOVL " + nc.C_num2 + nc.FeedV;
                    }
                }
                else
                {
                    nc.C_num2.v = num_POINT;
                    outstr = "MOVC " + nc.C_num2 + nc.FeedV;
                    circ_Ext_axis = Ext_axis_active;
                }
            }
            else
            {
                nc.UF.v0 = MaxReal;
                if (NextMOV != 2)
                {
                    if (type_Feed == -1)
                    {
                        nc.UF.v = num_CS;
                        nc.C_num2.v = num_POINT;
                        nc.FeedV.v = nc.FeedVJ.v;
                        outstr = "EIMOVL " + nc.UF + nc.C_num2 + nc.FeedV;
                    }
                    else
                    {
                        nc.UF.v = num_CS;
                        nc.C_num2.v = num_POINT;
                        outstr = "EIMOVL " + nc.UF + nc.C_num2 + nc.FeedV;
                    }
                }
                else
                {
                    nc.UF.v = num_CS;
                    nc.C_num2.v = num_POINT;
                    outstr = "EIMOVC " + nc.UF + nc.C_num2 + nc.FeedV;
                    circ_Ext_axis = Ext_axis_active;
                }
            }

            ReplaceOutstr();

            SectOutput(block_2, outstr + tmp_outstr);
            if (num_POINT >= MAX_line && NextMOV != 2)
            { //Никогда не заходит 
                EndOfFile();
                //CreateNextFile() 
                //TitleOfProgram()
            }
            Mode_prev_MOVE = 1;
        }
        public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
        {
            if (WorkpieceHolder == 0)
            {
                if (cmd.Flt["CSNumber"] >= 53)
                {
                    num_CS = cmd.Flt["CSNumber"] - 53;
                }
                else
                {
                    num_CS = cmd.Flt["CSNumber"];
                }
            }
            if (WorkpieceHolder == 1)
            {
                if (cmd.Flt["CSNumber"] >= 53)
                {
                    num_tool = cmd.Flt["CSNumber"] - 53;
                }
                else
                {
                    num_tool = cmd.Flt["CSNumber"];
                }
            }
        }
        public override void OnPartNo(ICLDPartNoCommand cmd, CLDArray cld)
        {
            MaxReal = 9999999;
            SectInit();
            num_POINT = 1;
            current_num_file = 0;
            first_MOV_line = 0;
            conf_l_prev = MaxReal; conf_l = MaxReal;
            conf_m_prev = MaxReal; conf_m = MaxReal;
            conf_n_prev = MaxReal; conf_n = MaxReal;
            conf_o_prev = MaxReal; conf_o = MaxReal;
            conf_p_prev = MaxReal; conf_p = MaxReal;
            conf_q_prev = MaxReal; conf_q = MaxReal;

            nc.E1.v = MaxReal; nc.E1.v0 = nc.E1.v;
            nc.E2.v = MaxReal; nc.E2.v0 = nc.E2.v;
            nc.E3.v = MaxReal; nc.E3.v0 = nc.E3.v;
            nc.E4.v = MaxReal; nc.E4.v0 = nc.E4.v;
            nc.E5.v = MaxReal; nc.E5.v0 = nc.E5.v;
            nc.E6.v = MaxReal; nc.E6.v0 = nc.E6.v;
            E1_prev = MaxReal;

            pulse_E1 = 1741;
            pulse_E2 = -1233;
            pulse_E3 = 1;
            pulse_E4 = 1;
            pulse_E5 = 1;
            pulse_E6 = 1;

            DataConvert();
            TimeConvert();

            if (cmd.ProjectName.Length > 8 && WorkingMode != 0)
                nc.WriteLine("The program name can be from ONE to EIGHT characters.");
            Joint_Line_cur = 99; Joint_Line_prev = 99;

            Rapid_percent = 30;
            if (WorkingMode == 1)
                MAX_line = MaxReal;
            else MAX_line = 99999999;
            pulse_S = 2389;
            pulse_L = -2391;
            pulse_U = 2104;
            pulse_R = -1885;
            pulse_B = 1713;
            pulse_T = -1099;
            deg_L = 90; deg_U = 0; deg_B = 0;
            A3_zero_in_horiz = 0;

            Synch_Ext_Axis = 1;

            Current_NAME_file = cmd.ProjectName.ToUpper();
            //ReadMachineAxesIndexes();
            ParametersOfRobot();
        }
        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            double oldFeedVJ;

            Joint_Line_cur = 0;
            if(Joint_Line_cur != Joint_Line_prev){
                SectOutput(block_1,"///POSTYPE PULSE");
                SectOutput(block_1,"///PULSE");
                pulse_user = 0;
            }
            Joint_Line_prev=Joint_Line_cur;
            
            A1_A6(11, cld[9], cld);

            ConfigPosition(cmd, "MachineStateFlags");
            CurX = cld[0]; CurY = cld[1]; CurZ = cld[2];
            CurRx = cld[3]; CurRy = cld[4]; CurRz = cld[5];  

            num_POINT += 1;

            nc.S.v0 = MaxReal; nc.L.v0 = MaxReal;
            nc.U.v0 = MaxReal; nc.R.v0 = MaxReal;
            nc.B.v0 = MaxReal; nc.T.v0 = MaxReal;
            nc.C_num.v0 = MaxReal;

            nc.C_num.v = num_POINT;
            nc.S.v = S_cur; nc.L.v = L_cur; nc.U.v = U_cur;
            nc.R.v = R_cur; nc.B.v = B_cur; nc.T.v = T_cur;
            outstr = nc.C_num + " = " + nc.S + nc.L + nc.U + nc.R + nc.B + nc.T;
            SectOutput(block_1, outstr);
            E1_E6(1, cmd);

            nc.FeedVJ.v0 = MaxReal; nc.C_num.v0 = MaxReal; oldFeedVJ = nc.FeedVJ.v;
            nc.FeedVJ.v = Rapid_percent;
            outstr = "MOVJ " + nc.C_num2 + nc.FeedVJ;
            nc.FeedVJ.v = oldFeedVJ;

            ReplaceOutstr();
            SectOutput(block_2, outstr + tmp_outstr);

            Mode_prev_MOVE = 0;
        }
        public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
        {
            double tmp_CS;

            if(cld[0] == 58){
                if(CurrentFile != null){
                    if(cmd.Int["PPFun(TechInfo).Operation(1).MachineCoordinateSystem.Auto"] == 0){
                         tmp_CS = cmd.Flt["PPFun(TechInfo).Operation(1).MachineCoordinateSystem.CSNumber"];
                    }
                    else tmp_CS = num_tool;
                    if (tmp_CS >= 53) tmp_CS = tmp_CS - 53; 
                    if(((cld[25] != num_tool || tmp_CS != num_CS) && WorkpieceHolder == 0) ||
                        ((cld[25]) != num_CS || tmp_CS != num_tool) && WorkpieceHolder == 1){
                            conf_l_prev=99; conf_m_prev=99; conf_n_prev=99; conf_o_prev=99; conf_p_prev=99; conf_q_prev=99;
                            Joint_Line_prev = 99;
                        }
                }
                if(Pos("ExtTool", cmd.Str["PPFun(TechInfo).Tool.RevolverID"]) != 0 ||
                     Pos("TableTool", cmd.Str["PPFun(TechInfo).Tool.RevolverID"]) != 0)
                    WorkpieceHolder = 1;
            }
        }
        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {
            type_Feed = -1;
            nc.FeedV.v = cld[0]/60;
        }
        public override void OnStructure(ICLDStructureCommand cmd, CLDArray cld)
        {
            string type_str = cmd.Str["NodeType"];
            if(cld[0] == 71){
                if(type_str == "Approach" || type_str == "Return")
                    approach_return = 1;
            }
            else if(cld[0] == 72){
                if(type_str == "Approach" || type_str == "Return")
                    approach_return = 0;
            }
        }
        //Подпрограммы
        private string CreateMaskE(){
            nc.EC_num.v = num_POINT;
            return nc.EC_num + "=" + nc.E1 + nc.E2 + nc.E3 + nc.E4 + nc.E5 + nc.E6;
        }
        private string CreateMaskC(){
            nc.C_num.v = num_POINT;
            nc.X.v = CurX;
            nc.Y.v = CurY;
            nc.Z.v = CurZ;
            nc.Rx.v = CurRx;                
            nc.Ry.v = CurRy;
            nc.Rz.v = CurRz;

            return nc.C_num + "=" + nc.X + nc.Y + nc.Z + nc.Rx + nc.Ry + nc.Rz;
        }
        private void ChangeX_RzPrevOnMax(){
            nc.X.v0 = MaxReal; 
            nc.Y.v0 = MaxReal; 
            nc.Z.v0 = MaxReal;
            nc.Rx.v0 = MaxReal; 
            nc.Ry.v0 = MaxReal; 
            nc.Rz.v0 = MaxReal;
        }
        private void ChangeE1_E6PrevOnMax(){
            nc.E1.v0 = MaxReal; 
            nc.E2.v0 = MaxReal; 
            nc.E3.v0 = MaxReal;
            nc.E4.v0 = MaxReal; 
            nc.E5.v0 = MaxReal; 
            nc.E6.v0 = MaxReal;
        }
        private void ChangeE1_E6PrevOnCur(){
            nc.E1.v0 = nc.E1.v;
            nc.E2.v0 = nc.E2.v;
            nc.E3.v0 = nc.E3.v;
            nc.E4.v0 = nc.E4.v;
            nc.E5.v0 = nc.E5.v;
            nc.E6.v0 = nc.E6.v;
        }
        private void ChangeS_TPrevOnCur(){
            nc.S.v0 = nc.S.v;
            nc.L.v0 = nc.L.v;
            nc.U.v0 = nc.U.v;
            nc.R.v0 = nc.R.v;
            nc.B.v0 = nc.B.v;
            nc.T.v0 = nc.T.v;
        }
        public void ParametersOfRobot(){
            var modelOfRobot = new string[14];
            modelOfRobot[0] = "Other...";
            modelOfRobot[1] = "ES165D";
            modelOfRobot[2] = "ES200N";
            modelOfRobot[3] = "ES280N";
            modelOfRobot[4] = "UP130";
            modelOfRobot[5] = "UP50N";
            modelOfRobot[6] = "MH50 II";
            modelOfRobot[7] = "MC2000";
            modelOfRobot[8] = "HP20D";
            modelOfRobot[9] = "HP20-A00";
            modelOfRobot[10] = "MH6F-A00";
            modelOfRobot[11] = "MA1400";
            modelOfRobot[12] = "MH5F";
            modelOfRobot[13] = "MA2010";

            dont_ask_again = 1;
            Current_model = "Oter... AXIS_S2389 AXIS_L-2391 AXIS_U2104 AXIS_R-1885 AXIS_B1713 AXIS_T-1099 AXIS_L_DEG90 AXIS_U_DEG0 AXIS_B_DEG0";
            ParametersOfRobotBASE();  
        }
        public void ParametersOfRobotBASE(){
            if(Pos("Other...", Current_model) != 0 && dont_ask_again == 1 && Length(Current_model)>10){
                int tmp_num1, tmp_num2;
                tmp_num1 = Pos("AXIS_S", Current_model) + 6;
                tmp_num2 = Pos("AXIS_L", Current_model) - 1 - tmp_num1;
                pulse_S = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_L", Current_model) + 6;
                tmp_num2 = Pos("AXIS_U", Current_model) - 1 - tmp_num1;
                pulse_L = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_U", Current_model) + 6;
                tmp_num2 = Pos("AXIS_R", Current_model) - 1 - tmp_num1;
                pulse_U = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_R", Current_model) + 6;
                tmp_num2 = Pos("AXIS_B", Current_model) - 1 - tmp_num1;
                pulse_R = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_B", Current_model) + 6;
                tmp_num2 = Pos("AXIS_T", Current_model) - 1 - tmp_num1;
                pulse_B = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_T", Current_model) + 6;
                tmp_num2 = Pos("AXIS_L_DEG", Current_model) - 1 - tmp_num1;
                pulse_T = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_L_DEG", Current_model) + 10;
                tmp_num2 = Pos("AXIS_U_TEG", Current_model) - 1 - tmp_num1;
                deg_L = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_U_DEG", Current_model) + 10;
                tmp_num2 = Pos("AXIS_B_TEG", Current_model) - 1 - tmp_num1;
                deg_U = Num(Copy(Current_model, tmp_num1, tmp_num2));
                tmp_num1 = Pos("AXIS_B_DEG", Current_model) + 10;
                tmp_num2 = Length(Current_model) - tmp_num1 + 1;
                deg_B = Num(Copy(Current_model, tmp_num1, tmp_num2));
            }
        }
        public void TimeConvert()
        {
            int i1 = 0, i2 = 0;
            string str1;

            while (i1 <= CurTime().Length)
            {
                str1 = Copy(CurTime(), i1, 1);
                if (str1 == ":") i2 = i1;
                i1 += 1;
            }
            current_format_time = Copy(CurTime(), 0, i2 - 1);
        }
        public void DataConvert()
        {
            string year, month, day;

            day = Copy(CurDate(), 0, 2);
            month = Copy(CurDate(), 3, 2);
            year = Copy(CurDate(), 6, 4);

            current_format_data = year + "/" + month + "/" + day;
        }
        public void SectInit()
        {
            block_1 = 0;
            block_2 = 1;
            block_J = 2;
            block_EC = 3;
            for (int i = 0; i < 4; i++)
            {
                ResetSection(i);
            }
        }
        public void CheckNextMov(ICLDCommand cmd)
        {
            int fi, i;
            fi = CurrentFile.Index;
            i = CurrentCmd.Index + 2;
            while (cmd.CmdType==CLDCmdType.MultiGoto ||
                cmd.CmdType==CLDCmdType.MultiArc ||
                cmd.CmdType==CLDCmdType.PhysicGoto ||
                i >= CLDProject.CLDFiles[fi].CmdCount)
            {
                i += 1;
            }

            if (cmd.CmdType==CLDCmdType.PhysicGoto) NextMOV = 0;
            else if (cmd.CmdType==CLDCmdType.MultiGoto) NextMOV = 1;
            else if (cmd.CmdType==CLDCmdType.MultiArc) NextMOV = 2;
            else if (i >= CLDProject.CLDFiles[fi].CmdCount) NextMOV = 10;
        }
        public void Output_C_L(string letter){
            sp_common = nc.FeedV.v;

            num_POINT = num_POINT + 1;
            nc.C_num.v0 = MaxReal;

            ChangeX_RzPrevOnMax();

            outstr = outstr = CreateMaskC();
            SectOutput(block_1, outstr);

            nc.C_num2.v0 = MaxReal; nc.FeedV.v0 = MaxReal;
            
            
            if (WorkpieceHolder == 0)
            {
                nc.C_num2.v = num_POINT;
                nc.FeedV.v = sp_common;
                outstr = "MOV" + letter.ToUpper() + " " + nc.C_num2 + nc.FeedV;
            }
            else
            {
                nc.UF.v0 = MaxReal;
                nc.UF.v = num_CS;
                nc.C_num2.v = num_POINT;
                nc.FeedV.v = sp_common;
                outstr = "EIMOV" + letter.ToUpper() + " " + nc.UF + nc.C_num2 + nc.FeedV;
            }

            ReplaceOutstr();
            SectOutput(block_2, outstr + tmp_outstr);
            E1_E6_reset();
        }
        public void E1_E6_prev()
        {
            if (nc.E1.v != MaxReal)
            {
                ChangeE1_E6PrevOnMax();

                if (Math.Abs(nc.E1.v - E1_prev) >= 1)
                    Ext_axis_active = 1;
                else
                    Ext_axis_active = 0;

                nc.EC_num.v0 = MaxReal;
                outstr = CreateMaskE();
                SectOutput(block_EC, outstr);
                nc.WriteLine(outstr);
                E1_prev = nc.E1.v;
            }
        }
        public void E1_E6_reset()
        {
            if (nc.E1.v != MaxReal)
                nc.E1.v0 = MaxReal;
            if (nc.E2.v != MaxReal)
                nc.E2.v0 = MaxReal;
            if (nc.E3.v != MaxReal)
                nc.E3.v0 = MaxReal;
            if (nc.E4.v != MaxReal)
                nc.E4.v0 = MaxReal;
            if (nc.E5.v != MaxReal)
                nc.E5.v0 = MaxReal;
            if (nc.E6.v != MaxReal)
                nc.E6.v0 = MaxReal;
        }
        public void ReplaceOutstr()
        {
            if (nc.E1.v != MaxReal)
            {
                tmp_outstr = "  +" + outstr;
                Replace(tmp_outstr, " C", " EC");
                Replace(tmp_outstr, " V=", " VJ=");

                tmp_outstr = Copy(tmp_outstr, 0, Pos(" VJ=", tmp_outstr) - 1);

                Replace(tmp_outstr, "MOVL", "MOVJ");
                Replace(tmp_outstr, "MOVC", "MOVJ");
            }
            else
                tmp_outstr = "";

            if (approach_return == 1)
                Replace(outstr, "MOVL", "MOVJ");
            if ((Ext_axis_active == 1 || circ_Ext_axis == 1) && Synch_Ext_Axis == 1)
            {
                Replace(outstr, "MOVL", "SMOVL");
                Replace(outstr, "MOVC", "SMOVC");
            }
        }
        public void E1_E6(int i, ICLDCommand cmd)
        {
            if (cmd.Ptr["Axes(ExtAxis1Pos)"] != null ||
                (cmd.Ptr["MidPos.Axes(ExtAxis1Pos)"] != null && i == 2) ||
                (cmd.Ptr["EndPos.Axes(ExtAxis1Pos)"] != null && i == 3))
            {
                switch (i)
                {
                    case 1:
                        nc.E1.v = cmd.Flt["Axes(ExtAxis1Pos).Value"];
                        break;
                    case 2:
                        nc.E1.v = cmd.Flt["MidPos.Axes(ExtAxis1Pos).Value"];
                        break;
                    case 3:
                        nc.E1.v = cmd.Flt["EndPos.Axes(ExtAxis1Pos).Value"];
                        break;
                }
                nc.E1.v = nc.E1.v * pulse_E1;
                nc.E1.v0 = MaxReal;
            }
            if (cmd.Ptr["Axes(ExtAxis2Pos)"] != null ||
                (cmd.Ptr["MidPos.Axes(ExtAxis2Pos)"] != null && i == 2) ||
                (cmd.Ptr["EndPos.Axes(ExtAxis2Pos)"] != null && i == 3))
            {
                switch (i)
                {
                    case 1:
                        nc.E2.v = cmd.Flt["Axes(ExtAxis2Pos).Value"];
                        break;
                    case 2:
                        nc.E2.v = cmd.Flt["MidPos.Axes(ExtAxis2Pos).Value"];
                        break;
                    case 3:
                        nc.E2.v = cmd.Flt["EndPos.Axes(ExtAxis2Pos).Value"];
                        break;
                }
                nc.E2.v = nc.E2.v * pulse_E2;
                nc.E2.v0 = MaxReal;
            }
            if (cmd.Ptr["Axes(ExtAxis3Pos)"] != null ||
                (cmd.Ptr["MidPos.Axes(ExtAxis3Pos)"] != null && i == 2) ||
                (cmd.Ptr["EndPos.Axes(ExtAxis3Pos)"] != null && i == 3))
            {
                switch (i)
                {
                    case 1:
                        nc.E3.v = cmd.Flt["Axes(ExtAxis3Pos).Value"];
                        break;
                    case 2:
                        nc.E3.v = cmd.Flt["MidPos.Axes(ExtAxis3Pos).Value"];
                        break;
                    case 3:
                        nc.E3.v = cmd.Flt["EndPos.Axes(ExtAxis3Pos).Value"];
                        break;
                }
                nc.E3.v = nc.E3.v * pulse_E3;
                nc.E3.v0 = MaxReal;
            }
            if (cmd.Ptr["Axes(ExtAxis4Pos)"] != null ||
                (cmd.Ptr["MidPos.Axes(ExtAxis4Pos)"] != null && i == 2) ||
                (cmd.Ptr["EndPos.Axes(ExtAxis4Pos)"] != null && i == 3))
            {
                switch (i)
                {
                    case 1:
                        nc.E4.v = cmd.Flt["Axes(ExtAxis4Pos).Value"];
                        break;
                    case 2:
                        nc.E4.v = cmd.Flt["MidPos.Axes(ExtAxis4Pos).Value"];
                        break;
                    case 3:
                        nc.E4.v = cmd.Flt["EndPos.Axes(ExtAxis4Pos).Value"];
                        break;
                }
                nc.E4.v = nc.E4.v * pulse_E4;
                nc.E4.v0 = MaxReal;
            }
            if (cmd.Ptr["Axes(ExtAxis5Pos)"] != null ||
                (cmd.Ptr["MidPos.Axes(ExtAxis5Pos)"] != null && i == 2) ||
                (cmd.Ptr["EndPos.Axes(ExtAxis5Pos)"] != null && i == 3))
            {
                switch (i)
                {
                    case 1:
                        nc.E5.v = cmd.Flt["Axes(ExtAxis5Pos).Value"];
                        break;
                    case 2:
                        nc.E5.v = cmd.Flt["MidPos.Axes(ExtAxis5Pos).Value"];
                        break;
                    case 3:
                        nc.E5.v = cmd.Flt["EndPos.Axes(ExtAxis5Pos).Value"];
                        break;
                }
                nc.E5.v = nc.E5.v * pulse_E5;
                nc.E5.v0 = MaxReal;
            }
            if (cmd.Ptr["Axes(ExtAxis6Pos)"] != null ||
                (cmd.Ptr["MidPos.Axes(ExtAxis6Pos)"] != null && i == 2) ||
                (cmd.Ptr["EndPos.Axes(ExtAxis6Pos)"] != null && i == 3))
            {
                switch (i)
                {
                    case 1:
                        nc.E6.v = cmd.Flt["Axes(ExtAxis6Pos).Value"];
                        break;
                    case 2:
                        nc.E6.v = cmd.Flt["MidPos.Axes(ExtAxis6Pos).Value"];
                        break;
                    case 3:
                        nc.E6.v = cmd.Flt["EndPos.Axes(ExtAxis6Pos).Value"];
                        break;
                }
                nc.E6.v = nc.E6.v * pulse_E6;
                nc.E6.v0 = MaxReal;
            }

            if (nc.E1.v != MaxReal)
            {
                nc.E1.v0 = MaxReal; nc.E2.v0 = MaxReal; nc.E3.v0 = MaxReal;
                nc.E4.v0 = MaxReal; nc.E5.v0 = MaxReal; nc.E6.v0 = MaxReal;

                if (Math.Abs(nc.E1 - E1_prev) >= 1)
                    Ext_axis_active = 1;
                else
                    Ext_axis_active = 0;

                nc.EC_num.v0 = MaxReal;

                nc.EC_num.v = num_POINT;

                string outstr = nc.EC_num + "=" + nc.E1 + nc.E2 + nc.E3 + nc.E4 + nc.E5 + nc.E6;
                SectOutput(block_EC, outstr);
                E1_prev = nc.E1.v;
            }
        }
        public void ConfigPosition(ICLDCommand cmd, string prm)
        {
            int elbowFlag, frontFlag;

            if (nc.B.v >= 0)
                conf_l = 0;
            else
                conf_l = 1;

            elbowFlag = cmd.Int[prm];

            if (elbowFlag == 2 || elbowFlag == 3 || elbowFlag == 6 || elbowFlag == 7)
                conf_m = 1;
            else
                conf_m = 0;
            frontFlag = elbowFlag;
            if (frontFlag < 4)
                conf_n = 0;
            else
                conf_n = 1;

            if (nc.S.v < 180)
                conf_q = 0;
            else
                conf_q = 1;

            if (nc.R.v < 180)
                conf_o = 0;
            else
                conf_o = 1;

            if (nc.T.v < 180)
                conf_p = 0;
            else
                conf_p = 1;

            if (conf_l_prev != conf_l || conf_m_prev != conf_m || conf_n_prev != conf_n ||
                conf_o_prev != conf_o || conf_q_prev != conf_q || conf_p_prev != conf_p)
            {
                SectOutput(block_1, "///RCONF " + Str(conf_l) + "," + Str(conf_m) + "," + Str(conf_n) + ","
                + Str(conf_o) + "," + Str(conf_p) + "," + Str(conf_q) + ",0,0");
            }

            conf_l_prev = conf_l; conf_m_prev = conf_m; conf_n_prev = conf_n; conf_o_prev = conf_o; conf_p_prev = conf_p; conf_q_prev = conf_q;

        }
        public void SectOutput(int sectId, string ncString)
        {
            switch (sectId)
            {
                case 0:
                    PrivateOutLineToSect(ncString, sectId, ref Sect1);
                    break;
                case 1:
                    PrivateOutLineToSect(ncString, sectId, ref Sect2);
                    break;
                case 2:
                    PrivateOutLineToSect(ncString, sectId, ref Sect3);
                    break;
                case 3:
                    PrivateOutLineToSect(ncString, sectId, ref Sect4);
                    break;
            }
        }
        public void A1_A6(int startCldI, int axesCount, CLDArray cld)
        {
            int axi, cli;

            for (axi = 1; axi < axesCount; axi++)
            {
                cli = startCldI + (axi - 1) * 2 - 1;
                if (cld[cli] == AIndex[0])
                {
                    nc.S.v = cld[cli + 1];
                    S_cur = nc.S.v * pulse_S;
                }
                if (cld[cli] == AIndex[1])
                {
                    nc.L.v = cld[cli + 1];
                    L_cur = (nc.L.v - deg_L) * pulse_L;
                }
                if (cld[cli] == AIndex[2])
                {
                    nc.U.v = cld[cli + 1];
                    if (A3_zero_in_horiz == 1)
                    {
                        U_cur = (nc.U.v + nc.L.v - deg_U - deg_L) * pulse_U;
                    }
                    else
                    {
                        U_cur = (nc.U.v - deg_U) * pulse_U;
                    }
                }
                if (cld[cli] == AIndex[3])
                {
                    nc.R.v = cld[cli + 1];
                    R_cur = nc.R.v * pulse_R;
                }
                if (cld[cli] == AIndex[4])
                {
                    nc.B.v = cld[cli + 1];
                    B_cur = (nc.B.v - deg_B) * pulse_B;
                }
                if (cld[cli] == AIndex[5])
                {
                    nc.T.v = cld[cli + 1];
                    T_cur = nc.T.v * pulse_T;
                }
            }
        }
        public void EndOfFile()
        {
            Joint_Line_prev = 99;

            nc.WriteLine("/JOB");
            nc.WriteLine("//NAME" + Current_NAME_file + "");
            nc.WriteLine("//POS");
            nc.WriteLine("///NPOS " + Str(num_POINT + 1) + ",0," + Str(num_POINT + 1) + ",0,0,0");
            nc.WriteLine("///USER " + Str(num_CS) + "");
            nc.WriteLine("///TOOL " + Str(num_tool));

            OutSection(block_J);

            OutSection(block_1);

            if (pulse_user == 1)
            {
                nc.WriteLine("///POSTYPE PULSE");
                nc.WriteLine("///PULSE");
                pulse_user = 0;
            }

            OutSection(block_EC);

            nc.WriteLine("//INST");
            nc.WriteLine("///DATE " + current_format_data + " " + current_format_time);
            nc.WriteLine("///COMM SPRUTCAM");

            // if(WorkingMode == 1)
            //     OutputPulseKoefs();

            nc.WriteLine("///ATTR SC,RW,RJ");
            nc.WriteLine("////FRAME USER " + Str(num_CS));
            nc.WriteLine("///GROUP1 RB1");

            if (nc.E1.v != MaxReal)
                nc.WriteLine("///GROUP2 ST1");

            nc.WriteLine("NOP");
            OutSection(block_2);
            nc.WriteLine("END");

            ResetSection(block_1);
            ResetSection(block_2);
            ResetSection(block_EC);
        }
        public void OutSection(int sectId)
        {
            switch (sectId)
            {
                case 0:
                    PrivateOutSect(sectId, ref Sect1);
                    break;
                case 1:
                    PrivateOutSect(sectId, ref Sect2);
                    break;
                case 2:
                    PrivateOutSect(sectId, ref Sect3);
                    break;
                case 3:
                    PrivateOutSect(sectId, ref Sect4);
                    break;
            }
        }
        public void PrivateOutLineToSect(string ncString, int sectId, ref List<string> sect)
        {
            int temp;
            temp = SectLen[sectId];
            SectLen[sectId] = temp;
            if (sect.Count > temp)
                sect[temp] = ncString;
            else {
                while(sect.Count <= temp){
                    sect.Add("");
                }
                sect[temp] = ncString;
            }
        }
        public void PrivateOutSect(int sectId, ref List<string> sect)
        {
            for (int i = 0; i < SectLen[sectId]; i++)
            {
                if (sect.Count-1 >= i)
                    nc.WriteLine(sect[i]);
                else
                {
                    while (sect.Count <= i)
                    {
                        sect.Add("");
                        nc.WriteLine(sect[i]);
                    }
                }
            }
        }   
        public void OutputPulseKoefs()
        {
            string msg_com;

            msg_com = "///COMM PULSE_KOEFS:";
            nc.WriteLine(msg_com + " S=" + Str(pulse_S) + " L=" + Str(pulse_L) + " U=" + Str(pulse_U));
            nc.WriteLine(msg_com + " R=" + Str(pulse_R) + " B=" + Str(pulse_B) + " T=" + Str(pulse_T));

            nc.WriteLine(msg_com + " E1=" + Str(pulse_E1) + " E2=" + Str(pulse_E2) + " E3=" + Str(pulse_E3));
            nc.WriteLine(msg_com + " E4=" + Str(pulse_E4) + " E5=" + Str(pulse_E5) + " E6=" + Str(pulse_E6));

            msg_com = "///COMM DEG_KOEFS:";
            nc.WriteLine(msg_com + " L=" + Str(deg_L) + " U=" + Str(deg_U) + " B=" + Str(deg_B));
        }
        public void ResetSection(int sectID)
        {
            SectLen[sectID] = 0;
        }
    }
}
