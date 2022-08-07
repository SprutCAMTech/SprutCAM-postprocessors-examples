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
        ///<summary>X coordinate of the movement</summary>
        public NumericNCWord X = new NumericNCWord("X @{-#####!000}", 0);
        ///<summary>Y coordinate of the movement</summary>
        public NumericNCWord Y = new NumericNCWord(",Y @{-#####!000}", 0);
        ///<summary></summary>
        public NumericNCWord Z = new NumericNCWord(",Z @{-#####!000}", 0);
        ///<summary></summary>
        public NumericNCWord A = new NumericNCWord(",A @{-#####!000}", 0);
        ///<summary></summary>
        public NumericNCWord B = new NumericNCWord(",B @{-#####!000}", 0);
        ///<summary></summary>
        public NumericNCWord C = new NumericNCWord(",C @{-#####!000}", 0);
        ///<summary></summary>6
        public NumericNCWord A1 = new NumericNCWord(" A1 @{-####!###}", 0);
        ///<summary></summary>
        public NumericNCWord A2 = new NumericNCWord(",A2 @{-####!000}", 0);
        ///<summary></summary>
        public NumericNCWord A3 = new NumericNCWord(",A3 @{-####!000}", 0);
        ///<summary></summary>
        public NumericNCWord A4 = new NumericNCWord(",A4 @{-####!000}", 0);
        ///<summary></summary>
        public NumericNCWord A5 = new NumericNCWord(",A5 @{-####!000}", 0);
        ///<summary></summary>
        public NumericNCWord A6 = new NumericNCWord(",A6 @{-####!000}", 0);
        ///<summary></summary>
        public NumericNCWord E1 = new NumericNCWord(",E1 @{-######!000}", 0);
        ///<summary></summary>
        public NumericNCWord E2 = new NumericNCWord(",E2 @{-######!000}", 0);
        ///<summary></summary>
        public NumericNCWord E3 = new NumericNCWord(",E3 @{-######!000}", 0);
        ///<summary></summary>
        public NumericNCWord E4 = new NumericNCWord(",E4 @{-######!000}", 0);
        ///<summary></summary>
        public NumericNCWord E5 = new NumericNCWord(",E5 @{-######!000}", 0);
        ///<summary></summary>
        public NumericNCWord E6 = new NumericNCWord(",E6 @{-######!000}", 0);
        ///<summary></summary>
        public NumericNCWord VEL_CP = new NumericNCWord("$VEL.CP={-###.###}", 0);
        ///<summary></summary>
        public NumericNCWord TOOL_DATA = new NumericNCWord("$TOOL=TOOL_DATA[@]{##_##}", 0);
        ///<summary></summary>
        public NumericNCWord BASE_DATA = new NumericNCWord("$BASE=BASE_DATA[@]{##_##}", 0);
        ///<summary></summary>
        public NumericNCWord v_p = new NumericNCWord("{-######!###}", 0);
        public NCFile(): base()
        {
            Block = new NCBlock(
                  this, 
                  X, 
                  Y, 
                  Z, 
                  A, 
                  B, 
                  C, 
                  A1, 
                  A2, 
                  A3, 
                  A4, 
                  A5, 
                  A6, 
                  E1, 
                  E2, 
                  E3, 
                  E4, 
                  E5, 
                  E6, 
                  VEL_CP, 
                  TOOL_DATA, 
                  BASE_DATA, 
                  v_p);
            OnInit();
        }
    }
}