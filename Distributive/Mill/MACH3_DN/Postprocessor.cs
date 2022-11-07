namespace SprutTechnology.SCPostprocessor
{

    public partial class NCFile: TTextNCFile
    {
        public string ProgName;
        // Declare variables specific to a particular file here, as shown below
        // int FileNumber;
    }

    

    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition
        // Declare here global variables that apply to the entire postprocessor.

        ///<summary>Current nc-file</summary>
        NCFile nc;

        #endregion

        double MaxReal = 99999.999;
        double XT_ = 0;
        double YT_ = 0;
        double ZT_ = 0;
        //! Constsnt cycles variables
        int CycleOn = 0;          //! 0 - cycle Off, 1 - cycle On
        int KodCycle = 0;         //! constant cycle code
        int Za = 0;               //! work depth
        int Zf = 0;               //! safe level
        int ZP_ = 0;              // ! reverse move level
        int Zl = 0;               //! depth of one drilling step
        int Zi = 0;               //! transitional value
        int Dwell = 0;            //! pause in constsnt cycles
        int Fr = 0;               //! work FEED_ 
        //! Set machene functions by default
        double cycleon = 0;
        double Feedout = 0;
        double INTERP_ = 99999.999;
        
        int D_, H_;
        int Firstap = 1;
        int Fedmod = 10;
        int Isfirstpass = 1;
        int SubIDShift = 0;//! Numbers of subroutines starts from it

        int cfi;
        int ppfj;

        const string SPPName = "MACH3_DN";
  
         public void OutToolList()
        {
            var diametr = CLDProject.Operations[0].Tool.Command.CLD[5];
            for(int i = 0; i < CLDProject.Operations.Count;i++)
            {
                var curtool =  CLDProject.Operations[i];
                if(curtool.Enabled)
                {
                    diametr = curtool.Tool.Command.CLD[5];
                }
                nc.Output($"(Tool) ({curtool.Tool.Number}) (Diametr) ({diametr}.) ({curtool.Tool.Caption}) (Operation) ({curtool.Comment}))");
                
            }
            nc.WriteLine();
        }

        public void Calcdist()
        {

        }
        public void Initialise()
        { 
            // nc.GInterp.v = MaxReal;               //! initilalise G0 rapid
            // nc.ZCycle.v = MaxReal;                //! initalise cycle depth
            // nc.ZClear.v = MaxReal;                //! initialise cycle rapid
            // nc.Cyc_retract.v = MaxReal;           //! initialise G98 retract
            // nc.Q.v = MaxReal;                     //! initialise peck amount
            // nc.Feed_.v = MaxReal;                 //! initialise feed
            // nc.X.v = MaxReal;                     //! initialise X
            // nc.Y.v = MaxReal;                     //! initialise Y
            // nc.Z.v = MaxReal;                     //! initialise Z
            
        }
        
        public override void OnStartProject(ICLDProject prj)
        {
            nc = new NCFile();
            nc.OutputFileName = Settings.Params.Str["OutFiles.NCFileName"];
            //nc.ProgName = Settings.Params.Str["OutFiles.NCProgName"];
            InputBox("Input the name of programs", ref nc.ProgName);
            if (String.IsNullOrEmpty(nc.ProgName))
                nc.ProgName = "";
            nc.Text.Show($"{nc.ProgName}");
            nc.WriteLine("%");
            nc.WriteLine("O" + nc.ProgName);
            
            nc.WriteLine();

            nc.WriteLine("( Postprocessor: " + SPPName + " )");
            nc.WriteLine("( Generated by SprutCAM )");
            nc.WriteLine("( DATE: " + CurDate() + " )");
            nc.WriteLine("( TIME: " + CurTime() + " )");

            nc.ZCycle.v0 = nc.ZCycle.v;
            nc.X.v = MaxReal; 
            nc.X.v0=nc.X.v;
            nc.Y.v = MaxReal; 
            nc.Y.v0=nc.Y.v;
            nc.Z.v = MaxReal; 
            nc.Z.v0=nc.Z.v;
            nc.AT.v = MaxReal;
            nc.AT.v0 = nc.AT.v;
            nc.BT.v = MaxReal;
            nc.BT.v0 = nc.BT.v;
            Firstap = 1;
            Fedmod = 10;
            D_ = 0;                  // ! values D, H
            H_ = 0;            
            nc.MSP.v = 5;
            nc.MSP.v0 = nc.MSP.v;       // ! spindle off
            Isfirstpass = 1;
            SubIDShift = 0;             //! Numbers of subroutines starts from it

            OutToolList();

            nc.GInterp.v = 100;
            nc.Plane.v = 17;
            nc.KorEcv.v = 40;
            nc.KorDL.v = 49;
            nc.Cycle.v  = 80;
            nc.ABS_INC.v = 90;
            nc.COORDSYS.v = 54;
            nc.SmoothMv.v = 64;
            nc.CancelScale.v = 50;
            nc.Block.Out();

            string Flip = "N";
            InputBox("Flip 4th Axis Project Say Y\\N (Case Sensative): ", ref Flip);
            if(Flip.ToUpper() == "Y")
            {
                nc.Flip.v = 1;
            }
            nc.Block.Out();
        }


        public override void OnGoto(ICLDGotoCommand cmd, CLDArray cld)
        {
            nc.GInterp.v = 1;
            nc.X.v = cmd.EP.X;
            nc.Y.v = cmd.EP.Y;
            nc.Z.v = cmd.EP.Z;
            nc.Block.Out();
        }

        public override void OnPPFun(ICLDPPFunCommand cmd, CLDArray cld)
        {
            switch(cld[1])
            {
                case 50:      // ! StartSub
                {
                    nc.Block.Out();
                    nc.WriteLine();
                    nc.WriteLine("%");
                    nc.WriteLine("O00" + Str(SubIDShift + cld[2]));
                    if (cmd.CLDataS != "")
                    {
                        nc.WriteLine("(" + cmd.CLDataS.ToUpper() + ")");
                    } 
                    nc.GInterp.v = MaxReal; 
                    nc.GInterp.v0 = nc.GInterp.v;
                    nc.Feed_.v = MaxReal; 
                    nc.Feed_.v0 = nc.Feed_.v;
                    nc.BlockN.v = 0;
                  break;
                }
                case 51:    //! EndSub
                {
                    nc.Block.Out();
                    nc.M.v = 99; 
                    nc.M.v0 = MaxReal;
                    nc.Block.Out();
                    nc.Output("%");
                    break;
                } 
                case 52:    //! CallSub
                {
                    nc.Block.Out();
                    nc.M.v = 98; 
                    nc.M.v0 = MaxReal;
                    nc.PSubNum.v = SubIDShift + cmd.CLD[2]; 
                    nc.PSubNum.v0 = MaxReal;
                    nc.Block.Out();
                    break;
                }
                  
                case 58 :
                  {
                      int unit = 0;
                      if(cld[20] == 0)
                      {
                          unit = 21;
                      }
                      else 
                      {
                          unit = 20;
                      }
                      if(unit == 21)
                      {
                          nc.Units.v = unit;
                          nc.Text.v = "(Metric)";
                          nc.TextBlock.Out();
                      }
                      else
                      {
                          nc.Units.v = unit;
                          nc.Text.v = "(Inch)";
                          nc.TextBlock.Out();
                      }
                      break;
                  }

                  case 59:  //! EndTechInfo - Finish of operation
                  {
                    cfi = -1;
                    ppfj = -1; //! CLDFile[cfi].Cmd[ppfj] = PPFun(TechInfo) of the current operation
                    if (CycleOn == 1) 
                    {
                        CycleOn = 0;
                        nc.Cycle.v = 80;
                    }
                    nc.Block.Out();
                    if (cmd.CLDFile.Index < cmd.CLDFile.CmdCount - 1)  // ! if not last operation
                    {
                        nc.Output(""); //! Spaces between operations //! case
                    }
                    break;
                  }
            }
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            nc.Block.Out();
            if(CycleOn == 1)
            {
                CycleOn = 0;
                nc.Cycle.v = 80;
            }
            nc.GoTCP.v = 998;
            nc.Block.Out();
            var Tool_ = cld[1];
            var H_ = cld[1];
            int Mspdir = 0;
            if(nc.MSP.v != 5)
            {
                Mspdir = (int)nc.MSP.v;
                nc.MSP.v = 5;
                nc.Block.Out();
            }
            nc.Tool.v = Tool_;
            nc.H.v = H_;
            nc.KorDL.v = 43;
            nc.Msm.v = 6;
            nc.Block.Out();
            if(Mspdir != 0)
            {
                nc.MSP.v = Mspdir;
            }
            nc.Block.Out();
            Initialise();
        }
        public override void OnComment(ICLDCommentCommand cmd, CLDArray cld)
        {
            nc.WriteLine("(" + cmd.CLDataS + ")");
        }

        public override void OnPlane(ICLDPlaneCommand cmd, CLDArray cld)
        {
            switch(cmd.CLD[1])
            {
                case 33 or 133:
                {
                    nc.Plane.v = 17;
                    break;
                }
                case 37 or 137:
                {
                    nc.Plane.v = 19;
                    break;
                }
                case 41 or 141:
                {
                    nc.Plane.v = 18;
                    break;
                }
            }
            if(cmd.CLD[1] > 100)
            {
                nc.Plane.v = -nc.Plane.v;
            }
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            int IsReverse;
            if(cmd.IsOn) //! On
            {
                nc.S.v = Math.Abs(cmd.RPMValue);
                if (cmd.RPMValue > 0)
                {
                    nc.MSP.v = 3;
                } 
                else nc.MSP.v = 4;        //! M3/M4  CW/CCW
                if (nc.MSP.v == (7-nc.MSP.v0))
                  IsReverse = 1;
                else
                  IsReverse = 0;
                if (cfi >= 0)
                {   
                    SprutTechnology.SCPostprocessor.ICLDCommand i = cmd;
                    while((i.CmdTypeCode) != 1079)
                    {
                        i = i.Prev;
                    }
                    if(i.CLD[58] != 0) //! PPFun.Cld[58] = Coolant tube number
                    nc.Mc.v = 8;
                }
                  
                
                nc.Block.Out();
                if ((IsReverse > 0) && (cmd.Next.CmdTypeCode != 1010))
                {
                  nc.GDwell.v = 4;
                  nc.GDwell.v0 = MaxReal;
                  nc.Pause.v = nc.Pause.v0 * 2;
                  nc.Pause.v0 = MaxReal;
                  nc.Block.Out();
                }
            } 
            
            if(cmd.IsOff)  // ! Off
            {
                nc.MSP.v = 5;
                nc.S.v = 0;
            }
            
            
            
            if(cmd.IsOrient)  //! Oriented stop
                              //!cld[2] - angle
                              //!S = 0
            {
                if (nc.S.v != nc.S.v0) 
                nc.Block.Out();
                nc.GDwell.v = 4;
                nc.GDwell.v0 = MaxReal;
                nc.Pause.v = 1;
                nc.Pause.v0 = MaxReal;
            }
                nc.Block.Out();
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            var X1 = XT_;
            var Y1 = YT_;
            var Z1 = ZT_;
            var A1 = nc.AT.v0;

            if (INTERP_ > 1)
            {
                INTERP_ = 1;
            } 
            if (cmd.Ptr["Axes(AxisXPos)"].ValueAsDouble != 0) 
            {
                nc.X.v = cmd.Flt["Axes(AxisXPos).Value"];
                XT_ = nc.X.v;
            }
            if (cmd.Ptr["Axes(AxisYPos)"].ValueAsDouble != 0)
            {
                nc.Y.v = cmd.Flt["Axes(AxisYPos).Value"];
                YT_ = nc.Y.v;
            }
            if (cmd.Ptr["Axes(AxisZPos)"].ValueAsDouble != 0)
            {      
                nc.Z.v = cmd.Flt["Axes(AxisZPos).Value"];
                ZT_ = nc.Z.v;
            } 
            if (cmd.Ptr["Axes(AxisAPos)"].ValueAsDouble != 0)
            {     
                nc.AT.v = cmd.Flt["Axes(AxisAPos).Value"];  
            } 
            if ((nc.X.v != nc.X.v0) || (nc.Y.v != nc.Y.v0) || (nc.Z.v != nc.Z.v0) || (nc.AT.v != nc.AT.v0))
            {
                var X2 = nc.X.v;
                var Y2 = nc.Y.v;
                var Z2 = nc.Z.v;
                var A2 = nc.AT.v;

                if ((INTERP_ == 1 )  && (nc.AT.v != nc.AT.v0))   //! Inverse time feed output
                {
                  Calcdist();
                }
                else 
                {
                    nc.GFeed.v = 94;
                    if (INTERP_ != 0)
                    {                  
                        nc.Feed_.v = Feedout;
                        nc.Feed_.v0 = MaxReal;
                    } 
                }
                if ((cycleon == 0) || (nc.AT.v != nc.AT.v0) || (nc.Z.v != nc.Z.v0))       //! when drilling, don't output X/Y pos
                {
                    nc.GInterp.v = INTERP_;
                    if ((CycleOn>0) && ((nc.AT.v != nc.AT.v0) || (nc.Z.v != nc.Z.v0)))   // ! Cannot G81 with A at the same time
                    {
                        nc.GInterp.v0 = MaxReal;
                        nc.Cycle.v = 80; 
                        nc.Cycle.v0 = MaxReal;
                        CycleOn = 0;
                    }
                    nc.Block.Out();            // ! output to NC block
                }
            }
            
            nc.GoTCP.v = 0;
            nc.GoTCP.v0 = nc.GoTCP.v;   //! After any move machine is not in tool change position
        
            if (cmd.Ptr["Axes(AxisBPos)"].ValueAsDouble != 0)
            {
              nc.BT.v = cmd.Flt["Axes(AxisBPos).Value"];
              if (nc.BT.v  !=  nc.BT.v0 ) 
              {
                nc.BT.v0 = nc.BT.v;
                nc.MStop.v = 1;
                nc.MStop.v0 = 0;
                nc.Block.Out();
                nc.WriteLine("(Set B-axis tilt position" + nc.BT.v + " degrees)");
              }
            } 
        }
        public override void OnFinishProject(ICLDProject prj)
        {
            nc.Write("%");
        }

        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            
        }

        public override void OnFinishTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            
        }

        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        // Uncomment line below (Ctrl + "/"), go to the end of "On" word and press Ctrl+Space to add a new CLData command handler
        // override On

    }
}
