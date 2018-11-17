using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BAMCIS.GeoJSON;
using DTOs;
using GISFunctions;

namespace ReadMyGIS
{
    public class GeoJsonFileReader
    {
        public List<RouteFeature> Read(string path)
        {
            var routeFeatures = ReadAndParseFeatures(path);

            //Debug.WriteLine("Started import");
            int i = 0;
            foreach (var routeFeature in routeFeatures)
            {
                i++;
                //Debug.WriteLine(i);

                foreach (var testRouteFeature in routeFeatures)
                {

                    if (AreNeighbours(routeFeature, testRouteFeature))
                    {
                        routeFeature.Neighbours.Add(testRouteFeature);
                    }
                }
            }

            //Debug.WriteLine("Finished import");


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

            if (Helpers.AreClose(startPoint1, startPoint2))
                return true;

            if (Helpers.AreClose(startPoint1, endPoint2))
                return true;

            if (Helpers.AreClose(endPoint1, startPoint2))
                return true;

            if (Helpers.AreClose(endPoint1, endPoint2))
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
                    //Length = CalculateLength(((LineString)f.Geometry).Coordinates)
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


        public static double CalculateLength(IEnumerable<double[]> coordinates)
        {
            var initial = coordinates.First();
            double length = 0;

            foreach (var p in coordinates.Skip(1))
            {
                length += Math.Sqrt(Math.Pow(initial[0] - p[0], 2) + Math.Pow(initial[1] - p[1], 2));
                initial = p;
            }

            return length * 100 * 1000;
        }

        private static double GetDistance(Position a, Position b)
        {
            return Math.Sqrt(Math.Pow(a.Latitude - b.Latitude, 2) + Math.Pow(a.Longitude - b.Longitude, 2));
        }
    }
}
