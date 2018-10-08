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
            //var endFeature = FindClosetFeature(request.End);

            return new RouteSearchResponse()
            {
                Type = "LineString",
                Coordinates = ((LineString)startFeature.Feature.Geometry).Coordinates.Select(x=> new[] { x.Longitude, x.Latitude}).ToArray()
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
            var first = coords.First();
            var second = coords.Skip(1).First();

            double distance = CalcualteDistanceToLine(first, second, coordinate);

            var initial = coords.Skip(1).First();

            foreach (var c in coords.Skip(2))
            {
                var newDistance = CalcualteDistanceToLine(initial,c,coordinate);
                initial = c;

                if (newDistance > distance)
                    distance = newDistance;
            }

            return distance;
        }

        private double CalcualteDistanceToLine(Position first, Position second, Coordinate coordinate)
        {
            return Math.Abs((second.Latitude - first.Latitude)*coordinate.Lng - (second.Longitude - first.Longitude)*coordinate.Lat + first.Latitude* second.Longitude - first.Longitude* second.Latitude) / (Math.Sqrt(Math.Pow(second.Latitude-first.Latitude,2)+Math.Pow(second.Longitude-first.Longitude,2)));
        }
    }
}
