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
        // http GET https://localhost:5001/demo/status/200 --verify no -v
        
        [HttpGet("status/{code:int}", Name = "get_status")]
        public IActionResult Status(int code)
        {
            return StatusCode(code);
        }

        
        // http GET https://localhost:5001/demo/error?errorType=ExternalDependency&dependency=ob-uk --verify no -v
        [HttpGet("error", Name = "get_error")]
        public IActionResult Error([FromQuery]ErrorType? errorType, [FromQuery]string? dependency)
        {
            if (errorType.HasValue)
            {
                HttpContext.SetSliError(errorType.Value, dependency);
            }

            return StatusCode(500);
        }

        // http GET https://localhost:5001/demo/exception --verify no -v
        [HttpGet("exception", Name = "get_exception")]
        public IActionResult Exception()
        {
            throw new HttpRequestException("Invalid request");
        }

        // http GET https://localhost:5001/demo/sliex --verify no -v
        [HttpGet("sliex", Name = "get_sliex")]
        public IActionResult SliException()
        {
            throw new SliException(ErrorType.ExternalDependency, "sliex_dependency");
        }
    }
}