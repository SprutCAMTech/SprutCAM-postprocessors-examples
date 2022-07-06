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

    /// <summary>The type of the robot's J3 joint</summary>
    public enum Join3Type
    {
        /// <summary>J3 and J2 value are independent</summary>
        J3Independent,
        /// <summary>J3 value depends on J2 value (parallelogram)</summary>
        J3DependsOnJ2
    }

    /// <summary>The type that contains the values of robot axes J1-J6, E1-E6 and some joints' properties</summary>
    public class Joints
    {
        /// <summary>Source value of a J3 (before subtraction of J2) for the case of dependent J3 and J2 joints</summary>
        public double J3Src;
        
        /// <summary>Array of joints' values J1-J6. J[0] doesn't use, just for convenient indexing starting from 1.</summary>
        public double[] J = new double[7];
        
        /// <summary>Array of robot's external axes' values E1-E6. E[0] doesn't use, just for convenient indexing starting from 1.</summary>
        public double[] E = new double[7];
        
        /// <summary>The flag that defines enabled the external axis E[i] or not</summary>
        public bool[] IsEOn = new bool[7];

        /// <summary>String for units of external axis E[i] - 'mm' or 'deg'</summary>
        public string[] EUnits = new string[7];

        /// <summary>True if at least one of E1-E6 is enabled</summary>
        public bool ThereAreE = false;

        /// <summary>Defines depends J3 from J2 or not</summary>
        public Join3Type J3Type;

        /// <summary>The number of group (GP1 or GP2) to write external axes E</summary>
        public int ExtAxesGroup = 1;

        /// <summary>Method to first initialize joints</summary>
        public Joints() {
            for (int i=1; i<=6; i++)
                EUnits[i] = "mm";
        }

        /// <summary>Method to read axes values from axes array of MultiGoto (and MultiArc) command to J[1..6] and E[1..6]</summary>
        public void FillJoints(IEnumerable axes) 
        {    
            foreach(CLDMultiMotionAxis ax in axes) {
                for (int i=1; i<=6; i++) {
                    if (SameText(ax.ID, "AxisA"+i+"Pos")) {
                        this.J[i] = ax.Value;
                        if (i==3) // Remember source J3 value to subtract J2 below
                            J3Src = ax.Value;
                        break;
                    } 
                    else 
                    if (SameText(ax.ID, "ExtAxis"+i+"Pos")) {
                        this.E[i] = ax.Value;
                        break;
                    } 
                }
            }
            // Subtract J3 = J3 - J2 if they are independent
            if (J3Type==Join3Type.J3Independent)
                J[3] = J3Src - J[2];
        }

        /// <summary>Method to read from the machine's properties the list of available external axes and their type</summary>
        public void DefineExtAxesIsOn(ICLDProject prj) {
            foreach (ICLDMachineAxisInfo ax in prj.Machine.Axes) {
                if (!ax.Enabled)
                    continue;
                for (int i=1; i<=6; i++) {
                    if (SameText(ax.AxisID, "ExtAxis"+i+"Pos")) {
                        IsEOn[i] = true;
                        if (ax.IsRotary)
                            EUnits[i] = "deg";
                        else
                            EUnits[i] = "mm";
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

    /// <summary>Configuration (flips, CONFIG) of the robot's axes</summary>
    public struct Config 
    {
        /// <summary>J5 flip - F or N</summary>
        public bool Flip;

        /// <summary>J3 flip - D or U</summary>
        public bool Down;

        /// <summary>J1 flip - B or T</summary>
        public bool Front;

        /// <summary>J1 axis range number (-1, 0, +1)</summary>
        public int J1;

        /// <summary>J4 axis range number (-1, 0, +1)</summary>
        public int J4;

        /// <summary>J6 axis range number (-1, 0, +1)</summary>
        public int J6;

        /// <summary>Method to read flips from SprutCAM's format to this internal flags</summary>
        public void Fill (int machineStateFlags, Joints J) {
            // Flip = (machineStateFlags & 0b001) != 0;
            Flip = J.J[5] > 0;
            Down = (machineStateFlags & 0b010) != 0;
            Front = (machineStateFlags & 0b100) != 0;

            if (J.J[1]<=-180)
                J1 = -1;
            else if (J.J[1]>=180)
                J1 = 1;
            else 
                J1 = 0;

            if (J.J[4]<=-180)
                J4 = -1;
            else if (J.J[4]>=180)
                J4 = 1;
            else 
                J4 = 0;

            if (J.J[6]<=-180)
                J6 = -1;
            else if (J.J[6]>=180)
                J6 = 1;
            else 
                J6 = 0;
        }

        /// <summary>Method to convert flips from internal flags to the fanuc's CONFIG string format</summary>
        public override string ToString() {
            string res = "";
            if (Flip)
                res = "F";
            else
                res = "N";
            if (Down)
                res = res + " D";
            else    
                res = res + " U";
            if (Front)
                res = res + " B";
            else    
                res = res + " T";
            res = res + ", " + J1 + ", " + J4 + ", " + J6;    
            return res;
        }
        
    }

    /// <summary>List of additional parameters to calculate degree of smoothing CNT and ACC</summary>
    public struct SmoothingParameters 
    {
        ///<summary>Default ACC value</summary>
        public int accDefaultValue;

        ///<summary>Maximal length to think that the movement is short for smoothing</summary>
        public double ShortLength;

        /// <summary>The velocity starts with to calculate smoothing. If velocity less that this value then smoothing is maxiaml (CNT100)</summary>
        public double StartVelocity;

        /// <summary>Value of CNT for rapid and joint motions</summary>
        public int cntValueRapid;

        /// <summary>Value of CNT for angles less than angMin</summary>
        public int cntValueMin;

        /// <summary>Value of CNT for angles more than angMax</summary>
        public int cntValueMax;

        /// <summary>Minimal angle between neighbor spans of toolpath to start smoothing calculation. 
        /// Angles less than this value are always on cntValueMin</summary>
        public double angMin;

        /// <summary>Maximal angle between neighbor spans of toolpath to finish smoothing calculation. 
        /// Angles more than this value are always on cntValueMax</summary>
        public double angMax;
    }

    ///<summary>List of variables that contain robot's state - axes values (J1-J6, E1-E6), flips, current and previous
    ///position in cartesian CS</summary>
    public class RobotState
    {
        ///<summary>Joints' values (J1-J6, E1-E6)</summary>
        public Joints J;

        ///<summary>Cartesian coordinates (X Y Z W P R) in current position</summary>
        public TInpLocation Pos;

        ///<summary>Cartesian coordinates (X Y Z W P R) in previous position</summary>
        public TInpLocation prevPos;

        ///<summary>Cartesian coordinates (X Y Z W P R) in arc middle position</summary>
        public TInpLocation middlePos;

        ///<summary>Some undefined number 9999999</summary>
        public static double Undefined = 9999999;

        ///<summary>Undefined cartesian coordinates constant (X Y Z W P R)=9999999</summary>
        public static TInpLocation UndefinedLocation = new TInpLocation(Undefined, Undefined, Undefined, Undefined, Undefined, Undefined, Undefined);

        ///<summary>Robot's flips configuration (CONFIG)</summary>
        public Config Flips;

        ///<summary>Current spindle revolutions</summary>
        public int spindleRevs = 0;

        ///<summary>True if tool is active (rotates)</summary>
        public bool toolIsOn = false;

        ///<summary>Current velocity of movements in mm/min</summary>
        public double velocity = 0;

        ///<summary>Current ACC value</summary>
        public int accValue = 65;

        ///<summary>Current CNT value. FINE = -1</summary>
        public int cntValue = 0;

        ///<summary>Current feed kind</summary>
        public CLDFeedKind feedKind;
        
        ///<summary>User frame number (UFRAME)</summary>
        public int uFrameNum;

        ///<summary>User frame coordinates (X Y Z W P R)</summary>
        public TInpLocation uFrame;

        ///<summary>User tool number (TFRAME)</summary>
        public int uToolNum;

        ///<summary>User tool coordinates (X Y Z W P R)</summary>
        public TInpLocation uTool;

        ///<summary>User frame output mode (numbers only or with coordinates)</summary>
        public CSWriteMode uFrameWriteMode;

        ///<summary>User tool output mode (numbers only or with coordinates)</summary>
        public CSWriteMode uToolWriteMode;

        ///<summary>What the robot holds - tool or part</summary>
        public RobotHolds robotHolds;

        ///<summary>Method to first initialize robot's state</summary>
        public RobotState() 
        {
            J = new Joints();
        }
    }

    ///<summary>List of geometrical properties for a one span of toolpath</summary>
    public struct ToolpathSpan
    {
        ///<summary>Radius of the span. R==0 means Cut, else - Arc</summary>
        public double R;

        ///<summary>Starting point of the span X,Y,Z</summary>
        public T3DPoint sp;

        ///<summary>End point of the span X,Y,Z</summary>
        public T3DPoint ep;

        ///<summary>Center point X,Y,Z for arc spans only</summary>
        public T3DPoint pc;

        ///<summary>Arc plane normal vector</summary>
        public T3DPoint Normal;

        ///<summary>True if the span is Cut</summary>
        public bool IsCut() {
            return R==0;
        }

        ///<summary>True if the span is Arc</summary>
        public bool IsArc() {
            return R!=0;
        }

        ///<summary>Returns the tangent vector of the span at point p</summary>
        public T3DPoint GetUnitTangentAtPoint(T3DPoint p) {
            if (IsCut()) {
                return T3DPoint.Norm(ep-sp);
            } else {
                T3DArc arc = new T3DArc(sp, ep, R, pc, Normal);
                return arc.GetUnitTangent(p);
            }
        }

        ///<summary>Returns the length of the span</summary>
        public double GetLength() {
            if (IsCut()) {
                return T3DPoint.Distance(ep, sp);
            } else {
                return VML.CalcArcLength(sp, ep, pc, Normal, R, false);
            }
        }

        ///<summary>Creates new Cut by two points (sp, ep)</summary>
        public static ToolpathSpan CreateCut(T3DPoint sp, T3DPoint ep) {
            ToolpathSpan result;
            result.R = 0;
            result.sp = sp;
            result.ep = ep;
            result.pc = T3DPoint.Zero;
            result.Normal = T3DPoint.UnitZ;
            return result;
        }

        ///<summary>Creates new Arc by three points (sp, mp, ep)</summary>
        public static ToolpathSpan CreateArc(T3DPoint sp, T3DPoint mp, T3DPoint ep) {
            ToolpathSpan result;
            T3DArc arc = T3DArc.Create(sp, mp, ep, 0.0001);
            result.R = arc.Rc;
            result.sp = sp;
            result.ep = ep;
            result.pc = arc.Pc;
            result.Normal = arc.Nc;
            return result;
        }
    }

    ///<summary>Movements file *.ls we are writing</summary>
    public partial class LSFile: TTextNCFile
    {
        ///<summary>ls-file index</summary>
        public int FileIndex {get; set;}

        ///<summary>The name of ls-file program</summary> 
        public string Name {get; set;}

        ///<summary>The count of movements inside the file</summary> 
        public int MoveCount {get; set;}

        ///<summary>The count of points inside the file</summary> 
        public int PointCount {get; set;}
        
        ///<summary>A label that contains position at header section (/PROG) inside the file</summary> 
        public INCLabel headerSection;

        ///<summary>A label that contains position at movements section (/MN) inside the file</summary> 
        public INCLabel movementsSection;

        ///<summary>A label that contains position at points section (/POS) inside the file</summary> 
        public INCLabel pointsSection;
        
        ///<summary>Method initializes the main structure of the file as a set of three sections - header, movements, points</summary> 
        public void InitSections() {
            WriteLine("/PROG " + Name.ToUpper());
            headerSection = CreateLabel();
            WriteLine("/MN");
            movementsSection = CreateLabel();
            WriteLine("/POS");
            pointsSection = CreateLabel();
        }

        ///<summary>Switches further writing to the header section</summary> 
        public void SwitchToHeaderSection() {
            headerSection.SnapToRight();
            this.DefaultLabel = headerSection;
        }
        
        ///<summary>Switches further writing to the movements section</summary> 
        public void SwitchToMovementsSection() {
            movementsSection.SnapToRight();
            this.DefaultLabel = movementsSection;
        }
        
        ///<summary>Switches further writing to the points section</summary> 
        public void SwitchToPointsSection() {
            pointsSection.SnapToRight();
            this.DefaultLabel = pointsSection;            
        }

        ///<summary>Writes current position of the robot (state) to the points section as joints or 
        ///as cartesian position depend on isJointMove</summary>
        ///<param name="isJointMove">How to write the point: as joint or as cartesian position</param>
        ///<param name="state">Current state of the robot</param>
        ///<returns>Index of a written point</returns>
        public int AddPoint(bool isJointMove, RobotState state) {
            PointCount++;
            SwitchToPointsSection();
            WriteLine("P[" + PointCount + "]{");
            WriteLine(" GP1:");
            Write("\tUF : " + state.uFrameNum + ", UT : " + state.uToolNum + ", ");
            if (isJointMove) {
                WriteLine();
                for (int i=1; i<=6; i++) {
                    Write("\tJ"+i+" = " + J.ToString(state.J.J[i]) + " deg");
                    if (i!=6)
                        Write(",");
                    if (i==3)
                        WriteLine();
                }
            } else {
                WriteLine("CONFIG : '" + state.Flips.ToString() + "',");
                Write("\tX = " + C.ToString(state.Pos.P.X) + " mm,");
                Write("\tY = " + C.ToString(state.Pos.P.Y) + " mm,");
                WriteLine("\tZ = " + C.ToString(state.Pos.P.Z) + " mm,");
                Write("\tW = " + C.ToString(state.Pos.N.A) + " deg,");
                Write("\tP = " + C.ToString(state.Pos.N.B) + " deg,");
                Write("\tR = " + C.ToString(state.Pos.N.C) + " deg");
            }
            if (state.J.ThereAreE) {
                string JWord = "";
                if (state.J.ExtAxesGroup==1) {
                    JWord = "E";
                    WriteLine(",");
                } else {
                    JWord = "J";
                    WriteLine();
                    WriteLine(" GP2:");
                    WriteLine("\tUF : " + state.uFrameNum + ", UT : " + state.uToolNum + ", ");
                }
                for (int i=1; i<=6; i++) {
                    if (!state.J.IsEOn[i])
                        break;
                    Write("\t" + JWord + i + " = " + E.ToString(state.J.E[i]) + " " + state.J.EUnits[i]);
                    if ((i<6) && state.J.IsEOn[i+1])
                        Write(",");
                    if (i==3)
                        WriteLine();
                }
                WriteLine();
            } else 
                WriteLine();

            WriteLine("};");
            return PointCount;
        }

        /// <summary>Writes new line s to the movements section with automatic numbering "1234:s"</summary>
        /// <param name="s">Text to write to the movements section</param>
        /// <returns>Total count of written movements</returns>
        public int AddMotionLine(string s)
        {
            SwitchToMovementsSection();
            MoveCount++;
            WriteLine("\t" + MoveCount + ":" + s);
            return MoveCount;
        }

        /// <summary>Writes coordinate system (UFRAME, TFRAME) to the movements section</summary>
        /// <param name="writeCoords">Should it write coordinates of CS</param>
        /// <param name="writeNumber">Should it write number of CS</param>
        /// <param name="csName">Name of CS - UFRAME or TFRAME</param>
        /// <param name="csNumber">The number of CS</param>
        /// <param name="PRNumber">First index i of PR[i,j] to wich to write</param>
        /// <param name="coords">Cartesian coordinates (X, Y, Z, W, P, R) of the writing CS</param>
        public void WriteCS(bool writeCoords, bool writeNumber, string csName, int csNumber, int PRNumber, TInpLocation coords) {
            if (writeCoords) {
                AddMotionLine(" PR["+PRNumber+",1]=" + Str(coords.P.X, 3, 3) + " ;");
                AddMotionLine(" PR["+PRNumber+",2]=" + Str(coords.P.Y, 3, 3) + " ;");
                AddMotionLine(" PR["+PRNumber+",3]=" + Str(coords.P.Z, 3, 3) + " ;");
                AddMotionLine(" PR["+PRNumber+",4]=" + Str(coords.N.A, 3, 3) + " ;");
                AddMotionLine(" PR["+PRNumber+",5]=" + Str(coords.N.B, 3, 3) + " ;");
                AddMotionLine(" PR["+PRNumber+",6]=" + Str(coords.N.C, 3, 3) + " ;");
                AddMotionLine(" " + csName + "[" + csNumber + "]=PR[" + PRNumber + "] ;");
            }
            if (writeNumber)
                AddMotionLine(" " + csName + "_NUM=" + csNumber + " ;");
        }

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

        ///<summary>Current list of movements file *.ls the postprocessor writes</summary>
        LSFile ls;

        ///<summary>maketp.bat file - LS files compilation runner (batch script)</summary>
        TTextNCFile maketpFile;

        ///<summary>True if project's units are inches</summary>
        bool unitsAreInches = false;

        ///<summary>The maximal count of movements inside file to split</summary> 
        public int MaxMoveCount = 1000000000;
        
        ///<summary>Robot's current state - cartesian coordinates, joints, flips</summary>
        RobotState state;

        ///<summary>Parameters to calculate degree of smoothing CNT</summary>
        SmoothingParameters smooth;
 
        #endregion

        /// <summary>The method to be possible to use CLData breakpoints. Just add a "Function breakpoint" with this method's name.</summary>
        public override void StopOnCLData() 
        {
            // Do nothing, just to be possible to use CLData breakpoints
        }

        /// <summary>Starts writing of a new ls-file. Finishes previous ls-file if it was.</summary>
        /// <param name="repeatHeader">Whether to repeat UFRAME and TFRAME in the starting file</param>
        void StartNewLSFile(bool repeatHeader) {
            int index = 0;
            if (ls!=null) { // if already have the previous file
                index = ls.FileIndex + 1;
                FinishLSFile();
            }
            
            ls = new LSFile();
            ls.FileIndex = index;
            ls.Name = Settings.Params.Str["OutFiles.LSFileName"];
            if (index>0) {
                ls.Name = ls.Name + index;
            } else {
                if ((ls.Name.Length<1) || (ls.Name.Length>8)) {
                    Log.Error("The program name can be from ONE to EIGHT characters.");
                } else if (Char.IsDigit(ls.Name[0]))    
                    Log.Error("The program name can NOT begin with a NUMBER.");
                if (ls.Name.Contains('@') || ls.Name.Contains('*'))    
                    Log.Error("Using (@) or (*) in program name is not allowed.");
            }    
            ls.OutputFileName = Settings.Params.Str["OutFiles.OutputFolder"] + @"\" + ls.Name + ".ls";

            var tpFileName = Path.ChangeExtension(ls.OutputFileName, ".tp");
            maketpFile.Output("call maketp.exe \"" + ls.OutputFileName + "\" \"" + tpFileName + "\"");

            ls.InitSections();

            if (repeatHeader) {
                ls.SwitchToMovementsSection();
                ls.WriteCS(state.uFrameWriteMode==CSWriteMode.Coordinates, true, "UFRAME", state.uFrameNum, 5, state.uFrame);
                ls.WriteCS(state.uToolWriteMode==CSWriteMode.Coordinates, true, "UTOOL", state.uToolNum, 4, state.uTool);
            }
        }

        /// <summary>Finishes writing of the current ls-file. Makes the header of this file.</summary>
        void FinishLSFile() {
            ls.SwitchToHeaderSection();
            ls.Output("/ATTR");
            ls.Output("OWNER\t\t= MNEDITOR;");
            ls.Output("PROG_SIZE\t= 0;");
            ls.Output("LINE_COUNT\t= " + ls.MoveCount + ";");
            ls.Output("MEMORY_SIZE\t= 0;");
            ls.Output("PROTECT\t\t= READ_WRITE;");
            ls.Output("TCD: STACK_SIZE\t= 0,");
            ls.Output("\t\tTASK_PRIORITY	= 50,");
            ls.Output("\t\tTIME_SLICE	= 0,");
            ls.Output("\t\tBUSY_LAMP_OFF	= 0,");
            ls.Output("\t\tABORT_REQUEST	= 0,");
            ls.Output("\t\tPAUSE_REQUEST	= 0;");
            if (state.J.ThereAreE && (state.J.ExtAxesGroup==2))
                ls.Output("DEFAULT_GROUP\t= 1,1,*,*,*;");
            else    
                ls.Output("DEFAULT_GROUP\t= 1,*,*,*,*;");
            ls.Output("CONTROL_CODE\t= 00000000 00000000;");
            ls.SwitchToPointsSection();
            ls.Output("/END");
        }
        
        /// <summary>Instead of PartNo</summary>
        public override void OnStartProject(ICLDProject prj)
        {
            unitsAreInches = prj.Int["Units"]>0;
            MaxMoveCount = Settings.Params.Int["OutFiles.MaxMoveCount"];

            smooth.accDefaultValue = Settings.Params.Int["Smoothing.DefaultACC"];
            smooth.ShortLength = Settings.Params.Flt["Smoothing.ShortLength"];
            smooth.StartVelocity = 60*Settings.Params.Flt["Smoothing.StartVelocity"];
            smooth.cntValueRapid = Settings.Params.Int["Smoothing.CntValueRapid"];
            smooth.cntValueMin = Settings.Params.Int["Smoothing.CntValueMin"];
            smooth.cntValueMax = Settings.Params.Int["Smoothing.CntValueMax"];
            smooth.angMin = Settings.Params.Int["Smoothing.AngMin"];
            smooth.angMax = Settings.Params.Int["Smoothing.AngMax"];
            
            state = new RobotState();
            state.accValue = smooth.accDefaultValue;
            state.J.DefineExtAxesIsOn(prj);
            // state.J.J3Type = (Join3Type)Settings.Params.Int["Format.A3Type"];
            state.J.ExtAxesGroup = Settings.Params.Int["Format.ExtAxesGroup"];
            state.uFrameWriteMode = (CSWriteMode)Settings.Params.Int["Format.UFrameFormat"];
            state.uToolWriteMode = (CSWriteMode)Settings.Params.Int["Format.UToolFormat"];
            state.robotHolds = (RobotHolds)Settings.Params.Int["Format.RobotHolds"];

            maketpFile = new TTextNCFile();
            maketpFile.OutputFileName = Settings.Params.Str["OutFiles.OutputFolder"] + @"\maketp.bat";
            maketpFile.Output("call setrobot.exe");            
        }
        
        /// <summary>Instead of Fini</summary>
        public override void OnFinishProject(ICLDProject prj)
        {
            FinishLSFile();
            maketpFile.Output("pause");
        }

        /// <summary>Instead of PPFun(58)</summary>
        public override void OnStartTechOperation(ICLDTechOperation op, ICLDPPFunCommand cmd, CLDArray cld)
        {
            state.Pos = RobotState.UndefinedLocation;
            state.feedKind = CLDFeedKind.First;

            if (cld[62]==0)
                state.J.J3Type = Join3Type.J3Independent;
            else
                state.J.J3Type = Join3Type.J3DependsOnJ2;

            if ((Pos("ExtTool", op.ToolRevolverID)>=0) || (Pos("TableTool", op.ToolRevolverID)>=0)) 
                state.robotHolds = RobotHolds.Part;

            if (ls==null)
                StartNewLSFile(false);
        }  

        public override void OnInsert(ICLDInsertCommand cmd, CLDArray cld)
        {
            AddMotionLine(" " + cmd.Text);
        }

        public override void OnSpindle(ICLDSpindleCommand cmd, CLDArray cld)
        {
            // if (cmd.IsOn) {
            //     int revs = Round(cmd.RPMValue*2000/18000);
            //     if (revs != state.spindleRevs) {
            //         AddMotionLine(" AO[1]=" + revs + ";");
            //         state.spindleRevs = revs;
            //     }
            //     if (!state.toolIsOn) {
            //         AddMotionLine(" DO[1]=ON ;");
            //         AddMotionLine(" WAIT  5.00(sec) ;");
            //         state.toolIsOn = true;
            //     }
            // } else if (cmd.IsOff) {
            //     if (state.toolIsOn) {
            //         AddMotionLine(" DO[1]=OFF ;");
            //         state.toolIsOn = false;
            //     }
            // }   
        }

        public override void OnWorkpieceCS(ICLDOriginCommand cmd, CLDArray cld)
        {
            const double tolerance = 0.001;
            int n = Round(cmd.CSNumber);
            if (n>=53)
                n = n - 53;
            TInpLocation coords = cmd.MCS;
            if (state.robotHolds==RobotHolds.Tool) 
            {
                bool shouldWriteCoords = 
                    (state.uFrameWriteMode==CSWriteMode.Coordinates) && !IsEqualLocation(state.uFrame, coords, tolerance);
                ls.WriteCS(shouldWriteCoords, n!=state.uFrameNum, "UFRAME", n, 5, coords);
                state.uFrameNum = n;
                state.uFrame = coords;
            } 
            else //if (state.robotHolds==RobotHolds.Part)
            {
                bool shouldWriteCoords = 
                    (state.uToolWriteMode==CSWriteMode.Coordinates) && !IsEqualLocation(state.uTool, coords, tolerance);
                ls.WriteCS(shouldWriteCoords, n!=state.uToolNum, "UTOOL", n, 5, coords);
                state.uToolNum = n;
                state.uTool = coords;
            }
        }

        public override void OnLoadTool(ICLDLoadToolCommand cmd, CLDArray cld)
        {
            const double tolerance = 0.001;
            int n = cmd.Number;
            if (n>=53)
                n = n - 53;
            TInpLocation coords = cmd.Overhang;
            if (state.robotHolds==RobotHolds.Tool) 
            {
                bool shouldWriteCoords = 
                    (state.uToolWriteMode==CSWriteMode.Coordinates) && !IsEqualLocation(state.uTool, coords, tolerance);
                ls.WriteCS(shouldWriteCoords, n!=state.uToolNum, "UTOOL", n, 4, coords);
                state.uToolNum = n;
                state.uTool = coords;
            } 
            else //if (state.robotHolds==RobotHolds.Part)
            {
                bool shouldWriteCoords = 
                    (state.uFrameWriteMode==CSWriteMode.Coordinates) && !IsEqualLocation(state.uFrame, coords, tolerance);
                ls.WriteCS(shouldWriteCoords, n!=state.uFrameNum, "UFRAME", n, 4, coords);
                state.uFrameNum = n;
                state.uFrame = coords;
            }
        }

        /// <summary>Fedrate + Rapid</summary>
        public override void OnMoveVelocity(ICLDMoveVelocityCommand cmd, CLDArray cld)
        {
            state.feedKind = cmd.FeedKind;
            if (unitsAreInches)
                state.velocity = cmd.FeedValue*25.4;
            else
                state.velocity = cmd.FeedValue;
            if (cmd.IsRapid)
                state.accValue = 100;    
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
                ICLDMultiArcCommand cmd = movement as ICLDMultiArcCommand;
                state.Pos = new TInpLocation(cmd.MidP.P, cmd.MidP.N);
                state.J.FillJoints(cmd.MidP.Axes);
                state.Flips.Fill(cmd.Int["MidPos.MachineStateFlags"], state.J);
                int midPointIndex = ls.AddPoint(false, state);
                state.middlePos = state.Pos;

                state.Pos = new TInpLocation(cmd.EndP.P, cmd.EndP.N);
                state.J.FillJoints(cmd.EndP.Axes);
                state.Flips.Fill(cmd.Int["EndPos.MachineStateFlags"], state.J);
                int endPointIndex = ls.AddPoint(false, state);

                CalculateCNT(movement);
                AddMotionLine("C P[" + midPointIndex + "] \r\n\t\tP[" + endPointIndex + "] " + Round(state.velocity/60) + 
                    " mm/sec " + FineOrCnt(state.cntValue) + " ACC" + state.accValue + " ;", false);
            } else {
                ICLDMultiMotionCommand cmd = movement as ICLDMultiMotionCommand;
                state.Pos = new TInpLocation(cmd.EP, cmd.EN);
                state.J.FillJoints(cmd.Axes);
                state.Flips.Fill(cmd.Int["MachineStateFlags"], state.J);
                if (movement.CmdType==CLDCmdType.From) {
                    // Output nothing, just remember
                    return;
                } else if (movement.CmdType==CLDCmdType.MultiGoto) {
                    // Lines
                    CalculateCNT(movement);
                    int pointIndex = ls.AddPoint(false, state);
                    AddMotionLine("L P[" + pointIndex + "] " + Round(state.velocity/60) + " mm/sec " + FineOrCnt(state.cntValue) + " ACC" + state.accValue + " ;");
                } else { //PhysicGoto or GoHome
                    // Joints
                    state.cntValue = smooth.cntValueRapid;
                    int pointIndex = ls.AddPoint(true, state);
                    if (!state.J.ThereAreE || (state.J.ExtAxesGroup==2)) {
                        AddMotionLine("J P[" + pointIndex + "] 10% " + FineOrCnt(state.cntValue) + " ;");
                    } else {
                        AddMotionLine("J P[" + pointIndex + "] 10% " + FineOrCnt(state.cntValue) + " ACC30;");
                    }
                }
            }
        }

        /// <summary>Calculates the CNT (state.cntValue) - degree of smoothing in %</summary>
        void CalculateCNT(ICLDMotionCommand curMov) {
            if (state.feedKind==CLDFeedKind.Rapid) {
                if (IsNextMovementNonRapid(curMov))
                    state.cntValue = 0;
                else
                    state.cntValue = smooth.cntValueRapid;
                return;
            }
            if (IsFinalWorkingMovement(curMov)) {
                state.cntValue = 0;
                return;
            }
            state.accValue = smooth.accDefaultValue;
            if (IsLastMovementInFile(curMov)) {
                state.cntValue = -1; // Fine
                return;
            }
            if (state.velocity>smooth.StartVelocity) {
                var nextMov = FindNextNonShortMovement(curMov);
                if (nextMov!=null) {
                    double len1, len2, r1, r2;
                    double ang = CalculateAngle(curMov, nextMov, out len1, out len2, out r1, out r2);
                    // double R = 1000000;
                    // if (ang<=175)
                    //     R = (len1+len2)/((180-ang)*DegToRad);
                    // Print("R(" + (ls.MoveCount+1) + ") = ", R);
                    double t = 0;
                    // if ((R<5) || ((r1>0) && (r1<2)))
                    //     t = 1.0/3.0;                    
                    // else 
                    if (ang<smooth.angMin)
                        t = 0;
                    else if (ang>smooth.angMax)
                        t = 1;
                    else 
                        t = (ang - smooth.angMin)/(smooth.angMax-smooth.angMin);
                    double cnt = (1-t)*smooth.cntValueMin + t*smooth.cntValueMax;
                    state.cntValue = 5*Round(cnt/5);
                    // Debug.WriteLine("Cmd:"+curMov.Index + " Ang = " + ang.ToString() + " cnt = " + state.cntValue);
                } else
                    state.cntValue = smooth.cntValueMax;
            } else
                state.cntValue = 100;
        }

        /// <summary>Calculates the angle between current movement and next movement</summary>
        /// <param name="curMov">Current movement command</param>
        /// <param name="nextMov">Next movement command</param>
        /// <param name="len1">Length of the current movement</param>
        /// <param name="len2">Length of the next movement</param>
        /// <param name="r1">Radius of the current movement (0 if it is a Cut)</param>
        /// <param name="r2">Radius of the next movement (0 if it is a Cut)</param>
        /// <returns>Angle between movements in deg. 180 for a smooth tangent case.</returns>
        double CalculateAngle(ICLDMotionCommand curMov, ICLDMotionCommand nextMov, out double len1, out double len2, out double r1, out double r2) {
            ToolpathSpan curSpan;
            ToolpathSpan nextSpan;
            if (curMov.CmdType==CLDCmdType.MultiArc)
                curSpan = ToolpathSpan.CreateArc(state.prevPos.P, state.middlePos.P, state.Pos.P);
            else
                curSpan = ToolpathSpan.CreateCut(state.prevPos.P, state.Pos.P);
            if (nextMov.CmdType==CLDCmdType.MultiArc) {
                var mArc = nextMov as ICLDMultiArcCommand;
                nextSpan = ToolpathSpan.CreateArc(state.Pos.P, mArc.MidP.P, mArc.EndP.P);
            } else
                nextSpan = ToolpathSpan.CreateCut(state.Pos.P, nextMov.EP);

            len1 = curSpan.GetLength();
            len2 = nextSpan.GetLength();
            r1 = curSpan.R;
            r2 = nextSpan.R;

            T3DPoint tg1 = curSpan.GetUnitTangentAtPoint(state.Pos.P);
            T3DPoint tg2 = nextSpan.GetUnitTangentAtPoint(state.Pos.P);
            double ang = 180 - VML.CalcVecsAngle(tg1, tg2)*RadToDeg;
            return ang;
        }

        /// <summary>Returns True if the next movement is not rapid</summary>
        bool IsNextMovementNonRapid(ICLDMotionCommand movement) {
            if (movement==null)
                return false;
            ICLDCommand cmd = movement.Next;
            while (cmd!=null) {
                CLDCmdType typ = cmd.CmdType;
                if (typ==CLDCmdType.Fedrat)
                    return true;    
                else if ((typ==CLDCmdType.Rapid) || (typ==CLDCmdType.PhysicGoto) || (typ==CLDCmdType.MultiGoto))
                    return false;    
                cmd = cmd.Next;    
            }            
            return false;
        }

        /// <summary>Returns True if current feed is working and the next is rapid</summary>
        bool IsFinalWorkingMovement(ICLDMotionCommand movement)
        {
            if (movement==null)
                return false;
            ICLDCommand cmd = movement.Next;
            bool curFeedIsWorking = IsWorkFeed(state.feedKind);
            while (cmd!=null) {
                CLDCmdType typ = cmd.CmdType;
                if ((typ==CLDCmdType.MultiGoto) || (typ==CLDCmdType.MultiArc) )
                    return false;    
                bool IsNonWorkingFeed = (typ==CLDCmdType.Rapid) ||
                    ((typ==CLDCmdType.Fedrat) && !IsWorkFeed((cmd as ICLDFeedrateCommand).FeedKind));                 
                if (curFeedIsWorking && IsNonWorkingFeed)
                    return true;    
                cmd = cmd.Next;    
            }            
            return curFeedIsWorking;
        }

        /// <summary>Returns True if the feedKind is one of a working feeds</summary>
        bool IsWorkFeed(CLDFeedKind feedKind) {
            return ((feedKind==CLDFeedKind.Working) || (feedKind==CLDFeedKind.Finish) || (feedKind==CLDFeedKind.First));
        }

        /// <summary>Returns True if the next movement is arc</summary>
        bool NextMovementIsArc(ICLDMotionCommand movement)
        {
            if (movement==null)
                return false;
            ICLDCommand cmd = movement.Next;
            while (cmd!=null) {
                CLDCmdType typ = cmd.CmdType;
                if (typ==CLDCmdType.MultiArc)
                    return true;
                else if ((typ==CLDCmdType.MultiGoto) || (typ==CLDCmdType.PhysicGoto) || (typ==CLDCmdType.GoHome))
                    return false;    
                cmd = cmd.Next;    
            }            
            return false;
        }

        /// <summary>Returns True if the movement is last because of max line count reached</summary>
        bool IsLastMovementInFile(ICLDMotionCommand movement) {
            if (movement.CmdType==CLDCmdType.MultiGoto)
                return ls.MoveCount+1>=MaxMoveCount;
            else
                return (ls.MoveCount+3>=MaxMoveCount) && !NextMovementIsArc(movement);
        }

        /// <summary>Returns the next movement with the length more than smooth.ShortLength</summary>
        ICLDMotionCommand FindNextNonShortMovement(ICLDMotionCommand movement) {
            if (movement==null)
                return null;
            ICLDCommand cmd = movement.Next;
            while (cmd!=null) {
                switch (cmd.CmdType) {
                    case CLDCmdType.MultiArc:
                        return cmd as ICLDMultiArcCommand;
                    case CLDCmdType.MultiGoto: {
                        var mg = cmd as ICLDMultiGotoCommand;
                        double length = mg.Time*state.velocity;
                        if (length>smooth.ShortLength)
                            return mg;
                        break;
                    }
                    case CLDCmdType.PhysicGoto:
                    case CLDCmdType.GoHome:
                        return null;
                }
                cmd = cmd.Next;
            }
            return null;
        }

        /// <summary>Converts integer cntValue to a FINE or CNTxxx string</summary>
        string FineOrCnt(int cntValue) {
            if (cntValue<0)
                return "FINE";
            else
                return "CNT" + cntValue;
        }

        /// <summary>Adds new line s to the movements section of the current ls-file. 
        /// Starts new ls file if the max line count limit exceeds.</summary>
        /// <param name="s">Text to write</param>
        /// <param name="allowNewFile">Can it split current ls-file and start new one</param>
        void AddMotionLine(string s, bool allowNewFile = true)
        {
            ls.AddMotionLine(s);
            if (allowNewFile && (ls.MoveCount>=MaxMoveCount)) {
                StartNewLSFile(true);
            }
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
    }
}
