using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DietitianClinic.API.Hubs;
using DietitianClinic.DataAccess.Context;
using DietitianClinic.Entity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DietitianClinic.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly DietitianClinicDbContext _context;
        private readonly IHubContext<ChatHub> _hub;
        private readonly IWebHostEnvironment _env;

        public MessagesController(DietitianClinicDbContext context, IHubContext<ChatHub> hub, IWebHostEnvironment env)
        {
            _context = context;
            _hub = hub;
            _env = env;
        }

        private int CurrentUserId()
        {
            var raw = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }

        [HttpGet("{otherUserId}")]
        public async Task<IActionResult> GetConversation(int otherUserId)
        {
            var me = CurrentUserId();
            var msgs = await _context.Messages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(m => !m.IsDeleted &&
                    ((m.SenderId == me && m.ReceiverId == otherUserId) ||
                     (m.SenderId == otherUserId && m.ReceiverId == me)))
                .OrderBy(m => m.CreatedDate)
                .Select(m => new {
                    m.Id, m.SenderId, m.ReceiverId, m.Content,
                    m.IsRead, m.AttachmentUrl, m.AttachmentName, m.AttachmentType,
                    m.CreatedDate
                })
                .ToListAsync();

            // Mark received as read
            var unread = await _context.Messages
                .IgnoreQueryFilters()
                .Where(m => !m.IsDeleted && m.ReceiverId == me && m.SenderId == otherUserId && !m.IsRead)
                .ToListAsync();
            unread.ForEach(m => m.IsRead = true);
            if (unread.Any()) await _context.SaveChangesAsync();

            return Ok(msgs);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var me = CurrentUserId();
            var count = await _context.Messages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .CountAsync(m => !m.IsDeleted && m.ReceiverId == me && !m.IsRead);
            return Ok(new { count });
        }

        [HttpGet("unread-by-sender")]
        public async Task<IActionResult> GetUnreadBySender()
        {
            var me = CurrentUserId();
            var grouped = await _context.Messages
                .AsNoTracking()
                .IgnoreQueryFilters()
                .Where(m => !m.IsDeleted && m.ReceiverId == me && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { senderId = g.Key, count = g.Count() })
                .ToListAsync();
            return Ok(grouped);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
        {
            try {
            var me = CurrentUserId();
            var msg = new Message
            {
                SenderId = me,
                ReceiverId = req.ReceiverId,
                Content = req.Content ?? string.Empty,
                AttachmentUrl = req.AttachmentUrl,
                AttachmentName = req.AttachmentName,
                AttachmentType = req.AttachmentType
            };
            _context.Messages.Add(msg);
            await _context.SaveChangesAsync();

            var payload = new {
                msg.Id, msg.SenderId, msg.ReceiverId, msg.Content,
                msg.IsRead, msg.AttachmentUrl, msg.AttachmentName, msg.AttachmentType,
                msg.CreatedDate
            };

            await _hub.Clients.Group($"user_{req.ReceiverId}").SendAsync("ReceiveMessage", payload);
            await _hub.Clients.Group($"user_{me}").SendAsync("ReceiveMessage", payload);

            return Ok(payload);
            } catch (Exception ex) { return StatusCode(500, new { error = ex.Message, inner = ex.InnerException?.Message }); }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Dosya boş");
            if (file.Length > 20 * 1024 * 1024) return BadRequest("Dosya 20MB'dan büyük olamaz");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".doc", ".docx", ".xlsx" };
            if (!allowed.Contains(ext)) return BadRequest("Desteklenmeyen dosya türü");

            var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "chat");
            Directory.CreateDirectory(uploadsDir);

            var fileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var type = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext) ? "image"
                : ext == ".pdf" ? "pdf" : "file";

            return Ok(new { url = $"/uploads/chat/{fileName}", name = file.FileName, type });
        }
    }

    public class SendMessageRequest
    {
        public int ReceiverId { get; set; }
        public string? Content { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentName { get; set; }
        public string? AttachmentType { get; set; }
    }
}
