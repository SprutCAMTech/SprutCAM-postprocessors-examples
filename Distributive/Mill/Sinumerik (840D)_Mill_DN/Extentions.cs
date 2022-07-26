namespace SprutTechnology.SCPostprocessor{
    public static class SinumerikCycleExtension{
        ///<summary>Set status of the cycle: false - off, true - on</summary>
        public static SinumerikCycle SetStatus(this SinumerikCycle cycle, bool status){
            cycle.State.CycleOn = status;
            return cycle;
        }

        ///<summary>Set polar interpolation status of the cycle: false - off, true - on</summary>
        public static SinumerikCycle SetPolarInterpolationStatus(this SinumerikCycle cycle, bool status){
            cycle.State.PolarInterp = status;
            return cycle;
        }

        ///<summary>Set cilind interpolation status of the cycle: false - off, true - on</summary>
        public static SinumerikCycle SetCilindInterpolationStatus(this SinumerikCycle cycle, bool status){
            cycle.State.CylindInterp = status;
            return cycle;
        }


        ///<summary>Set first status of the cycle: false - not first cycle, true - first cycle</summary>
        public static SinumerikCycle SetFirstStatus(this SinumerikCycle cycle, bool status){
            cycle.State.IsFirstCycle = status;
            return cycle;
        }

        ///<summary>Set cycle800 status of the cycle: false - not cycle800, true - cycle800</summary>
        public static SinumerikCycle SetCycle800Status(this SinumerikCycle cycle, bool status){
            cycle.State.WasCycle800 = status;
            return cycle;
        }

        ///<summary>Set pocket status of the cycle: false - not pocket, true - pocket</summary>
        public static SinumerikCycle SetPocketStatus(this SinumerikCycle cycle, bool status){
            cycle.State.Cycle_pocket = status;
            return cycle;
        }

        ///<summary>Set first status of the cycle: 0 - not cycle800, 1 - cycle800</summary>
        public static SinumerikCycle SetCycleCompareString(this SinumerikCycle cycle, string compare){
            cycle.State.Cyclecompare = compare;
            return cycle;
        }
    }

    public static class CountingNCWordExtension{
        public static void AddStep(this CountingNCWord word) => word.v += word.AutoIncrementStep;
    }
}