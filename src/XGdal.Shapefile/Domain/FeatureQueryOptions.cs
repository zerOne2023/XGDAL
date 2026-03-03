namespace XGdal.Shapefile.Domain;

public sealed class FeatureQueryOptions
{
    public BoundingBox? BoundingBox { get; init; }
    public Dictionary<string, object?> AttributeEquals { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public int? Take { get; init; }
}
