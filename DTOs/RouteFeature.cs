using System.Collections.Generic;
using BAMCIS.GeoJSON;

namespace DTOs
{
    public class RouteFeature
    {
        //public double Length { get; set; }
        public Feature Feature { get; set; }
        public List<RouteFeature> Neighbours { get; set; } = new List<RouteFeature>();
    }
}
