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
        double CycleOn = 0;          //! 0 - cycle Off, 1 - cycle On
        double KodCycle = 0;         //! constant cycle code
        double Za = 0;               //! work depth
        double Zf = 0;               //! safe level
        double ZP_ = 0;              // ! reverse move level
        double Zl = 0;               //! depth of one drilling step
        double Zi = 0;               //! transitional value
        double Dwell = 0;            //! pause in constsnt cycles
        double Fr = 0;               //! work FEED_ 
        //! Set machene functions by default
        double cycleon = 1;
        double Feedout = 0;
        double INTERP_ = 99999.999;
        double interp_ = 99999.999;
        string StructNodeName = "";
        
        int Firstap = 1;
        int Fedmod = 10;
        int Isfirstpass = 1;
        int SubIDShift = 0;//! Numbers of subroutines starts from it

        
        double Interp_;
        double CYCLEON;
        double KODCYCLE;
        double ZA;
        double ZF;
        double ZL;
        double ZI;
        double DWELL;
        double FR;
        double D_;
        double H_;
        int IsFirstpass;
        double X1;
        double Y1;
        double Z1;
        double A1;
        double X2;
        double Y2;
        double Z2;
        double A2;
        double FedMod;
        string OperationType;        
        int cfi;     //! CLDFile.CurrentFile
        int ppfj;   // ! CLDFile[cfi].Cmd[ppfj] = PPFun(TechInfo) of the current operation
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

        public void Tapper(double BottomLev,double ReturnLev,double WorkFeed,double ReturnFeed,double BottomDwell, double TopDwell)
        {
            nc.Block.Out();
            if (StructNodeName!="")
            {
              nc.BlockN.v = nc.BlockN.v + nc.BlockN.AutoIncrementStep;
              nc.Output("N" + nc.BlockN.v + " (" + StructNodeName + ")");
            } 
            nc.MSP.v0 = 99999.999;                          // ! Spindle forward
            nc.S.v0 = 99999.999;
            nc.Block.Out();
            nc.GInterp.v = 1; nc.GInterp.v0 = 99999.999;
            nc.Z.v = BottomLev; nc.Z.v0 = 99999.999;             // ! Feed to depth
            nc.Feed_.v = WorkFeed; nc.Feed_.v0 = 99999.999;     // ! Reduce feedrate
            nc.Block.Out();
            nc.MSP.v = 7-nc.MSP.v;                             // ! Spindle reverse
            nc.Block.Out();
            nc.GDwell.v = 4; nc.GDwell.v0 = 99999.999;            //! Dwell for spindle
            nc.Pause.v = BottomDwell; nc.Pause.v0 = 99999.999;
            nc.Block.Out();
            nc.GInterp.v = 1; nc.GInterp.v0 = 99999.999;
            nc.Z.v = ReturnLev; nc.Z.v0= 99999.999;             // ! Feed back to safe level
            nc.Feed_.v = ReturnFeed; nc.Feed_.v0 = 99999.999;    // ! Increase feedrate
            nc.Block.Out();
            nc.MSP.v = 7-nc.MSP.v;                             // ! Spindle forward
            nc.Block.Out();
            nc.GDwell.v = 4; nc.GDwell.v0 = 99999.999;           // ! Dwell for spindle
            nc.Pause.v = TopDwell; nc.Pause.v0 = 99999.999;
            nc.Block.Out();
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
          if (INTERP_>0)
          {
            nc.GFeed.v = 94;
            nc.Feed_.v = Feedout;
            if (nc.GFeed.v != nc.GFeed.v0)
              nc.Feed_.v0 = MaxReal;
          } 
          if (INTERP_ > 1)
          INTERP_ = 1;    //! G1
          nc.X.v = cld[1];                      // ! X,Y,Z in absolutes
          nc.Y.v = cld[2];
          nc.Z.v = cld[3];
          if ((nc.X.v != nc.X.v0) || (nc.Y.v0 != nc.Y.v0) || (nc.Z.v != nc.Z.v0))
          {
            if ((cycleon == 0) || (nc.AT.v != nc.AT.v0) || (nc.Z.v != nc.Z.v0))  //! when drilling, don't output X/Y pos
            {
                nc.GInterp.v = INTERP_;
              if ((CycleOn > 0) && ((nc.AT.v != nc.AT.v0) || (nc.Z.v0 != nc.Z.v0)))  // ! Cannot G81 with A at the same time
              {
                nc.GInterp.v0 = MaxReal;
                nc.Cycle.v = 80; 
                nc.Cycle.v0 = MaxReal;
                CycleOn = 0;
              }
              nc.Block.Out();               // ! output to NC block
            
            }
              
          } 
          XT_ = nc.X.v;  YT_ = nc.Y.v;  ZT_ = nc.Z.v;   //! save current coordinates
          nc.GoTCP.v = 0; nc.GoTCP.v0 = nc.GoTCP.v; //! After any move machine is not in tool change position
            // nc.GInterp.v = 1;
            // nc.X.v = cmd.EP.X;
            // nc.Y.v = cmd.EP.Y;
            // nc.Z.v = cmd.EP.Z;
            // nc.Block.Out();
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
            if (cmd.Axis.X != null) 
            {
                nc.X.v = cmd.Flt["Axes(AxisXPos).Value"];
                XT_ = nc.X.v;
            }
            if (cmd.Axis.Y != null)
            {
                nc.Y.v = cmd.Flt["Axes(AxisYPos).Value"];
                YT_ = nc.Y.v;
            }
            if (cmd.Axis.Z != null)
            {      
                nc.Z.v = cmd.Flt["Axes(AxisZPos).Value"];
                ZT_ = nc.Z.v;
            } 
            if (cmd.Axis.A != null)
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
                    // ! (X1 Y1 Y1 A1) - start position
                    // ! (X2 Y2 Z2 A2) - final position

                    double dx;
                    double dy; 
                    double dz; 
                    double da; 
                    double R;            
                    double stp;
                    double ti; 
                    double xn; 
                    double yn; 
                    double zn; 
                    double an; 
                    double px; 
                    double py; 
                    double pz; 
                    double pa; 
                    double Dist;
                    
                    var time = cmd.Time;
                    Dist = 0;
                    dx = X2-X1;
                    dy = Y2-Y1;
                    dz = Z2-Z1;
                    da = A2-A1;
                    if (da == 0)    //!3d case
                    {
                      Dist = Math.Pow(dx*dx+dy*dy+dz*dz,2);
                    } 
                    else
                    if ((dy == 0) && (dz == 0))    // !simple helic
                    {
                        R = Math.Pow(Y1*Y1+Z1*Z1,2);  //!radius of helic
                        R = R* da*3.141592653/180 ;  //!Length of arc
                        Dist = Math.Pow(dx*dx+R*R,2);   //! Length of helic
                    }
                    else    //!public case. Divide curve with 1 deg step
                    {
                        Dist = 0;
                        an = A1;
                        ti = 0;
                        xn = X2*ti + X1*(1-ti);
                        yn = Y2*ti + Y1*(1-ti);
                        zn = Z2*ti + Z1*(1-ti);
                        R = Math.Pow(yn*yn+zn*zn,2);  //!radius of current point
                        yn = R*Math.Cos(an);
                        zn = R*Math.Sin(an);
                        do  //! save previous position
                        {
                            px = xn;
                            py = yn;
                            pz = zn;        //! calculating new point in 1 deg. You can change the step if you need the better tolerance
                            if (A2>A1)
                            {
                                an = an + 1;
                            } 
                            else
                              an = an -1;
                            ti = (an-A1)/(A2-A1);
                            if (ti>1)
                            {
                              ti = 1;
                              an = A2;
                            } 
                            xn = X2*ti + X1*(1-ti);
                            yn = Y2*ti + Y1*(1-ti);
                            zn = Z2*ti + Z1*(1-ti);
                            R = Math.Pow(yn*yn+zn*zn,2);  //!radius of current point
                            yn = R*Math.Cos(an);
                            zn = R*Math.Sin(an);
                            dx = xn-px;
                            dy = yn-py;
                            dz = zn-pz;
                            Dist = Dist + Math.Pow(dx*dx+dy*dy+dz*dz,2);
                        }while(ti>=1);
                    }
                    nc.GFeed.v = 93; //!print dist
                    time = Dist / Feedout;
                    if (Abs(time)<0.00001)
                    {
                        nc.InvFeed.v = 60000;
                    }
                    else
                    {
                        nc.InvFeed.v = 1 / (time);
                    }
                    nc.InvFeed.v0 = MaxReal; //!Print InvFeed
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
                nc.GoTCP.v = 0;
                nc.GoTCP.v0 = nc.GoTCP.v;    //! After any move machine is not in tool change position
            }
              
        
            if (cmd.Axis.B != null)
            {
              nc.BT.v = cmd.Flt["Axes(AxisBPos).Value"];
              if (nc.BT.v  !=  nc.BT.v0 ) 
              {
                nc.BT.v0 = nc.BT.v;
                nc.MStop.v = 1;
                nc.MStop.v0 = 0;
                nc.Block.Out();
                nc.WriteLine("(Set B-axis tilt position" + (int)nc.BT.v + " degrees)");
              }
            } 
        
        }

        public override void OnStructure(ICLDStructureCommand cmd, CLDArray cld)
        {
            var NodeType = "";
            if (!cmd.IsClose)   // ! Open group
            {
                NodeType = cmd.Str["NodeType"];
                StructNodeName = cmd.Str["Comment"];
                if ((StructNodeName != "") &&
                 (NodeType == "Block") &&
                 ((cmd.Caption).ToUpper() == ("HoleMachiningOp").ToUpper()) &&
                 (cfi>=0))
                 {
                    SprutTechnology.SCPostprocessor.ICLDCommand i = cmd;
                    while((i.CmdTypeCode) != 1079)
                    {
                        i = i.Prev;
                    }
                    if(i.Int["PPFun(TechInfo).Operation(1).NCCodeFormat"] == 0)    // ! Expanded cycle format
                    {
                        nc.BlockN.v = nc.BlockN.v + nc.BlockN.AutoIncrementStep;
                        nc.Output("N" + Str(nc.BlockN.v) + " (" + StructNodeName + ")");
                    }
                 }
            }
            else                           //! Close group
            {
                StructNodeName = "";
            } 
        }

        public override void OnRapid(ICLDRapidCommand cmd, CLDArray cld)
        {
            if (INTERP_ > 0) 
            {
                INTERP_ = 0;    //! G0
            }
        }

        public override void OnCoolant(ICLDCoolantCommand cmd, CLDArray cld)
        {
            if ((nc.MSP.v != nc.MSP.v) || (nc.M.v != nc.M.v0)) 
                nc.Block.Out();
            if (cld[1] == 71) 
                nc.Mc.v = 8;
            else nc.Mc.v = 9;     //! On/Off coolant
        }

        public override void OnFeedrate(ICLDFeedrateCommand cmd, CLDArray cld)
        {
            Feedout = cld[1];               // ! filling FEED_ rgister
            if (cld[3]==316) 
              Feedout = nc.S.v/Feedout;
            if (INTERP_ == 0) 
                INTERP_ = 1;   //! G1
        }

        public override void OnCircle(ICLDCircleCommand cmd, CLDArray cld)
        {
            if (cld[4] > 0) 
                INTERP_ = 3;
            else INTERP_ = 2;   //! G3/G2
            nc.XC_.v = cld[1] - XT_ + 10/1000000;                         //  ! I,J,K in increments always
            nc.YC_.v = cld[2] - YT_ + 10/1000000;
            nc.ZC_.v = cld[3] - ZT_ + 10/1000000;
            nc.X.v = cld[5];                                 // ! X,Y,Z in absolutes
            nc.Y.v = cld[6];
            nc.Z.v = cld[7];
            nc.XC_.v0 = MaxReal; 
            nc.YC_.v0 = MaxReal; 
            nc.ZC_.v0 = MaxReal;
            nc.X.v0 = MaxReal; 
            nc.Y.v0 = MaxReal;  
            nc.Z.v0 = MaxReal;
            switch(nc.Plane.v)
            {
                case 17:
                {
                    nc.ZC_.v0 = nc.ZC_.v; 
                    nc.Z.v0 = nc.Z.v;   //!This line changed 17Jan07 by TM for helical path fix via Dave Pearson
                    break;
                }
                case 18:
                {
                    nc.YC_.v0 = nc.YC_.v; 
                    nc.Y.v0 = nc.Y.v;   //!This line changed 17Jan07 by TM for helical path fix via Dave Pearson
                    break;
                }
                case 19:
                {
                    nc.XC_.v0 = nc.XC_.v; 
                    nc.X.v0 = nc.X.v;   //!This line changed 17Jan07 by TM for helical path fix via Dave Pearson
                    break;
                }
            }
            if (interp_ > 1) 
                nc.Feed_.v = Feedout;
            nc.GInterp.v = INTERP_;
            nc.Block.Out();                                  //! output to NC block
            XT_ = cld[5];                                    //! save current coordinates
            YT_ = cld[6];
            ZT_ = cld[7];
            nc.GoTCP.v = 0; nc.GoTCP.v0 = nc.GoTCP.v; //! After any move machine is not in tool change position
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
              if (cmd.Axis.X != null)
              {
                nc.X.v = cmd.Flt["Axes(AxisXPos).Value"];
                XT_ = nc.X.v;
              } 
              if (cmd.Axis.Y != null) 
              {
                nc.Y.v = cmd.Flt["Axes(AxisYPos).Value"];
                YT_ = nc.Y.v;
              }
                
              if (cmd.Axis.Z != null)
              {
                nc.Z.v = cmd.Flt["Axes(AxisZPos).Value"];
                ZT_ = nc.Z.v;
              } 
              if (cmd.Axis.A != null)
              {   
                nc.AT.v = cmd.Flt["Axes(AxisAPos).Value"];
              } 
              if ((nc.X.v!=nc.X.v0) || (nc.Y.v!=nc.Y.v0) || (nc.Z.v!=nc.Z.v0) || (nc.AT.v!=nc.AT.v0))
              {
                nc.GInterp.v = 53; 
                nc.GInterp.v0 = MaxReal;  // ! G53
                nc.Block.Out();   // ! output to NC block
              } 

              if (cmd.Axis.B != null) 
              {
                nc.BT.v = cmd.Flt["Axes(AxisBPos).Value"];
                if (nc.BT.v != nc.BT.v0)
                {
                  nc.BT.v0 = nc.BT.v;
                  nc.MStop.v = 1;
                  nc.MStop.v0 = 0;
                  nc.Block.Out();
                  nc.Output("(Set B-axis tilt position" + nc.BT.v + " degrees)");
                } 
              }
        }

        // public override void OnAbsMov(ICLDPhysicGotoCommand cmd, CLDArray cld)
        // {
        //       if (INTERP_>0)
        //       {
        //         nc.GFeed.v = 94;
        //         nc.Feed_.v = Feedout;
        //         if (nc.GFeed.v!=nc.GFeed.v0)
        //         {
        //             nc.Feed_.v0=MaxReal;
        //         }
        //       } 
        //       if (INTERP_ > 1)    //! G1
        //       {
        //          INTERP_ = 1; 
        //       }
        //       nc.X.v = cld[1];                        // ! X,Y,Z in absolutes
        //       nc.Y.v = cld[2];
        //       nc.Z.v = cld[3];
        //       if ((nc.X.v != nc.X.v0) || (nc.Y.v!=nc.Y.v0) || (nc.Z.v!=nc.Z.v0))
        //       {
        //         if ((cycleon==0) && (nc.AT.v!=nc.AT.v0) && (nc.Z.v!=nc.Z.v0))     // ! when drilling, don't output X/Y pos
        //         {
        //           nc.GInterp.v = INTERP_;
        //           if ((CycleOn>0) && ((nc.AT.v!=nc.AT.v0) || (nc.Z.v!=nc.Z.v0)))    // ! Cannot G81 with A at the same time
        //           {
        //             nc.GInterp.v0 = MaxReal;
        //             nc.Cycle.v = 80; 
        //             nc.Cycle.v0 = MaxReal;
        //             CycleOn = 0;
        //           }
        //           nc.Block.Out();               // ! output to NC block
        //         } 
        //       } 
        //       XT_ = nc.X.v;  YT_ = nc.Y.v;  ZT_ = nc.Z.v; //! save current coordinates
        //       nc.GoTCP.v = 0; nc.GoTCP.v0 = nc.GoTCP.v; //! After any move machine is not in tool change position
        // }

        public override void OnCutCom(ICLDCutComCommand cmd, CLDArray cld)
        {
              if (cld[2]==9)    //! LENGTH
              {                   
                if (cld[1] ==71) 
                    H_=cld[3] ;
                else H_= 0;
              }
              
              else if (cld[2]==23)         //   ! RADIUS
              {
                if (cld[1]==72) 
                    nc.KorEcv.v=40;
                else
                {                
                  if (cld[10]==24) 
                    nc.KorEcv.v=42; 
                  else nc.KorEcv.v=41;
                  nc.D.v=cld[3];
                } 
              }
        }

        public override void OnCycle(ICLDCycleCommand cmd, CLDArray cld)
        {
            if (cld[1]==72)  //! cycle Off
            {
                CycleOn = 0;
                nc.Cycle.v = 80;
                nc.Block.Out();  // ! modified 1Oct07 per Dave Pearson to fix G80/G0 issue - ; OutBlock added
                INTERP_ = 0;
                nc.GInterp.v = 1; 
                nc.GInterp.v0 = nc.GInterp.v;
            }
            else    // ! cycle call
            {
                KodCycle = cld[1];
                if (KodCycle == 168)
                {  
                    if ((OperationType).ToUpper() == ("HoleMachiningOp").ToUpper())
                    {  
                      Fr = nc.S.v/cld[12]; //! For new HoleMachining operation take feed from thread step
                    } 
                    else    //! For old HoleMachining operation take feed from WorkFeed as it was before
                    {
                        if (cld[3]==316)
                            Fr=nc.S.v*cld[4]; 
                        else Fr=cld[4];
                    }
                    Tapper(cld[5]-cld[2], cld[8], Fr, cld[15], cld[10], 2*cld[10]);
                } 
                else
                {
                  nc.Cyc_retract.v = 98;
                  CycleOn = 1;                     //! cycle On
                  Za = cld[2];                     // ! work depth
                  if (cld[4] > 0)          //! creating FEED_ value
                  {
                    if (cld[3] == 316) 
                        Fr=cld[4]*nc.S.v;
                    else Fr=cld[4];
                  
                  }
                  Zf = cld[5];                     // ! safe level
                  ZP_ = cld[8];                    //  ! reverse move level
                  Dwell = cld[10];                  //! dwell time
                  if (cld[1] == 153 || cld[1]==288) // ! DEEP,BRKCHP
                  {
                    Zl = cld[6];                   // ! depth of one drilling step
                    Zi = cld[7];                   // ! transitional value
                    ZP_ = cld[8];                  // ! reverse move level
                  }
                  nc.ZCycle.v = Zf-Za; 
                  nc.ZClear.v = Zf;
                  nc.Pause.v = Dwell;
                  Dwell=0;
                  if (Zl != 0) 
                    nc.Q.v = Zl; 
                  Zl=0;
                  nc.Feed_.v = Fr;
                  switch(KodCycle)
                  {
                    case 163:
                    {
                        nc.Cycle.v = 81;  // ! DRILL
                        break;
                    }
                    case 81:
                    {
                        nc.Cycle.v = 82;  // ! FACE
                        break;
                    }
                    case 168:
                    {
                        nc.Cycle.v = 84;  // ! TAP
                        break;
                    }
                    case 209:
                    {
                        nc.Cycle.v = 85;  // ! BORE5
                        break;
                    }
                    case 210:
                    {
                        nc.Cycle.v = 86;  // ! BORE6
                        break;
                    }
                    case 211:
                    {
                        nc.Cycle.v = 87;   //! BORE7
                        break;
                    }
                    case 212:
                    {
                        nc.Cycle.v = 88;   //! BORE8
                        break;
                    }
                    case 213:
                    {
                        nc.Cycle.v = 89;   //! BORE9
                        break;
                    }
                    case 153:
                    {
                        nc.Cycle.v = 83;   //! DEEP
                        break;
                    }
                    case 288:
                    {
                        nc.Cycle.v = 73;   //! BRKCHP
                        break;
                    }
                  }
                  nc.Block.Out();
                } 
            }
        }

        public override void OnDelay(ICLDDelayCommand cmd, CLDArray cld)
        {
            nc.Block.Out();                  //  ! load old values to block
            nc.GDwell.v = 4; nc.GDwell.v0 = MaxReal; //! value G4
            nc.Pause.v = cld[1];                 // ! dwell value
            nc.Pause.v0 = MaxReal;               // ! dwell output be always
            nc.Block.Out();                  //! output NC block
        }

        public override void OnExtCycle(ICLDExtCycleCommand cmd, CLDArray cld)
        {
            if (cld[1]==72) //! cycle Off;
            {
                CycleOn = 0;
                nc.Cycle.v = 80;
                nc.Block.Out();
                INTERP_ = 0;
                nc.GInterp.v = 1; 
                nc.GInterp.v0 = nc.GInterp.v;
            } 
            else
            if (cld[1]==52)  //     ! do cycle
            {
                if ((cld[2]>=473) && (cld[2]<=489) && (cld[2]!=484)) //! Mill cycles
                {
                    //! All drill cycles have same ZCycle ZClear Feed_
                    if (cld[10] > 0)    //! creating FEED_ value
                    {       
                        if (cld[9] == 0)
                            Fr = cld[10]*nc.S.v;
                        else Fr = cld[10];
                        nc.Feed_.v= Fr;
                        nc.ZCycle.v = nc.Z.v - cld[8];
                        nc.ZClear.v = nc.Z.v - cld[6];
                        nc.Cyc_retract.v = 98;
                        CycleOn = 1;
                    } 
                }
                switch(cld[2])
                {
                    case 481:
                    {
                        if (nc.Cycle.v!=81)
                        {
                            nc.Feed_.v0=MaxReal;
                            nc.ZCycle.v0 = MaxReal;
                            nc.ZClear.v0 = MaxReal;
                        } 
                      nc.Cycle.v = 81;
                      nc.Block.Out();
                        break;
                    }
                    case 482: 
                    {
                        if (nc.Cycle.v!=82)
                        { 
                          nc.Feed_.v0=MaxReal;
                          nc.ZCycle.v0 = MaxReal;
                          nc.ZClear.v0 = MaxReal;
                          nc.Pause.v0 = MaxReal;
                        } 
                        nc.Pause.v = cld[15];
                        nc.Cycle.v = 82;
                        nc.Block.Out();
                        break;
                    }
                    case 483:
                    {
                        if (nc.Cycle.v!=83)
                        {
                            nc.Feed_.v0=MaxReal;
                            nc.ZCycle.v0 = MaxReal;
                            nc.ZClear.v0 = MaxReal;
                            //!Pause@ = MaxReal;
                            nc.QStep.v0 = MaxReal;
                        } 
                        nc.QStep.v = cld[17];
                        //!Pause = cld[15];
                        nc.Cycle.v = 83;
                        nc.Block.Out();
                        break;
                    } 
                    case 484:
                    {
                        if (cld[13]==0)
                            Fr = nc.S.v*cld[14]; 
                        else Fr = cld[14];
                        Tapper(nc.Z.v-cld[8], nc.Z.v-cld[6], nc.S.v*cld[17], Fr, cld[15], cld[16]);
                        break;
                    } 
                    case 485:
                    {
                        if(nc.Cycle.v!=85)
                        {
                            nc.Feed_.v0=MaxReal;
                            nc.ZCycle.v0 = MaxReal;
                            nc.ZClear.v0 = MaxReal;
                        } 
                          nc.Cycle.v = 85;
                          nc.Block.Out();
                        break;
                    } 
                    case 486:
                    {
                        if (nc.Cycle.v!=86)
                        {
                          nc.Feed_.v0=MaxReal;
                          nc.ZCycle.v0 = MaxReal;
                          nc.ZClear.v0 = MaxReal;
                        } 
                        nc.Pause.v = cld[15];
                        if (nc.Pause.v==0)
                            nc.Pause.v0 = 0;
                        nc.Cycle.v = 86;
                        nc.Block.Out();
                        break;
                    } 
                    case 487:
                    {
                        if (nc.Cycle.v!=87)
                        {
                          nc.Feed_.v0=MaxReal;
                          nc.ZCycle.v0 = MaxReal;
                          nc.ZClear.v0 = MaxReal;
                        } 
                        nc.Pause.v = cld[15];
                        if (nc.Pause.v==0)
                        {
                            nc.Pause.v0 = 0;
                            nc.Cycle.v = 87;
                            if (cld[17]>0)  //!call Swap(ZCycle, ZClear)
                            {
                                nc.XC_.v=cld[19]; nc.XC_.v0=0;
                                nc.YC_.v=cld[20]; nc.YC_.v0=0;
                                nc.ZC_.v=cld[21]; nc.ZC_.v0=0;
                            }
                        }
                        nc.Block.Out();
                        break; 
                    } 
                    case 488:
                    {
                        if (nc.Cycle.v!=88)
                        {
                          nc.Feed_.v0=MaxReal;
                          nc.ZCycle.v0 = MaxReal;
                          nc.ZClear.v0 = MaxReal;
                          nc.Pause.v0 = MaxReal;
                        } 
                        nc.Pause.v = cld[15];
                        nc.Cycle.v = 88;
                        nc.Block.Out();
                        break;
                    } 
                    case 489:
                    {
                        if (nc.Cycle!=89)
                        {
                            nc.Feed_.v0=MaxReal;
                            nc.ZCycle.v0 = MaxReal;
                            nc.ZClear.v0 = MaxReal;
                            nc.Pause.v0 = MaxReal;
                        } 
                        nc.Pause.v = cld[15];
                        nc.Cycle.v = 89;
                        nc.Block.Out();
                        break;
                    } 
                    case 473:
                    {
                        if (nc.Cycle.v!=73)
                        {
                            nc.Feed_.v0=MaxReal;
                            nc.ZCycle.v0 = MaxReal;
                            nc.ZClear.v0 = MaxReal;
                            nc.QStep.v0 = MaxReal;
                        } 
                        nc.QStep.v =cld[17];
                        nc.Cycle.v = 73;
                        nc.Block.Out();
                        break;
                    }
                    default:
                    {      
                        nc.Output("(MSG, Cycle doesn't supported)");
                        break;
                    }
            
                }
            
            }
          
        
        }
        
        public override void OnFinishProject(ICLDProject prj)
        {
            nc.GoTCP.v =  998; //!GoTCP@ = 0
            nc.Block.Out();                       // ! output go to Tool Ch Pos
            nc.M.v = 30;                            // ! M30 Rewind programm
            nc.Block.Out();                       //! output
            nc.Output("%");
            nc.WriteLine();
        }

        public override void OnGoHome(ICLDGoHomeCommand cmd, CLDArray cld)
        {
            if (CycleOn==1) 
            {  
              CycleOn=0;
              nc.Cycle.v=80;
              nc.Block.Out();                 //! Output in block
            }
            if (nc.GoTCP.v0!=998)
            {
                nc.Block.Out();
                nc.GoTCP.v = 998; //! M998 go to tool change point
                nc.Block.Out();
            } 
        }
        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            nc.Output(cmd.CLDataS);
        }
        public override void OnInterpolation(ICLDInterpolationCommand cmd, CLDArray cld)
        {
            nc.Output("(MSG, Interpolation doesn't supported)");
        }
        public override void OnOpStop(ICLDOpStopCommand cmd, CLDArray cld)
        {
            if (nc.M.v != nc.M.v0)
                nc.Block.Out();
            nc.M.v = 1;      //! M01
            nc.M.v = 0;
        }

        public override void OnOrigin(ICLDOriginCommand cmd, CLDArray cld)
        {
            void SwitchOffLocalCS()
            {
                if (nc.GLCS.v ==52) //! Switch off G52
                {
                    nc.GLCS.v0 = MaxReal;
                    nc.X.v = 0; nc.X.v0 = MaxReal;
                    nc.Y.v = 0; nc.Y.v0 = MaxReal;
                    nc.Z.v = 0; nc.Z.v0 = MaxReal;
                    nc.Block.Out();
                    nc.X.v = MaxReal; nc.X.v0 = nc.X.v;
                    nc.Y.v = MaxReal; nc.Y.v0 = nc.Y.v;
                    nc.Z.v = MaxReal; nc.Z.v0 = nc.Z.v;
                    nc.GLCS.v = 0;  nc.GLCS.v0 = nc.GLCS.v;
                }
           }
    

            if (cmd.OriginType==0)
            {
              SwitchOffLocalCS();
              if (cmd.Flt["CSNumber"] != 0)
              {
                    nc.COORDSYS.v = cmd.Flt["CSNumber"]; //! Work offset number G54-G59
                    if (nc.COORDSYS.v0!= nc.COORDSYS.v)
                        nc.Block.Out();
              } 
            } 
            else    //! Switch on G52
            {
              nc.GLCS.v = 52; nc.GLCS.v0 = MaxReal;
              nc.X.v = cmd.Flt["MCS.OriginPoint.X"]; nc.X.v0 = MaxReal;
              nc.Y.v = cmd.Flt["MCS.OriginPoint.Y"]; nc.Y.v0 = MaxReal;
              nc.Z.v = cmd.Flt["MCS.OriginPoint.Z"]; nc.Z.v0 = MaxReal;
              nc.Block.Out();
              nc.X.v = MaxReal; nc.X.v0 = nc.X.v;
              nc.Y.v = MaxReal; nc.Y.v0 = nc.Y.v;
              nc.Z.v = MaxReal; nc.Z.v0 = nc.Z.v;
            }
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