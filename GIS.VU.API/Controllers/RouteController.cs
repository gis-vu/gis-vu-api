using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace GIS.VU.API.Controllers
{
    
    [ApiController]
    public class RouteController : ControllerBase
    {
        [Route("")]
        [HttpGet]
        public ActionResult<IEnumerable<string>> Get()
        {
            return new string[] { "value1", "value2" };
        }


        [Route("api/[controller]")]
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }
    }
}
