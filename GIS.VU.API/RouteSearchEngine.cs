using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GIS.VU.API.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GIS.VU.API
{
    public class RouteSearchEngine
    {
        public RouteSearchResponse FindRoute(RouteSearchRequest request)
        {
            return new RouteSearchResponse()
            {
                Type = "LineString",
                Coordinates = new[] { new[] { request.Start.Lng, request.Start.Lat }, new[] { request.End.Lng, request.End.Lat } }
            };
        }
    }
}
