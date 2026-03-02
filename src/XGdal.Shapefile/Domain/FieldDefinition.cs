namespace XGdal.Shapefile.Domain;

public sealed record FieldDefinition(
    string Name,
    FieldType Type,
    int Width = 0,
    int Precision = 0,
    bool IsNullable = true);
