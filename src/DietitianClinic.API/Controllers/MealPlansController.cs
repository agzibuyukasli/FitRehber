using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using DietitianClinic.API.Models.Requests;
using DietitianClinic.API.Models.Response;
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
    public class MealPlansController : ControllerBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly DietitianClinicDbContext _context;
        private readonly ILogger<MealPlansController> _logger;

        public MealPlansController(DietitianClinicDbContext context, ILogger<MealPlansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMealPlans()
        {
            try
            {
                var (currentUserId, currentUserEmail, isDietitian, isPatient) = GetCurrentUserContext();
                var mealPlans = await BuildMealPlanQuery(currentUserId, currentUserEmail, isDietitian, isPatient)
                    .OrderByDescending(x => x.StartDate)
                    .ThenByDescending(x => x.CreatedDate)
                    .ToListAsync();

                return Ok(mealPlans.Select(ToMealPlanDetail));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme planlari getirme basarisiz");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetMealPlanById(int id)
        {
            try
            {
                var (currentUserId, currentUserEmail, isDietitian, isPatient) = GetCurrentUserContext();
                var mealPlan = await BuildMealPlanQuery(currentUserId, currentUserEmail, isDietitian, isPatient)
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (mealPlan == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Beslenme plani bulunamadi" });
                }

                return Ok(ToMealPlanDetail(mealPlan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme plani getirme basarisiz");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateMealPlan([FromBody] CreateMealPlanRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var (currentUserId, _, isDietitian, _) = GetCurrentUserContext();
                if (!isDietitian || !currentUserId.HasValue)
                {
                    return Forbid();
                }

                if (request.PatientId <= 0 || string.IsNullOrWhiteSpace(request.Title) || request.StartDate == default)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Danisan, baslik ve baslangic tarihi zorunludur" });
                }

                var patient = await _context.Patients
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.PatientId && !x.IsDeleted && x.UserId == currentUserId.Value);
                var dietitian = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == currentUserId.Value && !x.IsDeleted);

                if (patient == null)
                {
                    return BadRequest(new ApiResponse { Success = false, Message = "Danisan bulunamadi veya size bagli degil" });
                }

                var meals = new MealPlanMeals
                {
                    Breakfast = request.BreakfastContent,
                    Lunch = request.LunchContent,
                    Snack = request.SnackContent,
                    Dinner = request.DinnerContent
                };
                var metadata = BuildMetadata(request.Tasks, null, null, meals.HasContent() ? meals : null);
                var mealPlan = new MealPlan
                {
                    PatientId = request.PatientId,
                    UserId = currentUserId.Value,
                    Title = request.Title.Trim(),
                    Description = BuildMealPlanDescription(request.TargetCalories, request.TargetProtein, request.TargetCarbs, request.TargetFat),
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    DurationInDays = CalculateDurationInDays(request.StartDate, request.EndDate),
                    TargetCalories = request.TargetCalories,
                    TargetProtein = request.TargetProtein,
                    TargetCarbs = request.TargetCarbs,
                    TargetFat = request.TargetFat,
                    DietaryRestrictions = request.Restrictions ?? string.Empty,
                    Status = request.IsActive ? MealPlanStatus.Active : MealPlanStatus.Draft,
                    Notes = JsonSerializer.Serialize(metadata, JsonOptions)
                };

                await _context.MealPlans.AddAsync(mealPlan);
                await _context.SaveChangesAsync();

                mealPlan.Patient = patient;
                mealPlan.User = dietitian;
                return CreatedAtAction(nameof(GetMealPlanById), new { id = mealPlan.Id }, ToMealPlanDetail(mealPlan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme plani olusturma basarisiz");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMealPlan(int id, [FromBody] UpdateMealPlanRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var (currentUserId, _, isDietitian, _) = GetCurrentUserContext();
                if (!isDietitian || !currentUserId.HasValue)
                {
                    return Forbid();
                }

                var mealPlan = await _context.MealPlans
                    .Include(x => x.Patient)
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.UserId == currentUserId.Value);

                if (mealPlan == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Beslenme plani bulunamadi" });
                }

                var metadata = ReadMetadata(mealPlan.Notes);
                var updMeals = new MealPlanMeals
                {
                    Breakfast = request.BreakfastContent,
                    Lunch = request.LunchContent,
                    Snack = request.SnackContent,
                    Dinner = request.DinnerContent
                };
                if (updMeals.HasContent())
                {
                    metadata.Meals = updMeals;
                    metadata.Tasks = BuildMealTasks(updMeals);
                }
                else
                {
                    metadata.Tasks = NormalizeTasks(request.Tasks);
                }

                mealPlan.Title = string.IsNullOrWhiteSpace(request.Title) ? mealPlan.Title : request.Title.Trim();
                mealPlan.EndDate = request.EndDate;
                mealPlan.DurationInDays = CalculateDurationInDays(mealPlan.StartDate, request.EndDate);
                mealPlan.TargetCalories = request.TargetCalories;
                mealPlan.TargetProtein = request.TargetProtein;
                mealPlan.TargetCarbs = request.TargetCarbs;
                mealPlan.TargetFat = request.TargetFat;
                mealPlan.DietaryRestrictions = request.Restrictions ?? string.Empty;
                mealPlan.Status = request.IsActive ? MealPlanStatus.Active : MealPlanStatus.Draft;
                mealPlan.Description = BuildMealPlanDescription(mealPlan.TargetCalories, mealPlan.TargetProtein, mealPlan.TargetCarbs, mealPlan.TargetFat);
                mealPlan.Notes = JsonSerializer.Serialize(metadata, JsonOptions);

                await _context.SaveChangesAsync();
                return Ok(ToMealPlanDetail(mealPlan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme plani guncelleme basarisiz");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}/progress")]
        public async Task<IActionResult> UpdateMealPlanProgress(int id, [FromBody] UpdateMealPlanProgressRequest request)
        {
            try
            {
                var (currentUserId, currentUserEmail, isDietitian, isPatient) = GetCurrentUserContext();
                if (!isPatient || !currentUserId.HasValue)
                {
                    return Forbid();
                }

                var mealPlan = await _context.MealPlans
                    .Include(x => x.Patient)
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.Patient.Email == currentUserEmail);

                if (mealPlan == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Beslenme plani bulunamadi" });
                }

                var metadata = ReadMetadata(mealPlan.Notes);

                // Geçmiş kaydı: mevcut ilerlemeyi sıfırlamadan önce kaydet
                if (request.RecordHistory && !string.IsNullOrWhiteSpace(request.HistoryDate))
                {
                    var currentCompleted = metadata.CompletedTaskIds ?? new List<string>();
                    var currentTasks = metadata.Tasks ?? new List<MealPlanTaskItem>();
                    var total = currentTasks.Count;
                    var done = currentTasks.Count(x => currentCompleted.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
                    var historyPercent = total == 0 ? 0 : (int)Math.Round((double)done / total * 100);

                    metadata.ComplianceHistory ??= new List<ComplianceHistoryEntry>();
                    metadata.ComplianceHistory.RemoveAll(h => h.Date == request.HistoryDate);
                    metadata.ComplianceHistory.Add(new ComplianceHistoryEntry { Date = request.HistoryDate, Percent = historyPercent });

                    var cutoff = DateTime.UtcNow.AddDays(-90).ToString("yyyy-MM-dd");
                    metadata.ComplianceHistory.RemoveAll(h => string.Compare(h.Date, cutoff) < 0);
                }

                metadata.CompletedTaskIds = request.CompletedTaskIds?
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();
                mealPlan.Notes = JsonSerializer.Serialize(metadata, JsonOptions);

                await _context.SaveChangesAsync();
                return Ok(ToMealPlanDetail(mealPlan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme plani ilerleme guncelleme basarisiz");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id}/change-request")]
        public async Task<IActionResult> CreateChangeRequest(int id, [FromBody] CreateMealPlanChangeRequest request)
        {
            try
            {
                var (currentUserId, currentUserEmail, _, isPatient) = GetCurrentUserContext();
                if (!isPatient || !currentUserId.HasValue)
                {
                    return Forbid();
                }

                var mealPlan = await _context.MealPlans
                    .Include(x => x.Patient)
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.Patient.Email == currentUserEmail);

                if (mealPlan == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Beslenme plani bulunamadi" });
                }

                var metadata = ReadMetadata(mealPlan.Notes);
                metadata.ChangeRequests.Add(new MealPlanChangeRequestItem
                {
                    Type = string.IsNullOrWhiteSpace(request.Type) ? "Plan guncelleme" : request.Type.Trim(),
                    Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Normal" : request.Priority.Trim(),
                    Note = string.IsNullOrWhiteSpace(request.Note) ? "Danisan plan icin degisiklik talep etti." : request.Note.Trim(),
                    CreatedAt = DateTime.UtcNow
                });
                mealPlan.Notes = JsonSerializer.Serialize(metadata, JsonOptions);

                await _context.SaveChangesAsync();
                return Ok(ToMealPlanDetail(mealPlan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme plani talep olusturma basarisiz");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMealPlan(int id)
        {
            try
            {
                var (currentUserId, _, isDietitian, _) = GetCurrentUserContext();
                if (!isDietitian || !currentUserId.HasValue)
                {
                    return Forbid();
                }

                var mealPlan = await _context.MealPlans
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.UserId == currentUserId.Value);

                if (mealPlan == null)
                {
                    return NotFound(new ApiResponse { Success = false, Message = "Beslenme plani bulunamadi" });
                }

                mealPlan.IsDeleted = true;
                mealPlan.DeletedDate = DateTime.UtcNow;
                mealPlan.Status = MealPlanStatus.Cancelled;
                await _context.SaveChangesAsync();

                return Ok(new ApiResponse { Success = true, Message = "Beslenme plani silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Beslenme plani silme basarisiz");
                return BadRequest(ex.Message);
            }
        }

        private IQueryable<MealPlan> BuildMealPlanQuery(int? currentUserId, string? currentUserEmail, bool isDietitian, bool isPatient)
        {
            var query = _context.MealPlans
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .Include(x => x.Patient)
                .Include(x => x.User)
                .AsQueryable();

            if (isDietitian && currentUserId.HasValue)
            {
                query = query.Where(x => x.UserId == currentUserId.Value && x.Patient.UserId == currentUserId.Value);
            }
            else if (isPatient && !string.IsNullOrWhiteSpace(currentUserEmail))
            {
                query = query.Where(x => x.Patient.Email == currentUserEmail);
            }

            return query;
        }

        private (int? currentUserId, string? currentUserEmail, bool isDietitian, bool isPatient) GetCurrentUserContext()
        {
            var currentUserIdRaw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("userId")?.Value;
            var currentUserEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                ?? User.FindFirst(ClaimTypes.Email)?.Value;
            var roles = User.Claims
                .Where(x => x.Type == ClaimTypes.Role || x.Type == "role" || x.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                .Select(x => x.Value)
                .ToList();

            var isDietitian = roles.Any(x => x == UserRole.Dietitian.ToString() || x == "1");
            var isPatient = roles.Any(x => x == UserRole.Patient.ToString() || x == "3" || x.Equals("user", StringComparison.OrdinalIgnoreCase));
            return (int.TryParse(currentUserIdRaw, out var currentUserId) ? currentUserId : null, currentUserEmail, isDietitian, isPatient);
        }

        private static object ToMealPlanDetail(MealPlan mealPlan)
        {
            var metadata = ReadMetadata(mealPlan.Notes);
            var completed = metadata.CompletedTaskIds ?? new List<string>();
            var tasks = metadata.Tasks ?? new List<MealPlanTaskItem>();
            var totalTaskCount = tasks.Count;
            var completedTaskCount = tasks.Count(x => completed.Contains(x.Id, StringComparer.OrdinalIgnoreCase));
            var progressPercent = totalTaskCount == 0 ? 0 : (int)Math.Round((double)completedTaskCount / totalTaskCount * 100);

            return new
            {
                id = mealPlan.Id,
                patientId = mealPlan.PatientId,
                patientName = mealPlan.Patient == null ? "-" : $"{mealPlan.Patient.FirstName} {mealPlan.Patient.LastName}",
                userId = mealPlan.UserId,
                dietitianName = mealPlan.User == null ? "-" : $"{mealPlan.User.FirstName} {mealPlan.User.LastName}",
                title = mealPlan.Title,
                description = mealPlan.Description,
                startDate = mealPlan.StartDate,
                endDate = mealPlan.EndDate,
                durationInDays = mealPlan.DurationInDays,
                targetCalories = mealPlan.TargetCalories,
                targetProtein = mealPlan.TargetProtein,
                targetCarbs = mealPlan.TargetCarbs,
                targetFat = mealPlan.TargetFat,
                restrictions = mealPlan.DietaryRestrictions,
                status = (int)mealPlan.Status,
                progressPercent,
                completedTaskCount,
                totalTaskCount,
                tasks = tasks.Select(x => new
                {
                    id = x.Id,
                    title = x.Title,
                    note = x.Note,
                    isCompleted = completed.Contains(x.Id, StringComparer.OrdinalIgnoreCase)
                }),
                changeRequests = metadata.ChangeRequests
                    .OrderByDescending(x => x.CreatedAt)
                    .Select(x => new { x.Type, x.Priority, x.Note, x.CreatedAt }),
                meals = metadata.Meals == null ? null : new
                {
                    breakfast = metadata.Meals.Breakfast,
                    lunch = metadata.Meals.Lunch,
                    snack = metadata.Meals.Snack,
                    dinner = metadata.Meals.Dinner
                },
                details = BuildMealPlanDetails(mealPlan, tasks, metadata.ChangeRequests),
                complianceHistory = (metadata.ComplianceHistory ?? new List<ComplianceHistoryEntry>())
                    .OrderBy(h => h.Date)
                    .Select(h => new { date = h.Date, percent = h.Percent })
            };
        }

        private static string BuildMealPlanDescription(double calories, double protein, double carbs, double fat)
        {
            return $"Hedef kalori: {calories} kcal | Protein: {protein} g | Karbonhidrat: {carbs} g | Yag: {fat} g";
        }

        private static int CalculateDurationInDays(DateTime startDate, DateTime? endDate)
        {
            if (!endDate.HasValue || endDate.Value.Date < startDate.Date)
            {
                return 1;
            }

            return Math.Max(1, (endDate.Value.Date - startDate.Date).Days + 1);
        }

        private static MealPlanMetadata BuildMetadata(List<string>? rawTasks, List<string>? completedTaskIds, List<MealPlanChangeRequestItem>? changeRequests, MealPlanMeals? meals = null)
        {
            return new MealPlanMetadata
            {
                Tasks = meals != null && meals.HasContent() ? BuildMealTasks(meals) : NormalizeTasks(rawTasks),
                CompletedTaskIds = completedTaskIds ?? new List<string>(),
                ChangeRequests = changeRequests ?? new List<MealPlanChangeRequestItem>(),
                Meals = meals
            };
        }

        private static List<MealPlanTaskItem> BuildMealTasks(MealPlanMeals meals) => new()
        {
            new MealPlanTaskItem { Id = "meal-breakfast", Title = "Sabah Öğünü", Note = meals.Breakfast?.Trim() ?? "" },
            new MealPlanTaskItem { Id = "meal-lunch", Title = "Öğle Öğünü", Note = meals.Lunch?.Trim() ?? "" },
            new MealPlanTaskItem { Id = "meal-snack", Title = "Ara Öğünü", Note = meals.Snack?.Trim() ?? "" },
            new MealPlanTaskItem { Id = "meal-dinner", Title = "Akşam Öğünü", Note = meals.Dinner?.Trim() ?? "" }
        };

        private static List<MealPlanTaskItem> NormalizeTasks(List<string>? rawTasks)
        {
            var source = rawTasks?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();

            if (!source.Any())
            {
                source =
                [
                    "Sabah ogununu plana uygun tamamla",
                    "Ogle ogununu plana sadik kalarak tuket",
                    "Aksam ogununu atlamadan bitir",
                    "Gunluk su hedefini tamamla"
                ];
            }

            return source.Select((x, index) => new MealPlanTaskItem
            {
                Id = $"task-{index + 1}",
                Title = x,
                Note = "Danisan tarafinda isaretlenebilir gorev"
            }).ToList();
        }

        private static MealPlanMetadata ReadMetadata(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return BuildMetadata(null, null, null);
            }

            try
            {
                var parsed = JsonSerializer.Deserialize<MealPlanMetadata>(raw, JsonOptions);
                if (parsed == null)
                {
                    return BuildMetadata(null, null, null);
                }

                parsed.Tasks ??= new List<MealPlanTaskItem>();
                parsed.CompletedTaskIds ??= new List<string>();
                parsed.ChangeRequests ??= new List<MealPlanChangeRequestItem>();
                return parsed;
            }
            catch
            {
                return BuildMetadata(null, null, null);
            }
        }

        private static string[] BuildMealPlanDetails(MealPlan mealPlan, List<MealPlanTaskItem> tasks, List<MealPlanChangeRequestItem> requests)
        {
            var items = new List<string>
            {
                mealPlan.Description ?? "Plan aciklamasi bulunmuyor.",
                $"Sure: {mealPlan.DurationInDays} gun",
                $"Kisitlamalar: {(!string.IsNullOrWhiteSpace(mealPlan.DietaryRestrictions) ? mealPlan.DietaryRestrictions : "Yok")}"
            };

            items.AddRange(tasks.Select(x => $"Gorev: {x.Title}"));

            if (requests.Any())
            {
                var latest = requests.OrderByDescending(x => x.CreatedAt).First();
                items.Add($"Son talep: {latest.Type} / {latest.Priority} / {latest.Note}");
            }

            return items.ToArray();
        }

        private sealed class MealPlanMetadata
        {
            public List<MealPlanTaskItem>? Tasks { get; set; }
            public List<string>? CompletedTaskIds { get; set; }
            public List<MealPlanChangeRequestItem>? ChangeRequests { get; set; }
            public MealPlanMeals? Meals { get; set; }
            public List<ComplianceHistoryEntry>? ComplianceHistory { get; set; }
        }

        private sealed class ComplianceHistoryEntry
        {
            public string Date { get; set; } = string.Empty;
            public int Percent { get; set; }
        }

        private sealed class MealPlanMeals
        {
            public string? Breakfast { get; set; }
            public string? Lunch { get; set; }
            public string? Snack { get; set; }
            public string? Dinner { get; set; }
            public bool HasContent() =>
                !string.IsNullOrWhiteSpace(Breakfast) ||
                !string.IsNullOrWhiteSpace(Lunch) ||
                !string.IsNullOrWhiteSpace(Snack) ||
                !string.IsNullOrWhiteSpace(Dinner);
        }

        private sealed class MealPlanTaskItem
        {
            public string Id { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
        }

        private sealed class MealPlanChangeRequestItem
        {
            public string Type { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
        }
    }
}
