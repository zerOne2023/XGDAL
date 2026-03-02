using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;
using NetTopologySuite.Simplify;
using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Diagnostics;

namespace XGdal.Shapefile.Services;

public sealed class GeometryOperationsService : IGeometryOperationsService
{
    public Geometry Buffer(Geometry geometry, double distance)
        => EnsureGeometry(geometry).Buffer(distance);

    public Geometry Union(IEnumerable<Geometry> geometries)
    {
        var geometryList = geometries?.Where(x => x is not null).ToList() ?? throw new ShapefileException("Geometries collection cannot be null.");
        if (geometryList.Count == 0)
        {
            throw new ShapefileException("Geometries collection cannot be empty.");
        }

        return UnaryUnionOp.Union(geometryList);
    }

    public Geometry Intersection(Geometry left, Geometry right)
        => EnsureGeometry(left).Intersection(EnsureGeometry(right));

    public Geometry Difference(Geometry left, Geometry right)
        => EnsureGeometry(left).Difference(EnsureGeometry(right));

    public Geometry SymmetricDifference(Geometry left, Geometry right)
        => EnsureGeometry(left).SymmetricDifference(EnsureGeometry(right));

    public Geometry Simplify(Geometry geometry, double tolerance, bool preserveTopology = true)
    {
        var checkedGeometry = EnsureGeometry(geometry);
        if (tolerance < 0)
        {
            throw new ShapefileException("Tolerance cannot be negative.");
        }

        return preserveTopology
            ? TopologyPreservingSimplifier.Simplify(checkedGeometry, tolerance)
            : DouglasPeuckerSimplifier.Simplify(checkedGeometry, tolerance);
    }

    public double Area(Geometry geometry) => EnsureGeometry(geometry).Area;

    public double Length(Geometry geometry) => EnsureGeometry(geometry).Length;

    public bool Intersects(Geometry left, Geometry right)
        => EnsureGeometry(left).Intersects(EnsureGeometry(right));

    public bool Contains(Geometry container, Geometry target)
        => EnsureGeometry(container).Contains(EnsureGeometry(target));

    public bool Within(Geometry target, Geometry container)
        => EnsureGeometry(target).Within(EnsureGeometry(container));

    public double Distance(Geometry left, Geometry right)
        => EnsureGeometry(left).Distance(EnsureGeometry(right));

    private static Geometry EnsureGeometry(Geometry geometry)
        => geometry ?? throw new ShapefileException("Geometry cannot be null.");
}
