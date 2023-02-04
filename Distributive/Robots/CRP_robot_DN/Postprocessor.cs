using System.Collections;

namespace SprutTechnology.SCPostprocessor
{
    /// <summary>The type that defines how to write coordinate system (UFRAME, TFRAME)</summary>
    public enum CSWriteMode
    {
        /// <summary>Write only the number of CS</summary>
        NumberOnly,
        /// <summary>Write the number and coordinates of CS</summary>
        Coordinates
    }

    /// <summary>The type that defines what the robot holds - tool or part.</summary>
    public enum RobotHolds
    {
        /// <summary>Robot holds tool</summary>
        Tool,
        /// <summary>Robot holds part</summary>
        Part
    }

    /// <summary>The type of robot movement</summary>
    public enum RobotMovementType
    {
        Joint,
        Line,
        Circle
    }

    /// <summary>The type that contains the values of robot axes J1-J6, E1-E6 and some joints' properties</summary>
    public class Joints
    {
        /// <summary>Array of joints' values J1-J6. J[0] doesn't use, just for convenient indexing starting from 1.</summary>
        public double[] J = new double[7];
        
        /// <summary>Array of robot's external axes' values E1-E6. E[0] doesn't use, just for convenient indexing starting from 1.</summary>
        public double[] E = new double[7];
        
        /// <summary>The flag that defines enabled the external axis E[i] or not</summary>
        public bool[] IsEOn = new bool[7];

        /// <summary>Units of external axis E[i] - 'mm' or 'deg'</summary>
        public bool[] EUnitsAreDegrees = new bool[7];

        /// <summary>True if at least one of E1-E6 is enabled</summary>
        public bool ThereAreE = false;

        /// <summary>Method to first initialize joints</summary>
        public Joints() {
            for (int i=1; i<=6; i++)
                EUnitsAreDegrees[i] = false;
        }

        /// <summary>Method to read axes values from axes array of MultiGoto (and MultiArc) command to J[1..6] and E[1..6]</summary>
        public void FillJoints(IEnumerable axes) 
        {    
            foreach(CLDMultiMotionAxis ax in axes) {
                for (int i=1; i<=6; i++) {
                    if (SameText(ax.ID, "AxisA"+i+"Pos")) {
                        this.J[i] = ax.Value;
                        break;
                    } 
                    else 
                    if (SameText(ax.ID, "ExtAxis"+i+"Pos")) {
                        this.E[i] = ax.Value;
                        break;
                    } 
                }
            }
        }

        /// <summary>Method to read from the machine's properties the list of available external axes and their type</summary>
        public void DefineExtAxesIsOn(ICLDProject prj) {
            foreach (ICLDMachineAxisInfo ax in prj.Machine.Axes) {
                if (!ax.Enabled)
                    continue;
                for (int i=1; i<=6; i++) {
                    if (SameText(ax.AxisID, "ExtAxis"+i+"Pos")) {
                        IsEOn[i] = true;
                        EUnitsAreDegrees[i] = ax.IsRotary;
                        break;
                    } 
                }
            }
            // If E[i] is on then enabling all axes below i
            bool on = false;
            for (int i=6; i>=1; i--) {
                on = on || IsEOn[i];
                IsEOn[i] = on;
            }
            ThereAreE = on;
        }

    }

    ///<summary>List of variables that contain robot's state - axes values (J1-J6, E1-E6), flips, current and previous
    ///position in cartesian CS</summary>
    public class RobotState
    {
        ///<summary>Joints' values (J1-J6, E1-E6)</summary>
        public Joints J;

        ///<summary>Cartesian coordinates (X Y Z A B C) in current position</summary>
        public TInpLocation Pos;

        ///<summary>Cartesian coordinates (X Y Z A B C) in previous position</summary>
        public TInpLocation prevPos;

        ///<summary>Cartesian coordinates (X Y Z A B C) in arc middle position</summary>
        public TInpLocation middlePos;

        ///<summary>Some undefined number 9999999</summary>
        public static double Undefined = 9999999;

        ///<summary>Undefined cartesian coordinates constant (X Y Z A B C)=9999999</summary>
        public static TInpLocation UndefinedLocation = new TInpLocation(Undefined, Undefined, Undefined, Undefined, Undefined, Undefined, Undefined);

        ///<summary>Current spindle revolutions</summary>
        public int spindleRevs = 0;

        ///<summary>True if tool is active (rotates)</summary>
        public bool toolIsOn = false;

        ///<summary>Current velocity of movements in mm/min</summary>
        public double velocity = 0;

        ///<summary>Current PL value for non joint movements</summary>
        public double PLValue = 9;

        ///<summary>Current ACC value</summary>
        public double accValue = 0;

