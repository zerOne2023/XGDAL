using OSGeo.OGR;
using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Interop;

internal static class OgrTypeMapper
{
    public static wkbGeometryType ToOgr(GeometryKind kind) => kind switch
    {
        GeometryKind.Point => wkbGeometryType.wkbPoint,
        GeometryKind.LineString => wkbGeometryType.wkbLineString,
        GeometryKind.Polygon => wkbGeometryType.wkbPolygon,
        GeometryKind.MultiPoint => wkbGeometryType.wkbMultiPoint,
        GeometryKind.MultiLineString => wkbGeometryType.wkbMultiLineString,
        GeometryKind.MultiPolygon => wkbGeometryType.wkbMultiPolygon,
        _ => wkbGeometryType.wkbUnknown
    };

    public static GeometryKind ToDomain(wkbGeometryType geometryType)
    {
        var flattened = wkbGeometryType.wkbFlatten(geometryType);
        return flattened switch
        {
            wkbGeometryType.wkbPoint => GeometryKind.Point,
            wkbGeometryType.wkbLineString => GeometryKind.LineString,
            wkbGeometryType.wkbPolygon => GeometryKind.Polygon,
            wkbGeometryType.wkbMultiPoint => GeometryKind.MultiPoint,
            wkbGeometryType.wkbMultiLineString => GeometryKind.MultiLineString,
            wkbGeometryType.wkbMultiPolygon => GeometryKind.MultiPolygon,
            _ => GeometryKind.Unknown
        };
    }

    public static Domain.FieldType ToDomain(OSGeo.OGR.FieldType ogrType) => ogrType switch
    {
        OSGeo.OGR.FieldType.OFTInteger => Domain.FieldType.Integer,
        OSGeo.OGR.FieldType.OFTInteger64 => Domain.FieldType.Long,
        OSGeo.OGR.FieldType.OFTReal => Domain.FieldType.Real,
        OSGeo.OGR.FieldType.OFTDate => Domain.FieldType.Date,
        OSGeo.OGR.FieldType.OFTDateTime => Domain.FieldType.DateTime,
        OSGeo.OGR.FieldType.OFTString => Domain.FieldType.String,
        _ => Domain.FieldType.String
    };

    public static OSGeo.OGR.FieldType ToOgr(Domain.FieldType domainType) => domainType switch
    {
        Domain.FieldType.Integer => OSGeo.OGR.FieldType.OFTInteger,
        Domain.FieldType.Long => OSGeo.OGR.FieldType.OFTInteger64,
        Domain.FieldType.Real => OSGeo.OGR.FieldType.OFTReal,
        Domain.FieldType.Date => OSGeo.OGR.FieldType.OFTDate,
        Domain.FieldType.DateTime => OSGeo.OGR.FieldType.OFTDateTime,
        Domain.FieldType.Logical => OSGeo.OGR.FieldType.OFTInteger,
        _ => OSGeo.OGR.FieldType.OFTString
    };
}
