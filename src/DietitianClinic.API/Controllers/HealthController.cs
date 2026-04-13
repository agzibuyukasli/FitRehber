using Microsoft.AspNetCore.Mvc;

namespace DietitianClinic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Status()
        {
            var response = new
            {
                status = "✅ API is running",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            return Ok(response);
        }

        [HttpGet("database")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                var dbContext = HttpContext.RequestServices.GetRequiredService<DietitianClinic.DataAccess.Context.DietitianClinicDbContext>();
                var canConnect = await dbContext.Database.CanConnectAsync();

                if (canConnect)
                {
                    return Ok(new
                    {
                        status = "✅ Database connection successful",
                        database = "DietitianClinicDB",
                        timestamp = DateTime.UtcNow
                    });
                }
                else
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                    {
                        status = "❌ Database connection failed",
                        error = "Cannot connect to database"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection check failed");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new
                {
                    status = "❌ Database connection error",
                    error = ex.Message
                });
            }
        }

        [HttpGet("info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Info()
        {
            var response = new
            {
                apiName = "Dietitian Clinic Automation API",
                version = "1.0.0",
                description = "ASP.NET Core Web API ile geliştirilmiş, profesyonel Diyetisyen Kliniği Otomasyon sistemi.",
                authors = new[] { "Development Team" },
                endpoints = new
                {
                    health = "/api/health/status",
                    database = "/api/health/database",
                    info = "/api/health/info",
                    swagger = "/swagger"
                }
            };

            return Ok(response);
        }
    }
}
