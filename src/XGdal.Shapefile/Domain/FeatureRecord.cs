namespace XGdal.Shapefile.Domain;

public sealed class FeatureRecord
{
    public required Geometry Geometry { get; init; }
    public Dictionary<string, object?> Attributes { get; } = new(StringComparer.OrdinalIgnoreCase);
}
