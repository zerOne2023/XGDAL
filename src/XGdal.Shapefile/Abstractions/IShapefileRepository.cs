using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Abstractions;

public interface IShapefileRepository : IShapefileReader, IShapefileWriter
{
    Task AppendAsync(
        string dataSourcePath,
        IAsyncEnumerable<FeatureRecord> features,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default);

    Task OptimizeAsync(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string dataSourcePath, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default);

    Task DeleteAsync(string dataSourcePath, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeatureRecord>> QueryAsync(
        string dataSourcePath,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default);

    Task<int> UpdateAttributesAsync(
        string dataSourcePath,
        IReadOnlyDictionary<string, object?> updates,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default);

    Task<int> DeleteFeaturesAsync(
        string dataSourcePath,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default);

    Task<FieldStatistics> CalculateStatisticsAsync(
        string dataSourcePath,
        string fieldName,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default);
}
