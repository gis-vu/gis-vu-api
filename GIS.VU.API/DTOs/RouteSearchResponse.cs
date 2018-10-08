using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GIS.VU.API.DTOs
{
    public class RouteSearchResponse
    {
        public string Type { get; set; }
        public double[][] Coordinates { get; set; }
    }
}
