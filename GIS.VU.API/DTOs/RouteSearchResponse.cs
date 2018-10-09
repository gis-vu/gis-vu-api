using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GIS.VU.API.DTOs
{
    public class RouteSearchResponse
    {
        public Route[] Routes { get; set; }

        public RouteSearchResponse(IEnumerable<Route> routes)
        {
            Routes = routes.ToArray();
        }
    }
}
