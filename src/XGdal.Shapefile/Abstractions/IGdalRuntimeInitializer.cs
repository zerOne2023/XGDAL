using XGdal.Shapefile.Configuration;

namespace XGdal.Shapefile.Abstractions;

public interface IGdalRuntimeInitializer
{
    bool IsInitialized { get; }
    void Initialize(GdalRuntimeOptions options);
}
