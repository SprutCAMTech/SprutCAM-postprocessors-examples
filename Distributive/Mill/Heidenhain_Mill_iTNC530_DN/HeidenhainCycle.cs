namespace SprutTechnology.SCPostprocessor
{
    ///<summary>One Q parameter of a Heidenhain cycle.</summary> 
    public struct CycleQParameter
    {
        public int Number;
        public string Name;
        public double Value;
    }

    ///<summary>Actual set of data of a Heidenhain cycle: cycle name, number and the list of Q parameters.</summary> 
    public class CycleState
    {
        public string CycleName;
        public int CycleNumber;
        public InpArray<CycleQParameter> Q;

        public CycleState()
        {
            Q = new InpArray<CycleQParameter>();
        }

        public void Clear()
        {
            CycleName = "";
            CycleNumber = 0;
            Q.Clear();
        }
    }

    ///<summary>Class that helps to form the output of a Heidenhain cycle.</summary> 
    public class HeidenhainCycle
    {
        public static double tolerance = 0.001;
        CycleState curState, prevState;
        Postprocessor post;

        public HeidenhainCycle(Postprocessor post)
        {
            this.post = post;
            curState = new CycleState();
            prevState = new CycleState();
        }

        public bool Started 
        {
            get => prevState.CycleNumber!=0;
        }

        public void Clear()
        {
            curState.Clear();
            prevState.Clear();
        }

        public void CycleDef(int cycleNumber, string cycleName)
        {
            curState.CycleName = cycleName;
            curState.CycleNumber = cycleNumber;
            curState.Q.Clear();
        }

        public void AddQ(int number, double value, string name)
        {
            CycleQParameter q;
            q.Number = number;
            q.Value = value;
            q.Name = name;
            curState.Q.Add(q);
        }

    // 1422 CYCL DEF 252 CIRCULAR POCKET~
    //      Q215=+0       ;MACHINING OPERATION~
    //      Q223=+36      ;CIRCLE DIAMETER~
    //      Q368=+0       ;ALLOWANCE FOR SIDE~
    //      Q207=+200     ;FEED RATE FOR MILLING~
    //      Q351=+1       ;CLIMB OR UP-CUT~
    //      Q201=-17      ;DEPTH~
    //      Q202=+18      ;PLUNGING DEPTH~
    //      Q369=+0       ;ALLOWANCE FOR FLOOR~
    //      Q206=+200     ;FEED RATE FOR PLUNGING~
    //      Q338=+18      ;INFEED FOR FINISHING~
    //      Q200=+1       ;SETUP CLEARANCE~
    //      Q203=+0       ;SURFACE COORDINATE~
    //      Q204=+40      ;2ND SETUP CLEARANCE~
    //      Q370=+1       ;TOOL PATH OVERLAP~
    //      Q366=+1       ;PLUNGE~
    //      Q385=+200     ;FEED RATE FOR FINISHING
    // 1423 CYCL CALL
        public void Call()
        {
            bool same = AreSame(curState, prevState);
            if (same) {
                post.nc.MCycleCall.Show(99);
                post.OutLineMove();
            } else {
                post.nc.OutText($"CYCL DEF {curState.CycleNumber} {curState.CycleName}~");
                CycleQParameter q;
                int charCount = post.nc.BlockN.ToString().Length + 1;
                string indent = StringOfChar(' ', charCount);
                for (int i = 0; i<curState.Q.Count; i++) {
                    q = curState.Q[i];
                    string s = indent + $"Q{q.Number}={post.nc.Number.ToString(q.Value)}";
                    string space = StringOfChar(' ', 20-s.Length);
                    if (i<curState.Q.Count-1)
                        post.nc.WriteLine(s + space + $";{q.Name}~");
                    else
                        post.nc.WriteLine(s + space + $";{q.Name}");
                }
                post.nc.OutText($"CYCL CALL");
            }
            var tmpState = prevState;
            prevState = curState;
            curState = tmpState;
            curState.Clear();
        }

        private bool AreSame(CycleState c1, CycleState c2)
        {
            if ((c1==null) || (c2==null))
                return false;
            if (c1.CycleNumber != c2.CycleNumber)
                return false;
            if (c1.Q.Count != c2.Q.Count)
                return false;
            for (int i = 0; i<c1.Q.Count; i++) {
                CycleQParameter q1, q2;
                q1 = c1.Q[i];
                q2 = c2.Q[i];
                if (q1.Number != q2.Number)
                    return false;
                if (!IsEqD(q1.Value, q2.Value, tolerance))
                    return false;
            }
            return true;
        }
    }

}