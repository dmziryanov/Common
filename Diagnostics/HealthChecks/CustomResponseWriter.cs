using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Indusoft.CalendarPlanning.Common.Diagnostics.HealthChecks
{
    public class CustomResponseWriter
    {
        public static Task WriteHealthCheckResponse(HttpContext httpContext, HealthReport healthReport)
        {
            httpContext.Response.ContentType = "application/json; charset=utf-8";
            
            var result = JsonSerializer.Serialize(new
            {
                status = healthReport.Status.ToString(),
                errors = healthReport.Entries.Select(e => new {
                    key = e.Key,
                    value = e.Value.Status.ToString(),
                    description = e.Value.Description.ToString()
                })
            });
            return httpContext.Response.WriteAsync(result);
        }
    }
}