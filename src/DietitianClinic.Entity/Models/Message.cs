using DietitianClinic.Entity.Base;

namespace DietitianClinic.Entity.Models
{
    public class Message : BaseEntity
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public string? AttachmentUrl { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
    }
}
