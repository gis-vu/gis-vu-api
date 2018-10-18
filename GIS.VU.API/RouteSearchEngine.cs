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

        private double CalcualteDistanceToFeature(RouteFeature feature, Coordinate coordinate)
        {
            Tuple<Position, Position>[] lineSegments = SplitFeatureIntoLineSegments(feature);

            double distance = GetDistance(lineSegments.First(), coordinate);

            foreach (var c in lineSegments.Skip(1))
            {
                var newDistance = GetDistance(c,coordinate);

                if (newDistance < distance)
                    distance = newDistance;
            }

            return distance;
        }

        private Tuple<Position, Position>[] SplitFeatureIntoLineSegments(RouteFeature feature)
        {
            var coords = ((LineString)feature.Feature.Geometry).Coordinates;
            var lineSegments = new List<Tuple<Position, Position>>();

            var lastPosition = coords.First();

            foreach (var c in coords.Skip(1))
            {
                lineSegments.Add(new Tuple<Position, Position>(lastPosition, c));
                lastPosition = c;
            }

            return lineSegments.ToArray();
        }

        private double GetDistance(Tuple<Position, Position> lineSegment, Coordinate point)
        {
            double x = point.Lat, y= point.Lng, x1=lineSegment.Item1.Latitude, y1= lineSegment.Item1.Longitude, x2= lineSegment.Item2.Latitude, y2= lineSegment.Item2.Longitude;

            var A = x - x1;
            var B = y - y1;
            var C = x2 - x1;
            var D = y2 - y1;

            var dot = A * C + B * D;
            var len_sq = C * C + D * D;
            double param = -1;
            if (len_sq != 0) //in case of 0 length line
                param = dot / len_sq;

            double xx, yy;

            if (param < 0)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * C;
                yy = y1 + param * D;
            }

            var dx = x - xx;
            var dy = y - yy;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        //private double GetDistance(Position a, Coordinate b)
        //{
        //    return Math.Sqrt(Math.Pow(a.Latitude - b.Lat, 2) + Math.Pow(a.Longitude - b.Lng, 2));
        //}

        private double GetDistance(double[] first, double[] second)
        {
            return Math.Sqrt(Math.Pow(first.First() - second.First(), 2) + Math.Pow(first.Last() - second.Last(), 2));
        }
    }
}
