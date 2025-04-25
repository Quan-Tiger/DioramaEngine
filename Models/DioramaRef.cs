namespace DioramaEngine.Models;

public class DioramaRef
{
    public string FormID { get; set; }
    public string Name { get; set; }
    public bool Saved { get; set; }
    public float PosX { get; set; }
    public float PosY { get; set; }
    public float PosZ { get; set; }
    public float RotX { get; set; }
    public float RotY { get; set; }
    public float RotZ { get; set; }
    public float Scale { get; set; }
    public bool IsSelected { get; set; }
    public string Mod { get; set; }
    public string Cell { get; set; }
    public string CellMod { get; set; }
    public string BaseFormId { get; set; }
    public string BaseFormMod { get; set; }
    public bool IsDisabled { get; set; }

    public bool IsNew => FormID.ToLower().StartsWith("0xff");
    public bool IsFromESL => FormID.ToLower().StartsWith("0xfe");

    public static string GetFormattedFormID(string formID, bool fromESL)
    {
        // ESL - 0xFEYYYXXX -> 000XXX
        // ESP - 0xYYXXXXXX -> XXXXXX
        return fromESL
            ? formID.Replace("0x", "")[^3..].PadLeft(6, '0')
            : formID.Replace("0x", "").PadLeft(8, '0')[2..];
    }

    public static int GetModIndex(string formID, bool fromESL)
    {
        // ESL - 0xFE[XXX]XXX
        // ESP - 0x[XX]XXXXXX
        return fromESL
            ? int.Parse(formID[4..7], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture)
            : int.Parse(formID[2..4], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
    }
}