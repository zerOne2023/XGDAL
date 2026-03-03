# XGdal Shapefile / GDB Library (Pure Managed Skeleton)

这是一个**从零开始**实现的矢量数据类库骨架：

- 参考 GDAL 的分层设计思路（Reader/Writer/Repository/Geometry Service）。
- **不依赖 GDAL、NetTopologySuite 等第三方 GIS 库**。
- 提供可扩展的领域模型（`Geometry`、`FeatureRecord`、`ShapefileSchema`）。
- 当前以托管 JSON 存储格式实现读写流程，便于先完成架构和业务闭环。

## 当前能力（ArcGIS 常见工作流对应）

- 数据读写：`WriteAsync / ReadAsync / AppendAsync / GetInfo / ExistsAsync / DeleteAsync`。
- 属性查询（Select By Attributes）：`QueryAsync` + `FeatureQueryOptions.AttributeEquals`。
- 空间查询（Select By Location 简化版）：`QueryAsync` + `FeatureQueryOptions.BoundingBox`。
- 批量字段更新（Calculate Field）：`UpdateAttributesAsync`。
- 要素删除（Delete Features）：`DeleteFeaturesAsync`。
- 统计分析（Summary Statistics）：`CalculateStatisticsAsync`。
- 基础几何分析：`Buffer / Area / Length / Distance / Intersects / Contains / Within`。

## 设计目标

后续可在不破坏对外接口的前提下，逐步替换为真正的 SHP/DBF/SHX 二进制实现（或自研 GDB 解析模块）。

## 示例

```csharp
var query = new FeatureQueryOptions
{
    BoundingBox = new BoundingBox(116.0, 39.0, 117.0, 40.0),
    AttributeEquals = { ["Category"] = "City" },
    Take = 100
};

var selected = await facade.QueryAsync("D:/data/cities.json", query);
var updated = await facade.UpdateAttributesAsync(
    "D:/data/cities.json",
    new Dictionary<string, object?> { ["Reviewed"] = true },
    query);

var stats = await facade.CalculateStatisticsAsync("D:/data/cities.json", "Population", query);
```
