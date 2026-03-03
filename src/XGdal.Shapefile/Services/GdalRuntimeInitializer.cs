using Microsoft.Extensions.Logging;
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

            _logger.LogInformation("Runtime initialized in pure managed mode (no GDAL dependency).");
            IsInitialized = true;
        }
    }
}
