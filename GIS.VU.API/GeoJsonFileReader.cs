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
        public const double DistanceDiff = 10f / 100000;

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

            var startPoint1 = ((LineString)routeFeature.Feature.Geometry).Coordinates.First();
            var endPoint1 = ((LineString)routeFeature.Feature.Geometry).Coordinates.Last();

            var startPoint2 = ((LineString)testRouteFeature.Feature.Geometry).Coordinates.First();
            var endPoint2 = ((LineString)testRouteFeature.Feature.Geometry).Coordinates.Last();

            var distance1 = GetDistance(startPoint1, startPoint2); 
            var distance2 = GetDistance(startPoint1, endPoint2);  

            var distance3 = GetDistance(endPoint1, startPoint2);  
            var distance4 = GetDistance(endPoint1, endPoint2);  

            if (distance1 < DistanceDiff || distance2 < DistanceDiff || distance3 < DistanceDiff || distance4 < DistanceDiff)
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

        private double GetDistance(Position a, Position b)
        {
            return Math.Sqrt(Math.Pow(a.Latitude - b.Latitude, 2) + Math.Pow(a.Longitude - b.Longitude, 2));
        }
    }
}
