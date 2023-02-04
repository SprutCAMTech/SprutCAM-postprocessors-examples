// This file may be autogenerated, do not place meaningful code here. 
// Use it only to define a list of nc-words (registers) that may appear 
// in blocks of the nc-file.
namespace SprutTechnology.SCPostprocessor
{
    ///<summary>A class that defines the nc-file - main output file that should be generated by the postprocessor.</summary>
    public partial class RobotProgramFile: TTextNCFile
    {
        ///<summary>The block of the nc-file is an ordered list of nc-words</summary>
        public NCBlock Block;

        ///<summary>Movement type (MOVJ, MOVL, MOVC)</summary>
        public TextNCWord MovType = new TextNCWord("MOVJ");

        ///<summary>Velocity VL=500.0</summary>
        public NumericNCWord VL = new NumericNCWord("VL={-####!0##}", 500);

        ///<summary>Joints velocity VJ=20.0</summary>
        public NumericNCWord VJ = new NumericNCWord("VJ={-####!0##}", 20);

        ///<summary>PL=9</summary>
        public NumericNCWord PL = new NumericNCWord("PL={####}", 0);

        ///<summary>Acceleration ACC=0.0</summary>
        public NumericNCWord ACC = new NumericNCWord("ACC={####!0##}", 0);

        ///<summary>DEC=0</summary>
        public NumericNCWord DEC = new NumericNCWord("DEC={####}", 0);

        ///<summary>Tool number TOOL=1</summary>
        public NumericNCWord Tool = new NumericNCWord("TOOL={####}", 0);

        ///<summary>Base number BASE=0</summary>
        public NumericNCWord Base = new NumericNCWord("BASE={####}", 0);

        ///<summary>USE=0</summary>
        public NumericNCWord Use = new NumericNCWord("USE={####}", 0);

        ///<summary>POINT=2</summary>
        public NumericNCWord Point = new NumericNCWord("POINT={####}", 0);

        ///<summary>COUNT=0</summary>
        public NumericNCWord Count = new NumericNCWord("COUNT={####}", 0);

        ///<summary>Joint J1=102.3428388323</summary>
        public NumericNCWord J1 = new NumericNCWord("J1={-#####.000000}", 0);

        ///<summary>Joint J2=102.3428388323</summary>
        public NumericNCWord J2 = new NumericNCWord("J2={-#####.000000}", 0);

        ///<summary>Joint J3=102.3428388323</summary>
        public NumericNCWord J3 = new NumericNCWord("J3={-#####.000000}", 0);

        ///<summary>Joint J4=102.3428388323</summary>
        public NumericNCWord J4 = new NumericNCWord("J4={-#####.000000}", 0);

        ///<summary>Joint J5=102.3428388323</summary>
        public NumericNCWord J5 = new NumericNCWord("J5={-#####.000000}", 0);

        ///<summary>Joint J6=102.3428388323</summary>
        public NumericNCWord J6 = new NumericNCWord("J6={-#####.000000}", 0);

        ///<summary>Joint J7=102.3428388323</summary>
        public NumericNCWord J7 = new NumericNCWord("J7={-#####.000000}", 0);

        ///<summary>Joint J8=102.3428388323</summary>
        public NumericNCWord J8 = new NumericNCWord("J8={-#####.000000}", 0);

        ///<summary>Joint J9=102.3428388323</summary>
        public NumericNCWord J9 = new NumericNCWord("J9={-#####.000000}", 0);

        ///<summary>Matrix Nx=-0.3428388323</summary>
        public NumericNCWord Nx = new NumericNCWord("Nx={-#####.000000}", 0);

        ///<summary>Matrix Ny=-0.3428388323</summary>
        public NumericNCWord Ny = new NumericNCWord("Ny={-#####.000000}", 0);

        ///<summary>Matrix Nz=-0.3428388323</summary>
        public NumericNCWord Nz = new NumericNCWord("Nz={-#####.000000}", 0);

        ///<summary>Matrix Ox=-0.3428388323</summary>
        public NumericNCWord Ox = new NumericNCWord("Ox={-#####.000000}", 0);

        ///<summary>Matrix Oy=-0.3428388323</summary>
        public NumericNCWord Oy = new NumericNCWord("Oy={-#####.000000}", 0);

        ///<summary>Matrix Oz=-0.3428388323</summary>
        public NumericNCWord Oz = new NumericNCWord("Oz={-#####.000000}", 0);

        ///<summary>Matrix Ax=-0.3428388323</summary>
        public NumericNCWord Ax = new NumericNCWord("Ax={-#####.000000}", 0);

        ///<summary>Matrix Ay=-0.3428388323</summary>
        public NumericNCWord Ay = new NumericNCWord("Ay={-#####.000000}", 0);

        ///<summary>Matrix Az=-0.3428388323</summary>
        public NumericNCWord Az = new NumericNCWord("Az={-#####.000000}", 0);

        ///<summary>Matrix Px=-0.3428388323</summary>
        public NumericNCWord Px = new NumericNCWord("Px={-#####.000000}", 0);

        ///<summary>Matrix Py=-0.3428388323</summary>
        public NumericNCWord Py = new NumericNCWord("Py={-#####.000000}", 0);

        ///<summary>Matrix Pz=-0.3428388323</summary>
        public NumericNCWord Pz = new NumericNCWord("Pz={-#####.000000}", 0);

        ///<summary>E=0.00000</summary>
        public NumericNCWord E = new NumericNCWord("E={-#####.00000}", 1);

        ///<summary>Block numbers counter N</summary>
        public NumericNCWord N = new NumericNCWord("N{########}", 0);

        ///<summary>Joint value formatter</summary>
        public NumericNCWord J = new NumericNCWord("{-000.000}", 0);

        ///<summary>X, Y, Z, W, P, R value formatter</summary>
        public NumericNCWord C = new NumericNCWord("{-000!000}", 0);

        public RobotProgramFile(): base()
        {
            Block = new NCBlock(this, " ", 
                MovType, 
                VL,
                VJ,
                PL,
                ACC,
                DEC,
                Tool,
                Base,
                Use,
                Point,
                Count,
                J1,
                J2,
                J3,
                J4,
                J5,
                J6,
                J7,
                J8,
                J9,
                Nx,
                Ny,
                Nz,
                Ox,
                Oy,
                Oz,
                Ax,
                Ay,
                Az,
                Px,
                Py,
                Pz,
                E,
                N
            );
            OnInit();
        }

    }
}