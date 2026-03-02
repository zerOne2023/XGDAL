namespace XGdal.Shapefile.Domain;

public sealed record ShapefileDatasetInfo(
    string FilePath,
    string LayerName,
    DataSourceKind DataSourceKind,
    GeometryKind GeometryKind,
    int? Srid,
    int FeatureCount,
    IReadOnlyCollection<FieldDefinition> Fields);
