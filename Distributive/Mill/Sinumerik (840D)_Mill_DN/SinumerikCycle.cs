namespace SprutTechnology.SCPostprocessor{
    public class CycleState{
        public string CycleName;

        public double XT_;                // Текущее значение по X
        public double YT_;                // Текущее значение по Y
        public double ZT_;                // Текущее значение по Z

        public InpArray<double> Prms;

        public double Cycle_pocket; 
        
        public double CycleOn;      // Включен ли цикл

        public int PolarInterp;     // Полярная интерполяция: 0-выключена, 1-включена
        public int CylindInterp;     // Цилиндрическая интерполяция: 0-выключена, >0-включена

        public double IsFirstCycle; // 1 - если это первый цикл в операции, иначе - 0

        public string Cyclecompare = "";    // Сравниваем строки для того, чтобы знать нужно ли выводить цикл

        public int WasCycle800 = 0;

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

        public double CycleOn => State.CycleOn;
        public double Cycle_pocket => State.Cycle_pocket;
        public double IsFirstCycle => State.IsFirstCycle;

        public double PolarInterp => State.PolarInterp; 

        public InpArray<double> Prms => State.Prms;

        public double XT_ => State.XT_; 
        public double YT_ => State.YT_; 
        public double ZT_ => State.ZT_; 

        #endregion

        public SinumerikCycle(Postprocessor post){
            this.post = post;
            nc = post.nc;
            State = new CycleState();
        }

        public void AddPrm(double value, int i) => State.AddPrm(value, i);

        public void Cycle800SwitchOff(){
            double tInterp;
            tInterp = nc.GInterp.v0; nc.GInterp.v0 = nc.GInterp.v;
            nc.Block.Out();
            if (State.WasCycle800 != 0) {
                nc.WriteLine($"{nc.BlockN} CYCLE800()");
                nc.BlockN.v += 1;
                this.SetCycle800Status(0);
            }
            nc.GInterp.v0 = tInterp;
        }

        public void Cycle800(int v_FR, string v_TC, int v_ST, int v_MODE, double v_X0, 
            double v_Y0, double v_Z0, double v_A, double v_B, double v_C, 
            double v_X1, double v_Y1, double v_Z1, int v_DIR, double v_FR_I){
            nc.WriteLine($"{nc.BlockN} CYCLE800({v_FR},\"{v_TC}\",{v_ST},{v_MODE},{v_X0},{v_Y0},{v_Z0},{v_A},{v_B},{v_C},{v_X1},{v_Y1},{v_Z1},{v_DIR},{v_FR_I},0)");
            nc.BlockN.v += 1;
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
                if (IsFirstCycle != 1 && Cycle_pocket == 0) {          //меняется параметр, нужно закрыть цикл
                    nc.WriteLine($"{nc.BlockN} MCALL"); nc.BlockN.v += 1;
                    nc.X.v = XT_ ; nc.Y.v = YT_ ; nc.GInterp.v0 = double.MaxValue ; nc.Block.Out();   //Холостые ходы между проходами
                }
                var t = nc.GInterp.Changed ? nc.GInterp : null;
                nc.WriteLine($"{nc.BlockN} {t}{sss}"); //Вывод цикла
                nc.BlockN.v += 1;
                // nc.Block.Form();
                // TextNCWord word = new TextNCWord("", "", ""); word.v = sss;
                // nc.Block.Show(word);  
                this.SetCycleCompareString(sss);  //Запоминаем все параметры цикла
            }
        }

        public void Cycle_position(){
            switch(Abs(nc.GPlane.v)){
                case 17:
                    if (IsFirstCycle == 1) //Не выводит первое отверстие в цикле, т.к. считает что оно уже выведено
                    nc.X.v0 = double.MaxValue; nc.Y.v0 = double.MaxValue;
                    nc.X.v = XT_; nc.Y.v = YT_ ; nc.Block.Out(); // Вывод отверстий  ! XY
                    break;
                case 18:
                    if (IsFirstCycle == 1) //Не выводит первое отверстие в цикле, т.к. считает что оно уже выведено
                    nc.Z.v0 = double.MaxValue; nc.X.v0 = double.MaxValue;
                    nc.Z.v = ZT_; nc.X.v = XT_ ; nc.Block.Out(); // Вывод отверстий  ! ZX
                    break;
                case 19:
                    if (IsFirstCycle == 1) //Не выводит первое отверстие в цикле, т.к. считает что оно уже выведено
                    nc.Y.v0 = double.MaxValue; nc.Z.v0 = double.MaxValue;
                    nc.Y.v = YT_; nc.Z.v = ZT_ ; nc.Block.Out(); // Вывод отверстий  ! YZ
                    break;
            }
        }
    }
}