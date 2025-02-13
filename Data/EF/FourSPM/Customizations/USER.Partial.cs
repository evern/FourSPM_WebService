namespace FourSPM_WebService.Data.EF.FourSPM;

public partial class USER
{
    public string FullName => string.Concat(FIRST_NAME, " ", LAST_NAME);
}
