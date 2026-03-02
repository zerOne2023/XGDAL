namespace XGdal.Shapefile.Configuration;

public sealed class GdalRuntimeOptions
{
    public string? GdalDataPath { get; set; }
    public string? ProjLibPath { get; set; }
    public bool RegisterAllDrivers { get; set; } = true;
    public Dictionary<string, string> ConfigOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
