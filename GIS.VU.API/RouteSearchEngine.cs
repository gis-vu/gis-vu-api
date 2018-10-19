using System;
using System.Collections.Generic;
using System.Linq;
using BAMCIS.GeoJSON;
using GIS.VU.API.DTOs;
using Microsoft.EntityFrameworkCore.Internal;

namespace GIS.VU.API
{
    public class RouteSearchEngine
    {
        private readonly RouteFeature[] _routeFeatures;

        public RouteSearchEngine(GeoJsonFileReader fileReader, string path)
        {
            _routeFeatures = fileReader.Read(path).ToArray();
        }

        public RouteSearchResponse FindRoute(RouteSearchRequest request)
        {
            var startFeature = FindClosetFeature(request.Start);
            var endFeature = FindClosetFeature(request.End);

            var pointFeature = request.Point == null ? null : FindClosetFeature(request.Point);

            if (pointFeature == null)
            {
                var g1 = new Graph(_routeFeatures, null);
                var path = g1.FindShortestPath(startFeature, endFeature, null);

                if (path == null)
                    return new RouteSearchResponse(Array.Empty<Route>());

                var route1 = PathToRoute(path);

                var g2 = new Graph(_routeFeatures, request.SearchOptions);
                var path2 = g2.FindShortestPath(startFeature, endFeature, null);

                var route2 = PathToRoute(path2);

                return new RouteSearchResponse(new[] { route1, route2 });
            }
            else
            {
                var g1 = new Graph(_routeFeatures, null);
                var path1 = g1.FindShortestPath(startFeature, pointFeature, null);

                if (path1 == null)
                    return new RouteSearchResponse(Array.Empty<Route>());

                var path2 = g1.FindShortestPath(pointFeature, endFeature, null);
                if (path2 == null)
                    return new RouteSearchResponse(Array.Empty<Route>());

                var route1 = PathToRoute(path1);
                var route2 = PathToRoute(path2);
                var r1 = MergeTwoRoutes(route1, route2);

                var g2 = new Graph(_routeFeatures, request.SearchOptions);
                var path3 = g2.FindShortestPath(startFeature, pointFeature, null);
                var path4 = g2.FindShortestPath(pointFeature, endFeature, path3);            

                var route3 = PathToRoute(path3);
                var route4 = PathToRoute(path4);
                var r = MergeTwoRoutes(route3, route4);

                return new RouteSearchResponse(new[] { r1, r  });
            }
        }

        private Route MergeTwoRoutes(Route route1, Route route2)
        {
            var data =new RouteData()
            {
                Type = route1.Data.Type
            };
            var info = new RouteInfo()
            {
                Length = route1.Info.Length + route2.Info.Length
            };

            var result = new Route()
            {
                Data = data,
                Info = info
            };

            var coordinates = new List<double[]>();

            if (Enumerable.SequenceEqual(route1.Data.Coordinates.Last() , route2.Data.Coordinates.First()))
            {
                coordinates.AddRange(route1.Data.Coordinates);
                coordinates.AddRange(route2.Data.Coordinates);
            }
            else if (Enumerable.SequenceEqual(route1.Data.Coordinates.Last() , route2.Data.Coordinates.Last()))
            {
                
                coordinates.AddRange(route2.Data.Coordinates);
                coordinates.Reverse();
                coordinates.InsertRange(0, route1.Data.Coordinates);
            }
            else if (Enumerable.SequenceEqual(route1.Data.Coordinates.First() , route2.Data.Coordinates.First()))
            {
                coordinates.AddRange(route1.Data.Coordinates);
                coordinates.Reverse();
                coordinates.AddRange(route2.Data.Coordinates);
            }
            else if (Enumerable.SequenceEqual(route1.Data.Coordinates.First(), route2.Data.Coordinates.Last()))
            {
                coordinates.AddRange(route2.Data.Coordinates);
                coordinates.AddRange(route1.Data.Coordinates);
            }
           
            
            else
            {
                coordinates.AddRange(route1.Data.Coordinates);

                route2.Data.Coordinates = route2.Data.Coordinates.Reverse().ToArray();

                for (int i = 0; i < route2.Data.Coordinates.Length; i++)
                {
                    if(coordinates.Any(x=> Enumerable.SequenceEqual(x, route2.Data.Coordinates[i])))
                        continue;

                    route2.Data.Coordinates = route2.Data.Coordinates.Skip(i == 0? 0 : i-1).ToArray();

                 
                    return MergeTwoRoutes(route1, route2);
                }

            }


            result.Data.Coordinates = coordinates.ToArray();
            result.Info.Length =  Math.Round(GeoJsonFileReader.CalculateLength(result.Data.Coordinates),2);
            return result;
        }

        private Route PathToRoute(List<RouteFeature> path)
        {
            return new Route
            {
                Data = new RouteData
                {
                    Type = "LineString",
                    Coordinates = SorthPath(path.Select(x =>
                        ((LineString) x.Feature.Geometry).Coordinates.Select(y => new[] {y.Longitude, y.Latitude})
                        .ToArray()).ToArray())
                },
                Info = new RouteInfo
                {
                    Length = Math.Round(path.Sum(x => x.Length), 2)
                }
            };
        }

        private double[][] SorthPath(IList<double[][]> path)
        {
            var coordinates = new List<double[]>();
            var last = path.First().Last();

            coordinates.AddRange(path.First());

            foreach (var f in path.Skip(1))
                if (AreClose(last, f.First()))
                {
                    coordinates.AddRange(f);
                    last = f.Last();
                }
                else if (AreClose(last, f.Last())) //apversti
                {
                    coordinates.AddRange(f.Reverse());
                    last = f.First();
                }
                else //jei reikia deti i prieki
                {
                    coordinates = AreClose(coordinates.First(), f.Last()) ? f.Concat(coordinates.ToArray()).ToList() : f.Reverse().Concat(coordinates.ToArray()).ToList();
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
            var closet = _routeFeatures.First();
            var dist = CalcualteDistanceToFeature(closet, coordinate);

            foreach (var f in _routeFeatures.Skip(1))
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
            var lineSegments = SplitFeatureIntoLineSegments(feature);

            var distance = GetDistance(lineSegments.First(), coordinate);

            foreach (var c in lineSegments.Skip(1))
            {
                var newDistance = GetDistance(c, coordinate);

                if (newDistance < distance)
                    distance = newDistance;
            }

            return distance;
        }

        private Tuple<Position, Position>[] SplitFeatureIntoLineSegments(RouteFeature feature)
        {
            var coords = ((LineString) feature.Feature.Geometry).Coordinates;
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
            double x = point.Lat,
                y = point.Lng,
                x1 = lineSegment.Item1.Latitude,
                y1 = lineSegment.Item1.Longitude,
                x2 = lineSegment.Item2.Latitude,
                y2 = lineSegment.Item2.Longitude;

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

        private double GetDistance(double[] first, double[] second)
        {
            return Math.Sqrt(Math.Pow(first.First() - second.First(), 2) + Math.Pow(first.Last() - second.Last(), 2));
        }
    }
}