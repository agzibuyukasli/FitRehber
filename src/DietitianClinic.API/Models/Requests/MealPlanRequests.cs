namespace DietitianClinic.API.Models.Requests
{
    /// <summary>
    /// Beslenme planı oluşturma isteği
    /// </summary>
    public class CreateMealPlanRequest
    {
        public int PatientId { get; set; }
        public string Title { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int TargetCalories { get; set; }
        public double TargetProtein { get; set; }
        public double TargetCarbs { get; set; }
        public double TargetFat { get; set; }
        public string Restrictions { get; set; }
        public List<string>? Tasks { get; set; }
        public bool IsActive { get; set; } = true;
        public string? BreakfastContent { get; set; }
        public string? LunchContent { get; set; }
        public string? SnackContent { get; set; }
        public string? DinnerContent { get; set; }
    }

    /// <summary>
    /// Beslenme planı güncelleme isteği
    /// </summary>
    public class UpdateMealPlanRequest
    {
        public string Title { get; set; }
        public DateTime? EndDate { get; set; }
        public int TargetCalories { get; set; }
        public double TargetProtein { get; set; }
        public double TargetCarbs { get; set; }
        public double TargetFat { get; set; }
        public string Restrictions { get; set; }
        public List<string>? Tasks { get; set; }
        public bool IsActive { get; set; }
        public string? BreakfastContent { get; set; }
        public string? LunchContent { get; set; }
        public string? SnackContent { get; set; }
        public string? DinnerContent { get; set; }
    }

    public class UpdateMealPlanProgressRequest
    {
        public List<string>? CompletedTaskIds { get; set; }
    }

    public class CreateMealPlanChangeRequest
    {
        public string? Type { get; set; }
        public string? Priority { get; set; }
        public string? Note { get; set; }
    }
}
