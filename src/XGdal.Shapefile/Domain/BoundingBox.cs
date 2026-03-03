namespace XGdal.Shapefile.Domain;

public readonly record struct BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
    public bool Intersects(BoundingBox other)
        => MinX <= other.MaxX && MaxX >= other.MinX && MinY <= other.MaxY && MaxY >= other.MinY;

    public bool Contains(Coordinate coordinate)
        => coordinate.X >= MinX && coordinate.X <= MaxX && coordinate.Y >= MinY && coordinate.Y <= MaxY;
}
