namespace MeuIPTVPro.Models;

public class Channel
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string LogoUrl { get; set; } = "";
    public string StreamUrl { get; set; } = "";

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? "Canal sem nome" : Name;
    }
}
