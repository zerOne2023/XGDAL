using XGdal.Shapefile.Domain;

namespace XGdal.Shapefile.Factories;

public static class FeatureRecordFactory
{
    public static FeatureRecord Create(Geometry geometry, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        var record = new FeatureRecord { Geometry = geometry };

        if (attributes is null)
        {
            return record;
        }

        foreach (var attribute in attributes)
        {
            record.Attributes[attribute.Key] = attribute.Value;
        }

        return record;
    }
}
