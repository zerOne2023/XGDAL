# XGdal Shapefile / GDB Library (for C#/WPF)

这是一个面向大型软件架构设计的矢量数据类库骨架，基于 GDAL/OGR + NetTopologySuite，强调：

- 分层清晰（Abstractions / Domain / Services / Interop / Extensions）。
- 接口丰富（读、写、追加、优化、元数据查询、存在性检测、删除、几何运算）。
- 便于 WPF + DI 调用（`AddXGdalShapefile` 扩展）。
- 支持可配置运行时初始化（`GdalRuntimeOptions`）。
- 支持 **SHP** 与 **GDB（OpenFileGDB / FileGDB）** 驱动。

## 关键能力

- `DataSourceKind`：在 SHP、OpenFileGDB、FileGDB 之间切换。
- `VectorWriteOptions`：同时支持图层参数和数据集参数。
- `GetInfo/ReadAsync/AppendAsync`：支持传入 `layerName`，适配 GDB 多图层（Feature Class）场景。
- `IGeometryOperationsService`：支持 Buffer、Union、Intersection、Difference、Simplify、Contains/Within/Intersects、Distance 等几何计算。

## WPF 调用示例（写入 GDB + 几何运算）

```csharp
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using XGdal.Shapefile;
using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Configuration;
using XGdal.Shapefile.Domain;
using XGdal.Shapefile.Extensions;

var services = new ServiceCollection();
services.AddLogging();
services.AddXGdalShapefile();

using var provider = services.BuildServiceProvider();
var facade = new ShapefileLibraryFacade(
    provider.GetRequiredService<IGdalRuntimeInitializer>(),
    provider.GetRequiredService<IShapefileRepository>(),
    provider.GetRequiredService<IGeometryOperationsService>());

facade.Initialize(new GdalRuntimeOptions
{
    GdalDataPath = @"C:\gdal-data",
    ProjLibPath = @"C:\projlib"
});

var schema = new ShapefileSchema
{
    GeometryKind = GeometryKind.Point,
    Srid = 4326,
    LayerName = "cities"
};
schema.Fields.Add(new FieldDefinition("Name", FieldType.String, 50));

async IAsyncEnumerable<FeatureRecord> BuildFeatures()
{
    yield return new FeatureRecord
    {
        Geometry = new Point(116.39, 39.90),
        Attributes = { ["Name"] = "Beijing" }
    };
}

await facade.WriteAsync(
    @"D:\data\cities.gdb",
    schema,
    BuildFeatures(),
    new VectorWriteOptions
    {
        DataSourceKind = DataSourceKind.OpenFileGdb,
        LayerName = "cities"
    });

var buffered = facade.Buffer(new Point(116.39, 39.90), 0.1);
var area = facade.Area(buffered);
```
