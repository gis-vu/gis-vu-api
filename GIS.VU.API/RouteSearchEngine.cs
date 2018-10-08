using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BAMCIS.GeoJSON;
using GIS.VU.API.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GIS.VU.API
{
    public class RouteSearchEngine
    {
        private List<RouteFeature> routeFeatures;

        public RouteSearchEngine(GeoJsonFileReader fileReader, string path)
        {
            routeFeatures = fileReader.Read(path);
        }

        public RouteSearchResponse FindRoute(RouteSearchRequest request)
        {
            var startFeature = FindClosetFeature(request.Start);
            var endFeature = FindClosetFeature(request.End);


            Graph g = new Graph(routeFeatures);


            var path = g.shortest_path(startFeature, endFeature);

            return new RouteSearchResponse()
            {
                Type = "LineString",
                Coordinates = path.SelectMany(x => ((LineString)x.Feature.Geometry).Coordinates.Select(y => new[] { y.Longitude, y.Latitude }).ToArray()).ToArray()
            };
        }

        private RouteFeature FindClosetFeature(Coordinate coordinate)
        {
            var closet = routeFeatures.First();
            double dist = CalcualteDistanceToFeature(closet, coordinate);

            foreach (var f in routeFeatures.Skip(1))
            {
                var newDistance = CalcualteDistanceToFeature(f, coordinate);

                if (newDistance < dist)
                {
                    dist = newDistance;
                    closet = f;
                }
            }

            return closet;
        }

        private double CalcualteDistanceToFeature(RouteFeature closet, Coordinate coordinate)
        {
            var coords = ((LineString)closet.Feature.Geometry).Coordinates;

            double distance = GetDistance(coords.Skip(1).First(), coordinate);

            foreach (var c in coords.Skip(1))
            {
                var newDistance = GetDistance(c,coordinate);

                if (newDistance < distance)
                    distance = newDistance;
            }

            return distance;
        }

        private double GetDistance(Position a, Coordinate b)
        {
            return Math.Sqrt(Math.Pow(a.Latitude - b.Lat, 2) + Math.Pow(a.Longitude - b.Lng, 2));
        }
    }
}
