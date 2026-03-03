using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Configuration;
using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile;

public sealed class ShapefileLibraryFacade
{
    private readonly IGdalRuntimeInitializer _initializer;
    private readonly IShapefileRepository _repository;
    private readonly IGeometryOperationsService _geometry;

    public ShapefileLibraryFacade(
        IGdalRuntimeInitializer initializer,
        IShapefileRepository repository,
        IGeometryOperationsService geometry)
    {
        _initializer = initializer;
        _repository = repository;
        _geometry = geometry;
    }

    public void Initialize(GdalRuntimeOptions options) => _initializer.Initialize(options);

    public Task WriteAsync(string path, ShapefileSchema schema, IAsyncEnumerable<FeatureRecord> features, VectorWriteOptions? options = null, CancellationToken cancellationToken = default)
        => _repository.WriteAsync(path, schema, features, options, cancellationToken);

    public IAsyncEnumerable<FeatureRecord> ReadAsync(string path, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
        => _repository.ReadAsync(path, layerName, kind, cancellationToken);

    public ShapefileDatasetInfo GetInfo(string path, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile)
        => _repository.GetInfo(path, layerName, kind);

    public Task AppendAsync(string path, IAsyncEnumerable<FeatureRecord> features, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
        => _repository.AppendAsync(path, features, layerName, kind, cancellationToken);

    public Task<IReadOnlyList<FeatureRecord>> QueryAsync(string path, FeatureQueryOptions? options = null, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
        => _repository.QueryAsync(path, options, layerName, kind, cancellationToken);

    public Task<int> UpdateAttributesAsync(string path, IReadOnlyDictionary<string, object?> updates, FeatureQueryOptions? options = null, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
        => _repository.UpdateAttributesAsync(path, updates, options, layerName, kind, cancellationToken);

    public Task<int> DeleteFeaturesAsync(string path, FeatureQueryOptions? options = null, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
        => _repository.DeleteFeaturesAsync(path, options, layerName, kind, cancellationToken);

    public Task<FieldStatistics> CalculateStatisticsAsync(string path, string fieldName, FeatureQueryOptions? options = null, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
        => _repository.CalculateStatisticsAsync(path, fieldName, options, layerName, kind, cancellationToken);

    public Geometry Buffer(Geometry geometry, double distance) => _geometry.Buffer(geometry, distance);
    public Geometry Union(IEnumerable<Geometry> geometries) => _geometry.Union(geometries);
    public Geometry Intersection(Geometry left, Geometry right) => _geometry.Intersection(left, right);
    public Geometry Difference(Geometry left, Geometry right) => _geometry.Difference(left, right);
    public Geometry SymmetricDifference(Geometry left, Geometry right) => _geometry.SymmetricDifference(left, right);
    public Geometry Simplify(Geometry geometry, double tolerance, bool preserveTopology = true) => _geometry.Simplify(geometry, tolerance, preserveTopology);
    public double Area(Geometry geometry) => _geometry.Area(geometry);
    public double Length(Geometry geometry) => _geometry.Length(geometry);
    public bool Intersects(Geometry left, Geometry right) => _geometry.Intersects(left, right);
    public bool Contains(Geometry container, Geometry target) => _geometry.Contains(container, target);
    public bool Within(Geometry target, Geometry container) => _geometry.Within(target, container);
    public double Distance(Geometry left, Geometry right) => _geometry.Distance(left, right);
}
