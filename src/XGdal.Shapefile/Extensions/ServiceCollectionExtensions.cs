using Microsoft.Extensions.DependencyInjection;
using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Services;

namespace XGdal.Shapefile.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddXGdalShapefile(this IServiceCollection services)
    {
        services.AddSingleton<IGdalRuntimeInitializer, GdalRuntimeInitializer>();
        services.AddScoped<IShapefileRepository, ShapefileRepository>();
        services.AddSingleton<IGeometryOperationsService, GeometryOperationsService>();
        return services;
    }
}
