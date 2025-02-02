// This file may be autogenerated, do not place meaningful code here.
// Use it only to define a list of nc-words (registers) that may appear
// in blocks of the nc-file.
namespace SprutTechnology.SCPostprocessor;

///<summary>A class that defines the nc-file - main output file that should be generated by the postprocessor.</summary>
public partial class NCFile : TTextNCFile
{
    ///<summary>The block of the nc-file is an ordered list of nc-words</summary>
    public NCBlock Block;

    public CountingNCWord N_RowNumber = new("N{####}", 10, 10, 1);

    /// <summary>
    /// Technology conditions
    /// </summary>
    public NumericNCWord G_Technology = new("G{00}", 0);

    /// <summary>
    /// G90-G91
    /// </summary>
    public NumericNCWord G_AbsInc = new("G{00}", 0);

    /// <summary>
    /// G92
    /// </summary>
    public NumericNCWord G_CoordinateSystem = new("G{00}", 0);

    public NumericNCWord G_LowerInterpolation = new("G{00}", 999999);

    /// <summary>
    /// G50-G52
    /// </summary>
    public NumericNCWord G_Taper = new("G{00}", 50);

    /// <summary>
    /// G60-G61
    /// </summary>
    public NumericNCWord G_2Contour = new("G{00}", 60);

    /// <summary>
    /// G74-G75
    /// </summary>
    public NumericNCWord G_UV = new("G{00}", 75);

    /// <summary>
    /// G40-G42
    /// </summary>
    public NumericNCWord G_Compensation = new("G{00}", 0);

    public NumericNCWord G_MeasurementUnits = new("G{###}", 0);

    public NumericNCWord G_TaperMode = new("G{###}", 0);

    /// <summary>
    /// G48-G49
    /// </summary>
    public NumericNCWord G_RollMode = new("G{###}", 0);

    public NumericNCWord X_Lower = new("X{-####!###}", 0);

    public NumericNCWord Y_Lower = new("Y{-####!###}", 0);

    public NumericNCWord I_LowerPcX = new("I{-####!###}", 0);

    public NumericNCWord J_LowerPcY = new("J{-####!###}", 0);

    public NumericNCWord I_UpperPlane = new("I{-###.##}", 0);

    public NumericNCWord J_LowerPlane = new("J{-###.##}", 0);

    public NumericNCWord G_UpperInterpolation = new("G{00}", 0);

    public NumericNCWord U_UpperX = new("U{-####!###}", 0);

    public NumericNCWord V_UpperY = new("V{-####!###}", 0);

    public NumericNCWord K_UpperPcX = new("K{-####!###}", 0);

    public NumericNCWord L_UpperPcY = new("L{-####!###}", 0);

    public NumericNCWord T_Angle = new("T{-####!###}", 0);

    public NumericNCWord R_LowerRoll = new("R{-####!###}", 0);

    public NumericNCWord M_Stop = new("M{00}", 0);

    public NumericNCWord M_LoadWire = new("M{###}", 0);

    public NumericNCWord M_Sub = new("M{00}", 0);

    public NumericNCWord S_TechnologyCode = new("S{###}", 0);

    public NumericNCWord M_ResetMachiningTime = new("M{###}", 0);

    public NCFile() : base()
    {
        Block = new NCBlock(
              this,
              N_RowNumber,
              G_Technology,
              G_AbsInc,
              G_CoordinateSystem,
              G_LowerInterpolation,
              G_Taper,
              G_2Contour,
              G_UV,
              G_Compensation,
              G_MeasurementUnits,
              G_TaperMode,
              G_RollMode,
              X_Lower,
              Y_Lower,
              I_LowerPcX,
              J_LowerPcY,
              I_UpperPlane,
              J_LowerPlane,
              G_UpperInterpolation,
              U_UpperX,
              V_UpperY,
              K_UpperPcX,
              L_UpperPcY,
              T_Angle,
              R_LowerRoll,
              M_Stop,
              M_LoadWire,
              M_Sub,
              S_TechnologyCode,
              M_ResetMachiningTime);
        OnInit();
    }
}