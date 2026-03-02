using Microsoft.Extensions.Logging;
using OSGeo.GDAL;
using OSGeo.OGR;
using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Configuration;

namespace XGdal.Shapefile.Services;

public sealed class GdalRuntimeInitializer : IGdalRuntimeInitializer
{
    private readonly ILogger<GdalRuntimeInitializer> _logger;
    private readonly object _syncRoot = new();

    public GdalRuntimeInitializer(ILogger<GdalRuntimeInitializer> logger)
    {
        _logger = logger;
    }

    public bool IsInitialized { get; private set; }

    public void Initialize(GdalRuntimeOptions options)
    {
        if (IsInitialized)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (IsInitialized)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(options.GdalDataPath))
            {
                Gdal.SetConfigOption("GDAL_DATA", options.GdalDataPath);
            }

            if (!string.IsNullOrWhiteSpace(options.ProjLibPath))
            {
                Gdal.SetConfigOption("PROJ_LIB", options.ProjLibPath);
            }

            foreach (var item in options.ConfigOptions)
            {
                Gdal.SetConfigOption(item.Key, item.Value);
            }

            if (options.RegisterAllDrivers)
            {
                Gdal.AllRegister();
                Ogr.RegisterAll();
            }

            _logger.LogInformation("GDAL runtime initialized.");
            IsInitialized = true;
        }
    }
}
