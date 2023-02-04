// This file may be autogenerated, do not place meaningful code here.
// Use it only to define a list of nc-words (registers) that may appear
// in blocks of the nc-file.
namespace SprutTechnology.SCPostprocessor
{
    ///<summary>A class that defines the nc-file - main output file that should be generated by the postprocessor.</summary>
    public partial class NCFile : TTextNCFile
    {
        ///<summary>The block of the nc-file is an ordered list of nc-words</summary>
        public NCBlock Block;
        ///<summary>auto counting</summary>
        public CountingNCWord BlockN = new CountingNCWord("N{####}", 10, 10, 1);
        ///<summary>G code 0,1,3</summary>
        public NumericNCWord GInterp = new NumericNCWord("G{######}", double.MaxValue);
        ///<summary>G code for plane</summary>
        public NumericNCWord GPlane = new NumericNCWord("G{######}", double.MaxValue);
        ///<summary></summary>
        public NumericNCWord KorEcv = new NumericNCWord("G{######}", double.MaxValue);
        ///<summary>G code for coordinate system</summary>
        public NumericNCWord CoordSys = new NumericNCWord("G{######}", double.MaxValue);
        ///<summary>G54</summary>
        public TextNCWord G54 = new TextNCWord("G", "54", "");
        ///<summary>G4</summary>
        public NumericNCWord GPause = new NumericNCWord("G{#}", double.MaxValue);
        ///<summary>G94, G95</summary>
        public NumericNCWord GFeed = new NumericNCWord("G{######}", double.MaxValue);
        ///<summary>SUPA</summary>
        public NumericNCWord SUPA = new NumericNCWord("SUPA", 0);
        ///<summary>Tool number</summary>
        public NumericNCWord Tool = new NumericNCWord("T=\"T{#}\";", double.MaxValue);
        ///<summary>X coordinate of the movement</summary>
        public NumericNCWord X = new NumericNCWord("X{-#####.###}", double.NaN);
        ///<summary>Y coordinate of the movement</summary>
        public NumericNCWord Y = new NumericNCWord("Y{-#####.###}", double.NaN);
        ///<summary>Z coordinate of the movement</summary>
        public NumericNCWord Z = new NumericNCWord("Z{-#####.###}", double.NaN);
        ///<summary>A coordinate of the movement</summary>
        public NumericNCWord A = new NumericNCWord("A{-#####.###}", double.NaN);
        ///<summary>B coordinate of the movement</summary>
        public NumericNCWord B = new NumericNCWord("B{-#####.###}", double.NaN);
        ///<summary>C coordinate of the movement</summary>
        public NumericNCWord C = new NumericNCWord("C{-#####.###}", double.NaN);
        ///<summary>Circle I coordinate</summary>
        public NumericNCWord XC_ = new NumericNCWord("I=AC({-#####.###})", double.NaN);
        ///<summary>Circle J coordinate</summary>
        public NumericNCWord YC_ = new NumericNCWord("J=AC({-#####.###})", double.NaN);
        ///<summary>Circle K coordinate</summary>
        public NumericNCWord ZC_ = new NumericNCWord("K=AC({-#####.###})", double.NaN);
        ///<summary>S spindle speed</summary>
        public NumericNCWord S = new NumericNCWord("S{#####}", 0);
        ///<summary>Length correction for tool</summary>
        public NumericNCWord DTool = new NumericNCWord("D{######}", double.MaxValue);
        ///<summary>Number of turns</summary>
        public NumericNCWord Turn = new NumericNCWord("TURN={#####}", 1);
        ///<summary>M</summary>
        public NumericNCWord M = new NumericNCWord("M{####}", 0);
        ///<summary>Spindle on-off: M3, M4, M5</summary>
        public NumericNCWord Msp = new NumericNCWord("M{#}", 0);
        ///<summary>Coolant for M8, M9</summary>
        public NumericNCWord MCoolant = new NumericNCWord("M{#}", 9);
        ///<summary>Axes A break</summary>
        public NumericNCWord MABrake = new NumericNCWord("M{####}", double.MaxValue);
        ///<summary>Axes B break</summary>
        public NumericNCWord MBBrake = new NumericNCWord("M{####}", double.MaxValue);
        ///<summary>Axes C break</summary>
        public NumericNCWord MCBrake = new NumericNCWord("M{####}", double.MaxValue);
        ///<summary>Feed value</summary>
        public NumericNCWord Feed = new NumericNCWord("F{######}", 10000);
        ///<summary></summary>
        public NumericNCWord FPause = new NumericNCWord("F{##}", double.MaxValue);
        ///<summary>Text field</summary>
        public TextNCWord Text = new TextNCWord("", "", "");
        public NCFile() : base()
        {
            Block = new NCBlock(
                  this,
                  BlockN,
                  GInterp,
                  GPlane,
                  KorEcv,
                  CoordSys,
                  G54,
                  GPause,
                  GFeed,
                  SUPA,
                  Tool,
                  X,
                  Y,
                  Z,
                  A,
                  B,
                  C,
                  XC_,
                  YC_,
                  ZC_,
                  S,
                  DTool,
                  Turn,
                  M,
                  Msp,
                  MCoolant,
                  MABrake,
                  MBBrake,
                  MCBrake,
                  Feed,
                  FPause,
                  Text);
            OnInit();
        }
    }
}