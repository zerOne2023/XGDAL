using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Configuration;

public sealed class VectorWriteOptions
{
    public DataSourceKind DataSourceKind { get; set; } = DataSourceKind.Shapefile;
    public bool CreateSpatialIndex { get; set; } = true;
    public string Encoding { get; set; } = "UTF-8";
    public bool OverwriteExisting { get; set; } = true;
    public string? LayerName { get; set; }
    public List<string> LayerCreationOptions { get; set; } = new();
    public List<string> DatasetCreationOptions { get; set; } = new();
}
