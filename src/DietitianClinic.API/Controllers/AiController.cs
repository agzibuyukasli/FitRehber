using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.Entity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DietitianClinic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiController : ControllerBase
    {
        private readonly DietitianClinicDbContext _context;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;

        public AiController(DietitianClinicDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        [HttpPost("consult")]
        public async Task<IActionResult> Consult([FromBody] AiConsultRequest req)
        {
            var apiKey = _config["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_ANTHROPIC_API_KEY_HERE")
                return BadRequest(new { error = "AI entegrasyonu için appsettings.json dosyasında Anthropic:ApiKey değerini ayarlayın." });

            var me = CurrentUserId();

            // Danışan verilerini çek
            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.Measurements)
                .Include(p => p.MealPlans)
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.Id == req.PatientId && !p.IsDeleted);

            if (patient == null) return NotFound(new { error = "Danışan bulunamadı." });

            var context = BuildPatientContext(patient);

            var systemPrompt = @"Sen FitRehber klinik otomasyon sistemine entegre edilmiş uzman bir diyetisyen yapay zeka asistanısın.
Türkçe konuşursun. Diyetisyenlere danışanları hakkında klinik değerlendirme ve önerilerde bulunursun.
Yanıtların kısa, net ve uygulanabilir olsun. Tıbbi tavsiye vermekten kaçın, bunun yerine beslenme ve yaşam tarzı önerileri sun.
Danışan verilerine dayalı spesifik, kişiselleştirilmiş öneriler ver. Genel bilgi vermekten kaçın.";

            var userMessage = $"Danışan Verileri:\n{context}\n\nDiyetisyen sorusu: {req.Message}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var body = new
                {
                    model = "claude-haiku-4-5-20251001",
                    max_tokens = 600,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = userMessage } }
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return StatusCode(502, new { error = "AI servisi yanıt vermedi. Lütfen API anahtarınızı kontrol edin.", detail = responseBody });

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                return Ok(new { reply = text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("quick-analysis/{patientId}")]
        public async Task<IActionResult> QuickAnalysis(int patientId)
        {
            var apiKey = _config["Anthropic:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_ANTHROPIC_API_KEY_HERE")
                return BadRequest(new { error = "AI_KEY_NOT_SET" });

            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.Measurements)
                .Include(p => p.MealPlans)
                .Include(p => p.Appointments)
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.IsDeleted);

            if (patient == null) return NotFound();

            var context = BuildPatientContext(patient);

            var systemPrompt = "Sen FitRehber diyetisyen yapay zeka asistanısın. Türkçe, kısa ve net cevaplar ver.";
            var userMessage = $"Bu danışan için hızlı klinik değerlendirme yap ve 3-4 madde halinde önemli noktaları belirt:\n{context}";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var body = new
                {
                    model = "claude-haiku-4-5-20251001",
                    max_tokens = 400,
                    system = systemPrompt,
                    messages = new[] { new { role = "user", content = userMessage } }
                };

                var json = JsonSerializer.Serialize(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.anthropic.com/v1/messages", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) return StatusCode(502, new { error = "AI servisi yanıt vermedi." });

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString();
                return Ok(new { reply = text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private static string BuildPatientContext(Patient p)
        {
            var sb = new StringBuilder();
            var age = DateTime.Today.Year - p.BirthDate.Year;
            if (p.BirthDate.Date > DateTime.Today.AddYears(-age)) age--;

            sb.AppendLine($"Ad: {p.FirstName} {p.LastName}");
            sb.AppendLine($"Yaş: {age}, Cinsiyet: {(p.Gender == Gender.Male ? "Erkek" : p.Gender == Gender.Female ? "Kadın" : "Diğer")}");
            if (!string.IsNullOrWhiteSpace(p.MedicalHistory)) sb.AppendLine($"Tıbbi Geçmiş: {p.MedicalHistory}");
            if (!string.IsNullOrWhiteSpace(p.Allergies)) sb.AppendLine($"Alerjiler: {p.Allergies}");
            if (!string.IsNullOrWhiteSpace(p.Notes)) sb.AppendLine($"Notlar: {p.Notes}");

            var measurements = p.Measurements?.OrderByDescending(m => m.MeasurementDate).Take(5).ToList();
            if (measurements != null && measurements.Any())
            {
                sb.AppendLine("Son Ölçümler:");
                foreach (var m in measurements)
                {
                    sb.AppendLine($"  {m.MeasurementDate:dd.MM.yyyy}: Kilo={m.Weight}kg, Boy={m.Height}cm" +
                        (m.BMI.HasValue ? $", BMI={m.BMI:F1}" : "") +
                        (m.BodyFatPercentage.HasValue ? $", Yağ%={m.BodyFatPercentage:F1}" : "") +
                        (m.WaistCircumference.HasValue ? $", Bel={m.WaistCircumference}cm" : ""));
                }
                if (measurements.Count >= 2)
                {
                    var diff = measurements[0].Weight - measurements[measurements.Count - 1].Weight;
                    sb.AppendLine($"Kilo Trendi: {measurements.Count} ölçümde {diff:+0.#;-0.#;0} kg");
                }
            }

            var activePlan = p.MealPlans?.Where(m => m.Status == MealPlanStatus.Active).OrderByDescending(m => m.StartDate).FirstOrDefault();
            if (activePlan != null)
            {
                sb.AppendLine($"Aktif Beslenme Planı: {activePlan.Title}");
                if (activePlan.TargetCalories > 0) sb.AppendLine($"  Hedef Kalori: {activePlan.TargetCalories} kcal");
                if (activePlan.TargetProtein > 0) sb.AppendLine($"  Protein: {activePlan.TargetProtein}g, Karbonhidrat: {activePlan.TargetCarbs}g, Yağ: {activePlan.TargetFat}g");
                if (!string.IsNullOrWhiteSpace(activePlan.DietaryRestrictions)) sb.AppendLine($"  Diyet Kısıtlamaları: {activePlan.DietaryRestrictions}");
            }

            var lastAppt = p.Appointments?.Where(a => !a.IsDeleted).OrderByDescending(a => a.AppointmentDate).FirstOrDefault();
            if (lastAppt != null)
                sb.AppendLine($"Son Randevu: {lastAppt.AppointmentDate:dd.MM.yyyy}");

            return sb.ToString();
        }
    }

    public class AiConsultRequest
    {
        public int PatientId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
