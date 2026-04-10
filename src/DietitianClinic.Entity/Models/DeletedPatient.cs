namespace DietitianClinic.Entity.Models
{
    /// <summary>
    /// Silinen hastaların arşiv kaydı.
    /// BaseEntity'den türetilmez — global soft-delete query filter'dan muaf tutmak için.
    /// </summary>
    public class DeletedPatient
    {
        public int Id { get; set; }
        public int OriginalPatientId { get; set; }
        public int? OwnerUserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public Gender Gender { get; set; }
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string MedicalHistory { get; set; } = string.Empty;
        public string Allergies { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedDate { get; set; }
    }
}
