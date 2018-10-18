using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GIS.VU.API.DTOs
{
    public class RouteSearchRequest
    {
        public Coordinate Start { get; set; }
        public Coordinate End { get; set; }
        public SearchOptions SearchOptions { get; set; }
    }
}
