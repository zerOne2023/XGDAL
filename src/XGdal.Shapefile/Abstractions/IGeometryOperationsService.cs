using NetTopologySuite.Geometries;

namespace XGdal.Shapefile.Abstractions;

public interface IGeometryOperationsService
{
    Geometry Buffer(Geometry geometry, double distance);
    Geometry Union(IEnumerable<Geometry> geometries);
    Geometry Intersection(Geometry left, Geometry right);
    Geometry Difference(Geometry left, Geometry right);
    Geometry SymmetricDifference(Geometry left, Geometry right);
    Geometry Simplify(Geometry geometry, double tolerance, bool preserveTopology = true);
    double Area(Geometry geometry);
    double Length(Geometry geometry);
    bool Intersects(Geometry left, Geometry right);
    bool Contains(Geometry container, Geometry target);
    bool Within(Geometry target, Geometry container);
    double Distance(Geometry left, Geometry right);
}
