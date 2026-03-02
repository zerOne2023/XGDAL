namespace XGdal.Shapefile.Diagnostics;

public class ShapefileException : Exception
{
    public ShapefileException(string message) : base(message)
    {
    }

    public ShapefileException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
