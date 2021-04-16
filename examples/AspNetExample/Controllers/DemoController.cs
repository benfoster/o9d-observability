using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using O9d.Metrics.AspNet;
using O9d.Observability;

namespace examples.AspNetExample.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DemoController : ControllerBase
    {
        // http GET https://localhost:5001/demo/status/get_customer/200 --verify no -v
        
        [HttpGet("status/{operation}/{code:int}")]
        public IActionResult Status(string operation, int code)
        {
            HttpContext.SetOperation(operation);
            return StatusCode(code);
        }

        
        // http GET https://localhost:5001/demo/error/get_customer?errorType=ExternalDependency&dependency=ob-uk --verify no -v
        [HttpGet("error/{operation}")]
        public IActionResult Error(string operation, [FromQuery]ErrorType? errorType, [FromQuery]string? dependency)
        {
            HttpContext.SetOperation(operation);
            
            if (errorType.HasValue)
            {
                HttpContext.SetSliError(errorType.Value, dependency);
            }

            return StatusCode(500);
        }

        // http GET https://localhost:5001/demo/exception/get_customer --verify no -v
        [HttpGet("exception/{operation}")]
        public IActionResult Exception(string operation)
        {
            HttpContext.SetOperation(operation);
            throw new HttpRequestException("Invalid request");
        }
    }
}