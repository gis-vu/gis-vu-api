using BAMCIS.GeoJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GIS.VU.API
{
    public class RouteFeature
    {
        public double Length { get; set; }
        public Feature Feature { get; set; }
        public List<RouteFeature> Neighbours { get; set; } = new List<RouteFeature>();
    }
}
