using BAMCIS.GeoJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace GIS.VU.API
{
    public class GeoJsonFileReader
    {
        public List<RouteFeature> Read(string path)
        {
            var routeFeatures = ReadAndParseFeatures(path);

            foreach (var routeFeature in routeFeatures)
            {
                foreach (var testRouteFeature in routeFeatures)
                {
                    if(AreNeighbours(routeFeature, testRouteFeature))
                    {
                        routeFeature.Neighbours.Add(testRouteFeature);
                    }
                }
            }

            return routeFeatures;
        }

        private bool AreNeighbours(RouteFeature routeFeature, RouteFeature testRouteFeature)
        {
            if (routeFeature == testRouteFeature)
                return false;

            double distanceDiff = 10f / 100000;

            var startPoint1 = ((LineString)routeFeature.Feature.Geometry).Coordinates.First();
            var endPoint1 = ((LineString)routeFeature.Feature.Geometry).Coordinates.Last();

            var startPoint2 = ((LineString)testRouteFeature.Feature.Geometry).Coordinates.First();
            var endPoint2 = ((LineString)testRouteFeature.Feature.Geometry).Coordinates.Last();

            var distance1 = Math.Sqrt(Math.Pow(startPoint1.Latitude - startPoint2.Latitude, 2) + Math.Pow(startPoint1.Longitude - startPoint2.Longitude, 2));
            var distance2 = Math.Sqrt(Math.Pow(startPoint1.Latitude - endPoint2.Latitude, 2) + Math.Pow(startPoint1.Longitude - endPoint2.Longitude, 2));

            var distance3 = Math.Sqrt(Math.Pow(endPoint1.Latitude - startPoint2.Latitude, 2) + Math.Pow(endPoint1.Longitude - startPoint2.Longitude, 2));
            var distance4 = Math.Sqrt(Math.Pow(endPoint1.Latitude - endPoint2.Latitude, 2) + Math.Pow(endPoint1.Longitude - endPoint2.Longitude, 2));

            if (distance1 < distanceDiff || distance2 < distanceDiff || distance3 < distanceDiff || distance4 < distanceDiff)
                return true;

            return false;
        }

        private List<RouteFeature> ReadAndParseFeatures(string path)
        {
            var routeFeatures = new List<RouteFeature>();

            var features = FeatureCollection.FromJson(File.ReadAllText(path)).Features;

            foreach (var f in features)
            {
                routeFeatures.Add(new RouteFeature()
                {
                    Feature = f,
                    Length = CalculateLength(((LineString)f.Geometry).Coordinates)
                });
            }

            return routeFeatures;
        }

        private double CalculateLength(IEnumerable<Position> coordinates)
        {
            var initial = coordinates.First();
            double length = 0;

            foreach (var p in coordinates.Skip(1))
            {
                length += Math.Sqrt(Math.Pow(initial.Latitude - p.Latitude, 2) + Math.Pow(initial.Longitude - p.Longitude, 2));
                initial = p;
            }

            return length * 100 * 1000;
        }
    }
}
