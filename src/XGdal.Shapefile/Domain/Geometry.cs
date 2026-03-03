namespace XGdal.Shapefile.Domain;

public sealed class Geometry
{
    public required GeometryKind Kind { get; init; }
    public required IReadOnlyList<Coordinate> Coordinates { get; init; }

    public static Geometry Point(double x, double y) => new()
    {
        Kind = GeometryKind.Point,
        Coordinates = [new Coordinate(x, y)]
    };

    public static Geometry LineString(IEnumerable<Coordinate> coordinates) => new()
    {
        Kind = GeometryKind.LineString,
        Coordinates = coordinates.ToArray()
    };

    public static Geometry Polygon(IEnumerable<Coordinate> ringCoordinates) => new()
    {
        Kind = GeometryKind.Polygon,
        Coordinates = ringCoordinates.ToArray()
    };
}
