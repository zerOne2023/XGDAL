using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Abstractions;

public interface IShapefileReader
{
    ShapefileDatasetInfo GetInfo(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile);
    IAsyncEnumerable<FeatureRecord> ReadAsync(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default);
}
