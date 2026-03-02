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
}
