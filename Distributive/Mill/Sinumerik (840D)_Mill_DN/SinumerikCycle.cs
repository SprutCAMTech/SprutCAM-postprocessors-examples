namespace SprutTechnology.SCPostprocessor{
    public class CycleState{
        public InpArray<double> Prms;

        public bool Cycle_pocket; 
        
        public bool CycleOn;      // Включен ли цикл

        public bool PolarInterp;     // Полярная интерполяция: false-выключена, true-включена
        public bool CylindInterp;     // Цилиндрическая интерполяция: false-выключена, >true-включена

        public bool IsFirstCycle; // true - если это первый цикл в операции, иначе - false

        public string Cyclecompare = "";    // Сравниваем строки для того, чтобы знать нужно ли выводить цикл

        public CycleState(){
            Prms = new InpArray<double>();
        }

        public void AddPrm(double value, int i) => Prms[i] = value;
    }

    public class SinumerikCycle{
        // N170 M8 F200
        // N180 MCALL CYCLE81(10,-99.994,1,-135.774)
        // N190 X240 Y-240

        NCFile nc;

        Postprocessor post;

        public CycleState State;

        #region Parametrs

        public bool CycleOn => State.CycleOn;
        public bool Cycle_pocket => State.Cycle_pocket;
        public bool IsFirstCycle => State.IsFirstCycle;

        public bool PolarInterp => State.PolarInterp; 

        public InpArray<double> Prms => State.Prms;

        #endregion

        public SinumerikCycle(Postprocessor post){
            this.post = post;
            nc = post.nc;
            State = new CycleState();
        }

        public void AddPrm(double value, int i) => State.AddPrm(value, i);

        public void Cycle800SwitchOff(){
            nc.GInterp.Hide();
            nc.Block.Out();
            nc.WriteLine($"{nc.BlockN} CYCLE800()");
            nc.BlockN.AddStep();
            nc.GInterp.UpdateState();
        }

        public void Cycle800(int v_FR, string v_TC, int v_ST, int v_MODE, double v_X0, 
            double v_Y0, double v_Z0, double v_A, double v_B, double v_C, 
            double v_X1, double v_Y1, double v_Z1, int v_DIR, double v_FR_I){
            nc.WriteLine($"{nc.BlockN} CYCLE800({v_FR},\"{v_TC}\",{v_ST},{v_MODE},{v_X0},{v_Y0},{v_Z0},{v_A},{v_B},{v_C},{v_X1},{v_Y1},{v_Z1},{v_DIR},{v_FR_I},0)");
            nc.BlockN.AddStep();
        }

        public void OutCycle(string ACycleID, string CycleGeomName){
            int i, n;
            string sss; 

            sss = ACycleID;
            if (Prms.Count > 0)
                sss = sss + "(";
            if (CycleGeomName != "")
                sss = sss + Chr(34) + CycleGeomName + Chr(34);
            n = 0;
            for (i = 0; i < Prms.Count; i++){
                if (Prms[i] != double.MaxValue) n = i + 1;  
            }
            for (i = 0; i < n; i++){
                if ((i > 0) || (CycleGeomName != "")) 
                    sss = sss + ",";
                if (Prms[i] != double.MaxValue)
                    sss = sss + Str(Math.Round(Prms[i], 3));  
            }
            if (Prms.Count > 0) sss = sss + ")";
            if (State.Cyclecompare != sss) {       //Добавил вариант для того, чтобы выводить массив отверстий одним циклом
                if (!IsFirstCycle && !Cycle_pocket) {          //меняется параметр, нужно закрыть цикл
                    nc.WriteLine($"{nc.BlockN} MCALL"); nc.BlockN.AddStep();
                    nc.X.v = post.LastPnt.X; nc.Y.v = post.LastPnt.Y ; nc.GInterp.v0 = double.MaxValue ; nc.Block.Out();   //Холостые ходы между проходами
                }
                var t = nc.GInterp.Changed ? nc.GInterp : null;
                nc.WriteLine($"{nc.BlockN} {t}{sss}"); //Вывод цикла
                nc.BlockN.AddStep();
                this.SetCycleCompareString(sss);  //Запоминаем все параметры цикла
            }
        }

        public void Cycle_position(){
            switch(Abs(nc.GPlane.v)){
                case 17:
                    if (IsFirstCycle) //Не выводит первое отверстие в цикле, т.к. считает что оно уже выведено
                        nc.X.v0 = double.MaxValue; 
                    nc.X.v = post.LastPnt.X; 
                    nc.Y.Show(post.LastPnt.Y);
                    // nc.Y.v = post.LastPnt.Y; nc.Y.v0 = double.MaxValue;  
                    nc.Block.Out(); // Вывод отверстий  ! XY
                    break;
                case 18:
                    if (IsFirstCycle) //Не выводит первое отверстие в цикле, т.к. считает что оно уже выведено
                        nc.Z.v0 = double.MaxValue; 
                    nc.Z.v = post.LastPnt.Z;
                    nc.X.Show(post.LastPnt.X);
                    // nc.X.v = post.LastPnt.X; nc.X.v0 = double.MaxValue;
                    nc.Block.Out(); // Вывод отверстий  ! ZX
                    break;
                case 19:
                    if (IsFirstCycle) //Не выводит первое отверстие в цикле, т.к. считает что оно уже выведено
                        nc.Y.v0 = double.MaxValue; 
                    nc.Y.v = post.LastPnt.Y;
                    nc.Z.Show(post.LastPnt.Z);
                    // nc.Z.v = post.LastPnt.Z; nc.Z.v0 = double.MaxValue;
                    nc.Block.Out(); // Вывод отверстий  ! YZ
                    break;
            }
        }
    }
}