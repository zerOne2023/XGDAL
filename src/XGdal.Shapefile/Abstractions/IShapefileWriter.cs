using XGdal.Shapefile.Configuration;
using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Abstractions;

public interface IShapefileWriter
{
    Task WriteAsync(
        string dataSourcePath,
        ShapefileSchema schema,
        IAsyncEnumerable<FeatureRecord> features,
        VectorWriteOptions? options = null,
        CancellationToken cancellationToken = default);
}
