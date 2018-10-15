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
        private RouteFeature[] routeFeatures;

        public RouteSearchEngine(GeoJsonFileReader fileReader, string path)
        {
            routeFeatures = fileReader.Read(path).ToArray();
        }

        public RouteSearchResponse FindRoute(RouteSearchRequest request)
        {
            var startFeature = FindClosetFeature(request.Start);
            var endFeature = FindClosetFeature(request.End);


            Graph g1 = new Graph(routeFeatures);
            var path = g1.shortest_path(startFeature, endFeature);
            var route1 = PathToRoute(path);

            if(path.Count == 2)
                return new RouteSearchResponse(new[] { route1 });

            var routeFeature = path.Skip(path.Count / 2).First();


            var oldValue = routeFeature.Length;
            routeFeature.Length = double.MaxValue / 2;
            var route2 = PathToRoute(g1.shortest_path(startFeature, endFeature));
            routeFeature.Length = oldValue;
            //var routeFeatureClone = new RouteFeature()
            //{
            //    Feature = routeFeature.Feature,
            //    Neighbours = routeFeature.Neighbours,
            //    Length = double.MaxValue / 2
            //};

            //var routeFeaturesClone = (RouteFeature[])routeFeatures.Clone();
            //var index = Array.FindIndex(routeFeaturesClone, e => e == routeFeature);

            //routeFeaturesClone[index] = routeFeatureClone;

            //Graph g2 = new Graph(routeFeaturesClone);
            //var route2 = PathToRoute(g2.shortest_path(startFeature, endFeature));

            return new RouteSearchResponse(new[] { route1, route2 });
        }

        private Route PathToRoute(List<RouteFeature> path)
        {
            return new Route(){
                Data = new RouteData()
                {
                    Type = "LineString",
                    Coordinates = SorthPath(path.Select(x => ((LineString)x.Feature.Geometry).Coordinates.Select(y => new[] { y.Longitude, y.Latitude }).ToArray()))
                },
                Info = new RouteInfo()
                {
                    Length = Math.Round(path.Sum(x => x.Length),2)
                }
            };
        }

        private double[][] SorthPath(IEnumerable<double[][]> path)
        {
            var coordinates = new List<double[]>();
            var last = path.First().Last();

            coordinates.AddRange(path.First());

            foreach (var f in path.Skip(1))
            {
                if(AreClose(last, f.First()))
                {
                    coordinates.AddRange(f);
                    last = f.Last();
                }
                else if(AreClose(last, f.Last()))//apversti
                {
                    coordinates.AddRange(f.Reverse());
                    last = f.First();
                }
                else //jei reikia deti i prieki
                {
                    if (AreClose(coordinates.First(), f.Last()))
                        coordinates = f.Concat(coordinates.ToArray()).ToList();
                    else
                        coordinates = f.Reverse().Concat(coordinates.ToArray()).ToList();
                }
            }

            return coordinates.ToArray();
        }

        private bool AreClose(double[] first, double[] second)
        {
            if (GetDistance(first, second) < GeoJsonFileReader.DistanceDiff)
                return true;

            return false;
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

        private double GetDistance(double[] first, double[] second)
        {
            return Math.Sqrt(Math.Pow(first.First() - second.First(), 2) + Math.Pow(first.Last() - second.Last(), 2));
        }
    }
}
