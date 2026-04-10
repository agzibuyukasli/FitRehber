using System;

namespace DietitianClinic.Entity.Base
{
    /// <summary>
    /// Tüm entity'lerin kalıtım alacağı temel sınıf
    /// </summary>
    public abstract class BaseEntity
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }
        public DateTime? DeletedDate { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