        ///<summary>Current feed kind</summary>
        public CLDFeedKind feedKind;
        
        ///<summary>User frame number (UFRAME)</summary>
        public int BaseNum;

        ///<summary>User frame coordinates (X Y Z W P R)</summary>
        public TInpLocation Base;

        ///<summary>User tool number (TFRAME)</summary>
        public int ToolNum;

        ///<summary>User tool coordinates (X Y Z W P R)</summary>
        public TInpLocation Tool;

        ///<summary>User frame output mode (numbers only or with coordinates)</summary>
        public CSWriteMode BaseMode;

        ///<summary>User tool output mode (numbers only or with coordinates)</summary>
        public CSWriteMode ToolMode;

        ///<summary>What the robot holds - tool or part</summary>
        public RobotHolds robotHolds;

        ///<summary>Method to first initialize robot's state</summary>
        public RobotState() 
        {
            J = new Joints();
        }
    }

    ///<summary>Resulting robot program file we are writing</summary>
    public partial class RobotProgramFile: TTextNCFile
    {
        ///<summary>Program file index</summary>
        public int FileIndex {get; set;}

        ///<summary>The name of program file program</summary> 
        public string Name {get; set;}

        ///<summary>The count of movements inside the file</summary> 
        public int MoveCount {get; set;}

        /// <summary>Method in wich is possible to initialize some properties of the file</summary>
        public override void OnInit()
        {
        //     this.TextEncoding = Encoding.GetEncoding("windows-1251");
        }
    }

    /// <summary>The main class of the postprocessor that inherits from an abstract TPostprocessor class</summary>
    public partial class Postprocessor: TPostprocessor
    {
        #region Common variables definition

        ///<summary>Current robot program file the postprocessor writes</summary>
        RobotProgramFile prg;

        ///<summary>True if project's units are inches</summary>
        bool unitsAreInches = false;

        ///<summary>The maximal count of movements inside file to split</summary> 
        public int MaxMoveCount = 1000000000;
        
        ///<summary>Robot's current state - cartesian coordinates, joints, flips</summary>
        RobotState state;

        ///<summary>Last movement type</summary>
        RobotMovementType lastMoveType;
 
       ///<summary>Effector state type</summary>
        bool effectorIsOn=false;
 

        #endregion

        /// <summary>The method to be possible to use CLData breakpoints. Just add a "Function breakpoint" with this method's name.</summary>
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        /// <summary>Starts writing of a new program file. Finishes previous program file if it was.</summary>
        /// <param name="repeatHeader">Whether to repeat header in the starting file</param>
        void StartProgramFile(bool repeatHeader) {
            int index = 0;
            if (prg!=null) { // if already have the previous file
                index = prg.FileIndex + 1;
                FinishProgramFile();
            }
            
            prg = new RobotProgramFile();
            prg.FileIndex = index;
            prg.Name = Settings.Params.Str["OutFiles.ProgramName"];
            if (index>0) {
                prg.Name = prg.Name + index;
            } else {
                if ((prg.Name.Length<1) || (prg.Name.Length>256)) {
                    Log.Error("The program name can be from 1 to 256 characters.");
                } else if (Char.IsDigit(prg.Name[0]))    
                    Log.Error("The program name can NOT begin with a NUMBER.");
                if (prg.Name.Contains('@') || prg.Name.Contains('*'))    
                    Log.Error("Using (@) or (*) in program name is not allowed.");
            }    
            prg.OutputFileName = Settings.Params.Str["OutFiles.OutputFolder"] + @"\" + prg.Name;

            if (repeatHeader) {
        //        prg.WriteLine("DOUT M#(505)=ON");
            }
        }

        /// <summary>Finishes writing of the current ls-file. Makes the header of this file.</summary>
        void FinishProgramFile() {
            if (prg==null)
                return;
        //    prg.WriteLine("DOUT M#(505)=OFF");
        }
        
        /// <summary>Instead of PartNo</summary>
        public override void OnStartProject(ICLDProject prj)
        {
            unitsAreInches = prj.Int["Units"]>0;
            MaxMoveCount = Settings.Params.Int["OutFiles.MaxMoveCount"];
            
            state = new RobotState();
            state.accValue = Settings.Params.Flt["Smoothing.DefaultACC"];
            state.PLValue = Settings.Params.Int["Smoothing.PLValue"];
            state.velocity = Settings.Params.Flt["Smoothing.StartVelocity"];
            state.J.DefineExtAxesIsOn(prj);
            state.BaseMode = (CSWriteMode)Settings.Params.Int["Format.BaseFormat"];
            state.ToolMode = (CSWriteMode)Settings.Params.Int["Format.ToolFormat"];
            state.robotHolds = (RobotHolds)Settings.Params.Int["Format.RobotHolds"];
        }
        
