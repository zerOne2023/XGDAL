namespace XGdal.Shapefile.Domain;

public sealed record FieldStatistics(
    string FieldName,
    int TotalCount,
    int NullCount,
    int NumericCount,
    double? Min,
    double? Max,
    double? Sum,
    double? Average);
