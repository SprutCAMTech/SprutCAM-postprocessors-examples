namespace SprutTechnology.SCPostprocessor{
    public static class SinumerikCycleExtension{
        
        ///<summary>Set name of the cycle</summary>
        public static SinumerikCycle SetName(this SinumerikCycle cycle, string name){
            cycle.State.CycleName = name;
            return cycle;
        }

        ///<summary>Set status of the cycle: 0 - off, 1 - on</summary>
        public static SinumerikCycle SetStatus(this SinumerikCycle cycle, int status){
            cycle.State.CycleOn = status;
            return cycle;
        }

        ///<summary>Set polar interpolation status of the cycle: 0 - off, 1 - on</summary>
        public static SinumerikCycle SetPolarInterpolationStatus(this SinumerikCycle cycle, int status){
            cycle.State.PolarInterp = status;
            return cycle;
        }

        ///<summary>Set cilind interpolation status of the cycle: 0 - off, 1 - on</summary>
        public static SinumerikCycle SetCilindInterpolationStatus(this SinumerikCycle cycle, int status){
            cycle.State.CylindInterp = status;
            return cycle;
        }


        ///<summary>Set first status of the cycle: 0 - not first cycle, 1 - first cycle</summary>
        public static SinumerikCycle SetFirstStatus(this SinumerikCycle cycle, int status){
            cycle.State.IsFirstCycle = status;
            return cycle;
        }

        ///<summary>Set cycle800 status of the cycle: 0 - not cycle800, 1 - cycle800</summary>
        public static SinumerikCycle SetCycle800Status(this SinumerikCycle cycle, int status){
            cycle.State.WasCycle800 = status;
            return cycle;
        }

        ///<summary>Set pocket status of the cycle: 0 - not pocket, 1 - pocket</summary>
        public static SinumerikCycle SetPocketStatus(this SinumerikCycle cycle, int status){
            cycle.State.Cycle_pocket = status;
            return cycle;
        }

        ///<summary>Set first status of the cycle: 0 - not cycle800, 1 - cycle800</summary>
        public static SinumerikCycle SetCycleCompareString(this SinumerikCycle cycle, string compare){
            cycle.State.Cyclecompare = compare;
            return cycle;
        }

        ///<summary>Set position for cycle</summary>
        public static SinumerikCycle SetPosition(this SinumerikCycle cycle, double x, double y, double z){
            cycle.State.XT_ = x; cycle.State.YT_ = y; cycle.State.ZT_ = z;
            return cycle;
        }
    }
}