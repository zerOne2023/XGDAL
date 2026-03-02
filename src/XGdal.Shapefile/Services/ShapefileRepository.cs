using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using OSGeo.OGR;
using OSGeo.OSR;
using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Configuration;
using XGdal.Shapefile.Diagnostics;
using XGdal.Shapefile.Domain;
using XGdal.Shapefile.Interop;

namespace XGdal.Shapefile.Services;

public sealed class ShapefileRepository : IShapefileRepository
{
    private readonly IGdalRuntimeInitializer _runtimeInitializer;
    private readonly ILogger<ShapefileRepository> _logger;

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
        return kind == DataSourceKind.Shapefile
            ? File.Exists(dataSourcePath)
            : Directory.Exists(dataSourcePath) || File.Exists(dataSourcePath);
    }

    public async Task DeleteAsync(string dataSourcePath, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePath(dataSourcePath);

        if (kind != DataSourceKind.Shapefile)
        {
            if (Directory.Exists(dataSourcePath))
            {
                Directory.Delete(dataSourcePath, true);
            }

            return;
        }

        var baseName = Path.Combine(Path.GetDirectoryName(dataSourcePath) ?? string.Empty, Path.GetFileNameWithoutExtension(dataSourcePath));
        var sidecars = new[] { ".shp", ".shx", ".dbf", ".prj", ".cpg", ".qix" };

        foreach (var ext in sidecars)
        {
            var file = baseName + ext;
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    public ShapefileDatasetInfo GetInfo(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        using var ds = Ogr.Open(dataSourcePath, 0) ?? throw new ShapefileException($"Cannot open datasource: {dataSourcePath}");
        using var layer = ResolveLayer(ds, layerName) ?? throw new ShapefileException("Requested layer was not found.");

        var defn = layer.GetLayerDefn();
        var fields = new List<FieldDefinition>();
        for (var i = 0; i < defn.GetFieldCount(); i++)
        {
            var fd = defn.GetFieldDefn(i);
            fields.Add(new FieldDefinition(fd.GetName(), OgrTypeMapper.ToDomain(fd.GetFieldType()), fd.GetWidth(), fd.GetPrecision(), true));
        }

        int? srid = null;
        var srs = layer.GetSpatialRef();
        if (srs is not null && srs.AutoIdentifyEPSG() == 0)
        {
            var authCode = srs.GetAuthorityCode(null);
            if (int.TryParse(authCode, out var parsed))
            {
                srid = parsed;
            }
        }

        return new ShapefileDatasetInfo(dataSourcePath, layer.GetName(), kind, OgrTypeMapper.ToDomain(layer.GetGeomType()), srid, (int)layer.GetFeatureCount(1), fields);
    }

    public async IAsyncEnumerable<FeatureRecord> ReadAsync(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        using var ds = Ogr.Open(dataSourcePath, 0) ?? throw new ShapefileException($"Cannot open datasource: {dataSourcePath}");
        using var layer = ResolveLayer(ds, layerName) ?? throw new ShapefileException("Requested layer was not found.");

        layer.ResetReading();
        Feature? feature;
        while ((feature = layer.GetNextFeature()) != null)
        {
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            using (feature)
            {
                var record = new FeatureRecord
                {
                    Geometry = WktToGeometry(feature.GetGeometryRef()?.ExportToWkt() ?? "POINT EMPTY")
                };

                for (var i = 0; i < feature.GetFieldCount(); i++)
                {
                    var fieldName = feature.GetFieldDefnRef(i).GetName();
                    record.Attributes[fieldName] = ReadFieldValue(feature, i);
                }

                yield return record;
            }
        }
    }

    public async Task WriteAsync(string dataSourcePath, ShapefileSchema schema, IAsyncEnumerable<FeatureRecord> features, VectorWriteOptions? options = null, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        options ??= new VectorWriteOptions();
        var kind = options.DataSourceKind;

        if (options.OverwriteExisting)
        {
            await DeleteAsync(dataSourcePath, kind, cancellationToken);
        }

        var driver = ResolveDriver(kind);
        using var ds = driver.CreateDataSource(dataSourcePath, options.DatasetCreationOptions.ToArray())
            ?? throw new ShapefileException("Failed to create data source.");

        using var srs = schema.Srid.HasValue ? new SpatialReference(string.Empty) : null;
        if (srs is not null)
        {
            srs.ImportFromEPSG(schema.Srid.Value);
        }

        var layerName = options.LayerName ?? schema.LayerName ?? Path.GetFileNameWithoutExtension(dataSourcePath);
        var layerOptions = BuildLayerCreationOptions(options, kind);
        using var layer = ds.CreateLayer(layerName, srs, OgrTypeMapper.ToOgr(schema.GeometryKind), layerOptions)
            ?? throw new ShapefileException("Failed to create layer.");

        foreach (var field in schema.Fields)
        {
            using var fieldDef = new FieldDefn(field.Name, OgrTypeMapper.ToOgr(field.Type));
            if (field.Width > 0) fieldDef.SetWidth(field.Width);
            if (field.Precision > 0) fieldDef.SetPrecision(field.Precision);

            if (layer.CreateField(fieldDef, 1) != 0)
            {
                throw new ShapefileException($"Failed to create field '{field.Name}'.");
            }
        }

        await WriteFeaturesAsync(layer, features, cancellationToken);

        if (options.CreateSpatialIndex && kind == DataSourceKind.Shapefile)
        {
            layer.CreateSpatialIndex(0);
        }

        _logger.LogInformation("Vector dataset created: {Path}, kind: {Kind}", dataSourcePath, kind);
    }

    public async Task AppendAsync(string dataSourcePath, IAsyncEnumerable<FeatureRecord> features, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        EnsureReady();
        ValidatePath(dataSourcePath);

        using var ds = Ogr.Open(dataSourcePath, 1) ?? throw new ShapefileException($"Cannot open datasource: {dataSourcePath}");
        using var layer = ResolveLayer(ds, layerName) ?? throw new ShapefileException("Requested layer was not found.");

        await WriteFeaturesAsync(layer, features, cancellationToken);
    }

    public async Task OptimizeAsync(string dataSourcePath, string? layerName = null, DataSourceKind kind = DataSourceKind.Shapefile, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        EnsureReady();
        ValidatePath(dataSourcePath);

        using var ds = Ogr.Open(dataSourcePath, 1) ?? throw new ShapefileException($"Cannot open datasource: {dataSourcePath}");
        using var layer = ResolveLayer(ds, layerName) ?? throw new ShapefileException("Requested layer was not found.");

        layer.SyncToDisk();
        if (kind == DataSourceKind.Shapefile)
        {
            layer.CreateSpatialIndex(0);
        }
    }

    private static async Task WriteFeaturesAsync(Layer layer, IAsyncEnumerable<FeatureRecord> features, CancellationToken cancellationToken)
    {
        var defn = layer.GetLayerDefn();

        await foreach (var item in features.WithCancellation(cancellationToken))
        {
            using var feature = new Feature(defn);

            var wkt = item.Geometry.AsText();
            using var ogrGeometry = OSGeo.OGR.Geometry.CreateFromWkt(wkt);
            feature.SetGeometry(ogrGeometry);

            foreach (var attribute in item.Attributes)
            {
                var fieldIndex = defn.GetFieldIndex(attribute.Key);
                if (fieldIndex < 0)
                {
                    continue;
                }

                if (attribute.Value is null)
                {
                    feature.UnsetField(fieldIndex);
                    continue;
                }

                SetFieldValue(feature, fieldIndex, attribute.Value);
            }

            if (layer.CreateFeature(feature) != 0)
            {
                throw new ShapefileException("Failed to create feature.");
            }
        }
    }

    private static object? ReadFieldValue(Feature feature, int fieldIndex)
    {
        if (!feature.IsFieldSet(fieldIndex))
        {
            return null;
        }

        var type = feature.GetFieldDefnRef(fieldIndex).GetFieldType();
        return type switch
        {
            OSGeo.OGR.FieldType.OFTInteger => feature.GetFieldAsInteger(fieldIndex),
            OSGeo.OGR.FieldType.OFTInteger64 => feature.GetFieldAsInteger64(fieldIndex),
            OSGeo.OGR.FieldType.OFTReal => feature.GetFieldAsDouble(fieldIndex),
            OSGeo.OGR.FieldType.OFTDate or OSGeo.OGR.FieldType.OFTDateTime => feature.GetFieldAsString(fieldIndex),
            _ => feature.GetFieldAsString(fieldIndex)
        };
    }

    private static void SetFieldValue(Feature feature, int fieldIndex, object value)
    {
        switch (value)
        {
            case int v:
                feature.SetField(fieldIndex, v);
                break;
            case long v:
                feature.SetField(fieldIndex, v);
                break;
            case double v:
                feature.SetField(fieldIndex, v);
                break;
            case float v:
                feature.SetField(fieldIndex, (double)v);
                break;
            case bool v:
                feature.SetField(fieldIndex, v ? 1 : 0);
                break;
            default:
                feature.SetField(fieldIndex, value.ToString() ?? string.Empty);
                break;
        }
    }

    private static string[] BuildLayerCreationOptions(VectorWriteOptions options, DataSourceKind kind)
    {
        var result = new List<string>(options.LayerCreationOptions);

        if (kind == DataSourceKind.Shapefile
            && !string.IsNullOrWhiteSpace(options.Encoding)
            && result.All(x => !x.StartsWith("ENCODING=", StringComparison.OrdinalIgnoreCase)))
        {
            result.Add($"ENCODING={options.Encoding}");
        }

        return result.ToArray();
    }

    private static Driver ResolveDriver(DataSourceKind kind)
    {
        var driverName = kind switch
        {
            DataSourceKind.Shapefile => "ESRI Shapefile",
            DataSourceKind.OpenFileGdb => "OpenFileGDB",
            DataSourceKind.FileGdb => "FileGDB",
            _ => "ESRI Shapefile"
        };

        return Ogr.GetDriverByName(driverName) ?? throw new ShapefileException($"Driver '{driverName}' is unavailable.");
    }

    private static Layer? ResolveLayer(DataSource ds, string? layerName)
    {
        if (!string.IsNullOrWhiteSpace(layerName))
        {
            return ds.GetLayerByName(layerName);
        }

        return ds.GetLayerByIndex(0);
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
            throw new ShapefileException("GDAL runtime is not initialized. Call IGdalRuntimeInitializer.Initialize first.");
        }
    }

    private static Geometry WktToGeometry(string wkt)
    {
        var reader = new NetTopologySuite.IO.WKTReader();
        return reader.Read(wkt);
    }
}
