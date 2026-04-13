using Microsoft.AspNetCore.Mvc;

namespace DietitianClinic.API.Controllers
{
    [ApiController]
    [Route("")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var indexPath = Path.Combine(wwwRoot, "index.html");
            
            if (System.IO.File.Exists(indexPath))
            {
                var content = System.IO.File.ReadAllText(indexPath);
                return Content(content, "text/html");
            }
            
            var response = new
            {
                message = "🏥 Dietitian Clinic Automation API'ye hoş geldiniz!",
                version = "1.0.0",
                status = "✅ Running",
                documentation = "Swagger UI: /swagger",
                endpoints = new
                {
                    health = "GET /api/health/status",
                    database = "GET /api/health/database",
                    register = "POST /api/users/register",
                    login = "POST /api/users/login",
                    patients = "GET /api/patients"
                }
            };

            return Ok(response);
        }

        [HttpGet("swagger")]
        public IActionResult Swagger()
        {
            return Redirect("/swagger/index.html");
        }
    }
}
