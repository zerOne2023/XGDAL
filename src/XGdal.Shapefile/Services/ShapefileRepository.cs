using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Configuration;
using XGdal.Shapefile.Diagnostics;
using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Services;

public sealed class ShapefileRepository : IShapefileRepository
{
    private readonly IGdalRuntimeInitializer _runtimeInitializer;
    private readonly ILogger<ShapefileRepository> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ShapefileRepository(IGdalRuntimeInitializer runtimeInitializer, ILogger<ShapefileRepository> logger)
    {
        _runtimeInitializer = runtimeInitializer;
        _logger = logger;
    }

    public async Task<bool> ExistsAsync(string dataSourcePath, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePath(dataSourcePath);
        return File.Exists(dataSourcePath);
    }

    public async Task DeleteAsync(string dataSourcePath, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        ValidatePath(dataSourcePath);

        if (File.Exists(dataSourcePath))
        {
            File.Delete(dataSourcePath);
        }
    }

    public ShapefileDatasetInfo GetInfo(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        var store = LoadStore(dataSourcePath);
        return new ShapefileDatasetInfo(
            dataSourcePath,
            store.LayerName,
            kind,
            store.Schema.GeometryKind,
            store.Schema.Srid,
            store.Features.Count,
            store.Schema.Fields);
    }

