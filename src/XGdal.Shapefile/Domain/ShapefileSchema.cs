namespace XGdal.Shapefile.Domain;

public sealed class ShapefileSchema
{
    public required GeometryKind GeometryKind { get; init; }
    public int? Srid { get; init; }
    public string? LayerName { get; init; }
    public List<FieldDefinition> Fields { get; } = new();
}
