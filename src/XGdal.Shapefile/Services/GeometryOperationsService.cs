using XGdal.Shapefile.Abstractions;
using XGdal.Shapefile.Diagnostics;
using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Services;

public sealed class GeometryOperationsService : IGeometryOperationsService
{
    public Geometry Buffer(Geometry geometry, double distance)
    {
        var source = EnsureGeometry(geometry);
        if (source.Kind != GeometryKind.Point || source.Coordinates.Count != 1)
        {
            throw new ShapefileException("Current Buffer implementation only supports point geometry.");
        }

        if (distance <= 0)
        {
            throw new ShapefileException("Buffer distance must be positive.");
        }

        var center = source.Coordinates[0];
        var vertices = new List<Coordinate>();
        const int segments = 32;
        for (var i = 0; i <= segments; i++)
        {
            var angle = 2 * Math.PI * i / segments;
            vertices.Add(new Coordinate(
                center.X + distance * Math.Cos(angle),
                center.Y + distance * Math.Sin(angle)));
        }

        return Geometry.Polygon(vertices);
    }

    public Geometry Union(IEnumerable<Geometry> geometries)
        => geometries?.FirstOrDefault() ?? throw new ShapefileException("Geometries collection cannot be null or empty.");

    public Geometry Intersection(Geometry left, Geometry right)
    {
        if (!Intersects(left, right))
        {
            return Geometry.Point(double.NaN, double.NaN);
        }

        return left;
    }

    public Geometry Difference(Geometry left, Geometry right) => EnsureGeometry(left);

    public Geometry SymmetricDifference(Geometry left, Geometry right)
        => Intersects(left, right) ? Difference(left, right) : Union([left, right]);

    public Geometry Simplify(Geometry geometry, double tolerance, bool preserveTopology = true)
    {
        if (tolerance < 0)
        {
            throw new ShapefileException("Tolerance cannot be negative.");
        }

        return EnsureGeometry(geometry);
    }

    public double Area(Geometry geometry)
    {
        var checkedGeometry = EnsureGeometry(geometry);
        if (checkedGeometry.Kind != GeometryKind.Polygon || checkedGeometry.Coordinates.Count < 3)
        {
            return 0;
        }

        var area = 0d;
        for (var i = 0; i < checkedGeometry.Coordinates.Count; i++)
        {
            var p1 = checkedGeometry.Coordinates[i];
            var p2 = checkedGeometry.Coordinates[(i + 1) % checkedGeometry.Coordinates.Count];
            area += (p1.X * p2.Y) - (p2.X * p1.Y);
        }

        return Math.Abs(area) / 2d;
    }

    public double Length(Geometry geometry)
    {
        var checkedGeometry = EnsureGeometry(geometry);
        if (checkedGeometry.Coordinates.Count < 2)
        {
            return 0;
        }

        var length = 0d;
        for (var i = 1; i < checkedGeometry.Coordinates.Count; i++)
        {
            length += Distance(checkedGeometry.Coordinates[i - 1], checkedGeometry.Coordinates[i]);
        }

        return length;
    }

    public bool Intersects(Geometry left, Geometry right)
    {
        var leftBox = GetBoundingBox(EnsureGeometry(left));
        var rightBox = GetBoundingBox(EnsureGeometry(right));

        return leftBox.MinX <= rightBox.MaxX
               && leftBox.MaxX >= rightBox.MinX
               && leftBox.MinY <= rightBox.MaxY
               && leftBox.MaxY >= rightBox.MinY;
    }

    public bool Contains(Geometry container, Geometry target)
    {
        var containerBox = GetBoundingBox(EnsureGeometry(container));
        var targetBox = GetBoundingBox(EnsureGeometry(target));

        return containerBox.MinX <= targetBox.MinX
               && containerBox.MaxX >= targetBox.MaxX
               && containerBox.MinY <= targetBox.MinY
               && containerBox.MaxY >= targetBox.MaxY;
    }

    public bool Within(Geometry target, Geometry container) => Contains(container, target);

    public double Distance(Geometry left, Geometry right)
        => Distance(GetRepresentativePoint(EnsureGeometry(left)), GetRepresentativePoint(EnsureGeometry(right)));

    private static double Distance(Coordinate left, Coordinate right)
        => Math.Sqrt(Math.Pow(left.X - right.X, 2) + Math.Pow(left.Y - right.Y, 2));

    private static Coordinate GetRepresentativePoint(Geometry geometry) => geometry.Coordinates.First();

    private static (double MinX, double MaxX, double MinY, double MaxY) GetBoundingBox(Geometry geometry)
    {
        if (geometry.Coordinates.Count == 0)
        {
            throw new ShapefileException("Geometry has no coordinates.");
        }

        var xs = geometry.Coordinates.Select(x => x.X);
        var ys = geometry.Coordinates.Select(x => x.Y);
        return (xs.Min(), xs.Max(), ys.Min(), ys.Max());
    }

    private static Geometry EnsureGeometry(Geometry geometry)
        => geometry ?? throw new ShapefileException("Geometry cannot be null.");
}