        /// <summary>Instead of Fini</summary>
        public override void OnFinishProject(ICLDProject prj)
        {
            FinishProgramFile();
        }

        /// <summary>Instead of PPFun(58)</summary>
        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            state.Pos = RobotState.UndefinedLocation;
            state.feedKind = CLDFeedKind.First;

            if ((Pos("ExtTool", op.ToolRevolverID)>=0) || (Pos("TableTool", op.ToolRevolverID)>=0)) 
                state.robotHolds = RobotHolds.Part;

            if (prg==null)
                StartProgramFile(true);
        }  

        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            prg.WriteLine(cmd.Text);
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            // if (cmd.IsOn) {
            //     int revs = Round(cmd.RPMValue*2000/18000);
            //     if (revs != state.spindleRevs) {
            //         prg.WriteLine("AO[1]=" + revs + ";");
            //         state.spindleRevs = revs;
            //     }
            //     if (!state.toolIsOn) {
            //         prg.WriteLine("DO[1]=ON ;");
            //         prg.WriteLine("WAIT  5.00(sec) ;");
            //         state.toolIsOn = true;
            //     }
            // } else if (cmd.IsOff) {
            //     if (state.toolIsOn) {
            //         prg.WriteLine("DO[1]=OFF ;");
            //         state.toolIsOn = false;
            //     }
            // }   
        }

        public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            int n = Round(cmd.CSNumber);
            if (n>=53)
                n = n - 53;
            TInpLocation coords = cmd.MCS;
            if (state.robotHolds==RobotHolds.Tool) 
            {
                state.BaseNum = n - n;
                state.Base = coords;
            } 
            else //if (state.robotHolds==RobotHolds.Part)
            {
                state.ToolNum = n;
                state.Tool = coords;
            }
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            int n = cmd.Number;
            if (n>=53)
                n = n - 53;
            TInpLocation coords = cmd.Overhang;
            if (state.robotHolds==RobotHolds.Tool) 
            {
                state.ToolNum = n;
                state.Tool = coords;
            } 
            else //if (state.robotHolds==RobotHolds.Part)
            {
                state.BaseNum = n;
                state.Base = coords;
            }
        }

        /// <summary>Fedrate + Rapid</summary>
        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            state.feedKind = cmd.FeedKind;
            if (unitsAreInches)
                state.velocity = cmd.FeedValue*25.4 / 60;
            else
                state.velocity = cmd.FeedValue / 60;
            // if (cmd.IsRapid)
            //     state.accValue = 100;    
        }

        public override void OnFrom(ICLDFromCommand cmd, CLDArray cld)
        {
            Motion(cmd);            
        }

        public override void OnMultiGoto(ICLDMultiGotoCommand cmd, CLDArray cld)
        {
            Motion(cmd);            
        }

        public override void OnPhysicGoto(ICLDPhysicGotoCommand cmd, CLDArray cld)
        {
            Motion(cmd);
        }

        public override void OnGoHome(ICLDGoHomeCommand cmd, CLDArray cld)
        {
            Motion(cmd);
        }

        public override void OnMultiArc(ICLDMultiArcCommand cmd, CLDArray cld)
        {
            Motion(cmd);
        }

        /// <summary>All movement commands call this method (MultiGOTO, PhysicGoto, MultiArc, From)</summary>
        void Motion(ICLDMotionCommand movement) 
        {
            state.prevPos = state.Pos;
            if (movement.CmdType==CLDCmdType.MultiArc) {
                if ((lastMoveType==RobotMovementType.Joint) || (lastMoveType==RobotMovementType.Line))
                    WriteState(RobotMovementType.Circle, 1);
                
                ICLDMultiArcCommand cmd = movement as ICLDMultiArcCommand;
                state.Pos = new TInpLocation(cmd.MidP.P, cmd.MidP.N);
                state.J.FillJoints(cmd.MidP.Axes);
                state.middlePos = state.Pos;
                WriteState(RobotMovementType.Circle, 2);

                state.Pos = new TInpLocation(cmd.EndP.P, cmd.EndP.N);
                state.J.FillJoints(cmd.EndP.Axes);
                WriteState(RobotMovementType.Circle, 3);
            } else {
                ICLDMultiMotionCommand cmd = movement as ICLDMultiMotionCommand;
                state.Pos = new TInpLocation(cmd.EP, cmd.EN);
                state.J.FillJoints(cmd.Axes);
                if (movement.CmdType==CLDCmdType.From) {
                    // Output nothing, just remember
                    return;
                } else if (movement.CmdType==CLDCmdType.MultiGoto) {
                    WriteState(RobotMovementType.Line);
                } else { //PhysicGoto or GoHome
                    WriteState(RobotMovementType.Joint);
                }
            }
        }

        void WriteState(RobotMovementType movType, int circlePointNumber = 0)
        {
            //MOVL VL=500.0 PL=9 ACC=0.0 DEC=0 TOOL=1 BASE=0 USE=0 COUNT=0 J1=102.3428388323 J2=79.9043432916 J3=-42.1848670696 J4=-6.2346733636 J5=27.6416595213 J6=15.1561628069 J7=0.0000000000 J8=0.0000000000 J9=0.0000000000 Nx=-0.0439043706 Ny=0.9820992279 Nz=0.1831761798 Ox=0.9989712543 Oy=0.0410740796 Oz=0.0192185595 Ax=0.0113507395 Ay=0.1838315169 Az=-0.9828922291 Px=-303.8966104802 Py=1428.6400814277 Pz=78.5759473199 E=0.00000 N1 
            switch (movType)    
            {
                case RobotMovementType.Joint:
                    prg.MovType.Show("MOVJ");
                    prg.PL.Show(0);
                    prg.VJ.Show(); 
                    break;
                case RobotMovementType.Line:
                    prg.MovType.Show("MOVL");
                    prg.PL.Show(state.PLValue);
                    prg.VL.Show(state.velocity); 
                    break;
                case RobotMovementType.Circle:
                    prg.MovType.Show("MOVC");
                    prg.PL.Show(state.PLValue);
                    prg.VL.Show(state.velocity);
                    prg.Point.Show(circlePointNumber);
                    break;
            }
            prg.ACC.Show(state.accValue); 
            prg.DEC.Show();
            prg.Tool.Show(state.ToolNum); 
            prg.Base.Show(state.BaseNum); 
            prg.Use.Show();
            prg.Count.Show();
            prg.J1.Show(state.J.J[1]); 
            prg.J2.Show(state.J.J[2]); 
            prg.J3.Show(state.J.J[3]); 
            prg.J4.Show(state.J.J[4]); 
            prg.J5.Show(state.J.J[5]); 
            prg.J6.Show(state.J.J[6]); 
            if (state.J.IsEOn[1]) 
                prg.J7.Show(state.J.E[1]); 
            if (state.J.IsEOn[2]) 
                prg.J8.Show(state.J.E[2]); 
            if (state.J.IsEOn[3]) 
                prg.J9.Show(state.J.E[3]);

            TComplexRotationConvention convention = new TComplexRotationConvention(TRotationConvention.XYZ, true, false);
            T3DMatrix mtx = TRotationsConverter.LocationToMatrix(state.Pos, convention);

            prg.Nx.Show(mtx.vX.X);
            prg.Ny.Show(mtx.vX.Y);
            prg.Nz.Show(mtx.vX.Z);
            prg.Ox.Show(mtx.vY.X);
            prg.Oy.Show(mtx.vY.Y);
            prg.Oz.Show(mtx.vY.Z);
            prg.Ax.Show(mtx.vZ.X);
            prg.Ay.Show(mtx.vZ.Y);
            prg.Az.Show(mtx.vZ.Z);
            prg.Px.Show(state.Pos.P.X);
            prg.Py.Show(state.Pos.P.Y);
            prg.Pz.Show(state.Pos.P.Z);

            prg.E.Show();
            prg.MoveCount++;
            prg.N.Show(prg.MoveCount);
            if (prg.MoveCount>=MaxMoveCount) {
                StartProgramFile(true);
            }
            prg.Block.Out();
            lastMoveType = movType;
        }

        /// <summary>Returns true if two coorinate systems are equal with defined tolerance</summary>
        bool IsEqualLocation(TInpLocation loc1, TInpLocation loc2, double tolerance) {
            return IsEqD(loc1.P.X, loc2.P.X, tolerance) &&
                   IsEqD(loc1.P.Y, loc2.P.Y, tolerance) &&
                   IsEqD(loc1.P.Z, loc2.P.Z, tolerance) &&
                   IsEqD(loc1.N.A, loc2.N.A, tolerance) &&
                   IsEqD(loc1.N.B, loc2.N.B, tolerance) &&
                   IsEqD(loc1.N.C, loc2.N.C, tolerance);
        }

        public override void OnEffector(ICLDEffectorCommand cmd, CLDArray cld)
        {
        //    if (cmd.IsOn && !effectorIsOn) {
        //        prg.WriteLine("DOUT M#(111)=ON");
        //    } else if (cmd.IsOff  && effectorIsOn) {
        //        prg.WriteLine("DOUT M#(111)=OFF");
        //    }
            effectorIsOn = cmd.IsOn;
        }
    }
}