    public async IAsyncEnumerable<FeatureRecord> ReadAsync(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        var store = LoadStore(dataSourcePath);

        foreach (var feature in store.Features)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return feature;
        }
    }

    public async Task WriteAsync(string dataSourcePath, ShapefileSchema schema, IAsyncEnumerable<FeatureRecord> features, VectorWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(features);

        options ??= new VectorWriteOptions();

        if (!options.OverwriteExisting && File.Exists(dataSourcePath))
        {
            throw new ShapefileException($"Data source already exists: {dataSourcePath}");
        }

        if (options.OverwriteExisting && File.Exists(dataSourcePath))
        {
            File.Delete(dataSourcePath);
        }

        var bufferedFeatures = new List<FeatureRecord>();
        await foreach (var feature in features.WithCancellation(cancellationToken))
        {
            bufferedFeatures.Add(feature);
        }

        var store = new StoredDataSet
        {
            LayerName = options.LayerName ?? schema.LayerName ?? Path.GetFileNameWithoutExtension(dataSourcePath),
            Schema = schema,
            Features = bufferedFeatures
        };

        SaveStore(dataSourcePath, store);
        _logger.LogInformation("Vector dataset created in managed format: {Path}", dataSourcePath);
    }

    public async Task AppendAsync(string dataSourcePath, IAsyncEnumerable<FeatureRecord> features, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        var store = LoadStore(dataSourcePath);

        await foreach (var feature in features.WithCancellation(cancellationToken))
        {
            store.Features.Add(feature);
        }

        SaveStore(dataSourcePath, store);
    }

    public async Task OptimizeAsync(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        EnsureReady();
        ValidatePath(dataSourcePath);

        var store = LoadStore(dataSourcePath);
        SaveStore(dataSourcePath, store);
    }

    public async Task<IReadOnlyList<FeatureRecord>> QueryAsync(
        string dataSourcePath,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        EnsureReady();
        ValidatePath(dataSourcePath);

        var store = LoadStore(dataSourcePath);
        var result = ApplyQuery(store.Features, options, cancellationToken).ToList();
        return result;
    }

    public async Task<int> UpdateAttributesAsync(
        string dataSourcePath,
        IReadOnlyDictionary<string, object?> updates,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        EnsureReady();
        ValidatePath(dataSourcePath);

        if (updates.Count == 0)
        {
            return 0;
        }

        var store = LoadStore(dataSourcePath);
        var matched = ApplyQuery(store.Features, options, cancellationToken).ToList();

        foreach (var feature in matched)
        {
            foreach (var update in updates)
            {
                feature.Attributes[update.Key] = update.Value;
            }
        }

        SaveStore(dataSourcePath, store);
        return matched.Count;
    }

    public async Task<int> DeleteFeaturesAsync(
        string dataSourcePath,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        EnsureReady();
        ValidatePath(dataSourcePath);

        var store = LoadStore(dataSourcePath);
        var toDelete = ApplyQuery(store.Features, options, cancellationToken).ToHashSet();
        if (toDelete.Count == 0)
        {
            return 0;
        }

        store.Features.RemoveAll(x => toDelete.Contains(x));
        SaveStore(dataSourcePath, store);
        return toDelete.Count;
    }

    public async Task<FieldStatistics> CalculateStatisticsAsync(
        string dataSourcePath,
        string fieldName,
        FeatureQueryOptions? options = null,
        string? layerName = null,
        DataSourceKind kind = DataSourceKind.Shapefile,
        CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        EnsureReady();
        ValidatePath(dataSourcePath);

        if (string.IsNullOrWhiteSpace(fieldName))
        {
            throw new ShapefileException("Field name cannot be null or empty.");
        }

        var store = LoadStore(dataSourcePath);
        var matched = ApplyQuery(store.Features, options, cancellationToken).ToList();

        var nullCount = 0;
        var values = new List<double>();
        foreach (var item in matched)
        {
            if (!item.Attributes.TryGetValue(fieldName, out var value) || value is null)
            {
                nullCount++;
                continue;
            }

            if (TryConvertToDouble(value, out var number))
            {
                values.Add(number);
            }
        }

        var numericCount = values.Count;
        var sum = numericCount > 0 ? values.Sum() : null;
        var min = numericCount > 0 ? values.Min() : null;
        var max = numericCount > 0 ? values.Max() : null;
        var average = numericCount > 0 ? values.Average() : null;

        return new FieldStatistics(fieldName, matched.Count, nullCount, numericCount, min, max, sum, average);
    }

    private static IEnumerable<FeatureRecord> ApplyQuery(IEnumerable<FeatureRecord> source, FeatureQueryOptions? options, CancellationToken cancellationToken)
    {
        var query = source;
        if (options?.BoundingBox is not null)
        {
            query = query.Where(x => GetBoundingBox(x.Geometry).Intersects(options.BoundingBox.Value));
        }

        if (options is not null && options.AttributeEquals.Count > 0)
        {
            query = query.Where(feature =>
                options.AttributeEquals.All(condition =>
                    feature.Attributes.TryGetValue(condition.Key, out var value)
                    && Equals(value, condition.Value)));
        }

        if (options?.Take is > 0)
        {
            query = query.Take(options.Take.Value);
        }

        foreach (var item in query)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }

    private static BoundingBox GetBoundingBox(Geometry geometry)
    {
        if (geometry.Coordinates.Count == 0)
        {
            throw new ShapefileException("Geometry has no coordinates.");
        }

        var minX = geometry.Coordinates.Min(x => x.X);
        var minY = geometry.Coordinates.Min(x => x.Y);
        var maxX = geometry.Coordinates.Max(x => x.X);
        var maxY = geometry.Coordinates.Max(x => x.Y);
        return new BoundingBox(minX, minY, maxX, maxY);
    }

    private static bool TryConvertToDouble(object value, out double number)
    {
        switch (value)
        {
            case byte b:
                number = b;
                return true;
            case sbyte sb:
                number = sb;
                return true;
            case short s:
                number = s;
                return true;
            case ushort us:
                number = us;
                return true;
            case int i:
                number = i;
                return true;
            case uint ui:
                number = ui;
                return true;
            case long l:
                number = l;
                return true;
            case ulong ul:
                number = ul;
                return true;
            case float f:
                number = f;
                return true;
            case double d:
                number = d;
                return true;
            case decimal m:
                number = (double)m;
                return true;
            case string text when double.TryParse(text, out var parsed):
                number = parsed;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static StoredDataSet LoadStore(string path)
    {
        if (!File.Exists(path))
        {
            throw new ShapefileException($"Data source not found: {path}");
        }

        var json = File.ReadAllText(path);
        var node = JsonNode.Parse(json)?.AsObject() ?? throw new ShapefileException("Invalid dataset format.");

        var layerName = node["layerName"]?.GetValue<string>() ?? throw new ShapefileException("Missing layerName.");
        var schema = node["schema"]?.Deserialize<ShapefileSchema>(JsonOptions) ?? throw new ShapefileException("Missing schema.");
        var featuresNode = node["features"]?.AsArray() ?? [];
        var features = new List<FeatureRecord>();

        foreach (var item in featuresNode)
        {
            var feature = item?.Deserialize<FeatureRecord>(JsonOptions);
            if (feature is not null)
            {
                features.Add(feature);
            }
        }

        return new StoredDataSet
        {
            LayerName = layerName,
            Schema = schema,
            Features = features
        };
    }

    private static void SaveStore(string path, StoredDataSet store)
    {
        var json = JsonSerializer.Serialize(new
        {
            layerName = store.LayerName,
            schema = store.Schema,
            features = store.Features
        }, JsonOptions);

        File.WriteAllText(path, json);
    }

    private static void ValidatePath(string dataSourcePath)
    {
        if (string.IsNullOrWhiteSpace(dataSourcePath))
        {
            throw new ShapefileException("Data source path cannot be null or empty.");
        }
    }

    private void EnsureReady()
    {
        if (!_runtimeInitializer.IsInitialized)
        {
            throw new ShapefileException("Runtime is not initialized. Call IGdalRuntimeInitializer.Initialize first.");
        }
    }

    private sealed class StoredDataSet
    {
        public required string LayerName { get; init; }
        public required ShapefileSchema Schema { get; init; }
        public required List<FeatureRecord> Features { get; init; }
    }
}
