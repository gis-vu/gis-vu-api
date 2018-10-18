using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GIS.VU.API.DTOs;

namespace GIS.VU.API
{
    class Graph
    {
        private RouteFeature[] _routeFeatures;
        private SearchOptions _searchOptions;


        public Graph(RouteFeature[] routeFeatures, SearchOptions searchOptions)
        {
            _routeFeatures = routeFeatures;
            _searchOptions = searchOptions;
        }

        public List<RouteFeature> FindShortestPath(RouteFeature startFeature, RouteFeature endFeature)
        {
            var previous = new Dictionary<RouteFeature, RouteFeature>();
            var distances = new Dictionary<RouteFeature, double>();
            var nodes = new List<RouteFeature>();

            List<RouteFeature> path = null;

            foreach (var vertex in _routeFeatures)
            {
                if (vertex == startFeature)
                {
                    distances[vertex] = ApplySearchOptionsToGetLength(startFeature);
                }
                else
                {
                    distances[vertex] = double.MaxValue;
                }

                nodes.Add(vertex);
            }

            while (nodes.Count != 0)
            {
                nodes.Sort((x, y) => Math.Sign(distances[x] - distances[y]));

                var smallest = nodes.First();
                nodes.Remove(smallest);

                if (smallest == endFeature)
                {
                    path = new List<RouteFeature>();
                    while (previous.ContainsKey(smallest))
                    {
                        path.Add(smallest);
                        smallest = previous[smallest];
                    }

                    break;
                }

                if (distances[smallest] == double.MaxValue)
                {
                    break;
                }

                foreach (var neighbor in smallest.Neighbours)
                {
                    var alt = distances[smallest] + ApplySearchOptionsToGetLength(neighbor);
                    if (alt < distances[neighbor])
                    {
                        distances[neighbor] = alt;
                        previous[neighbor] = smallest;
                    }
                }
            }

            if (path == null) //no path 
                return null;

            path.Add(startFeature);

            return path;
        }

        private double ApplySearchOptionsToGetLength(RouteFeature feature)
        {
            if (_searchOptions == null)
                return feature.Length;

            var option = _searchOptions.PropertyImportance.FirstOrDefault(x =>
                feature.Feature.Properties.Any(y => y.Key == x.Property && y.Value == x.Value));

            if (option == null)
                return feature.Length;

            return feature.Length * option.Importance;
        }
    }
}
