using System.Collections.Concurrent;

namespace DietitianClinic.API.Services
{
    /// <summary>
    /// Şifre sıfırlama kodlarını ve doğrulama tokenlarını bellekte yönetir.
    /// Singleton olarak kayıt edilmeli.
    /// </summary>
    public class PasswordResetService
    {
        private readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _codes = new();
        private readonly ConcurrentDictionary<string, (string Token, DateTime Expiry)> _tokens = new();

        private static readonly TimeSpan CodeExpiry  = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan TokenExpiry = TimeSpan.FromMinutes(5);

        /// <summary>6 haneli kod üretir, önceki kodu geçersiz kılar.</summary>
        public string GenerateCode(string email)
        {
            Cleanup();
            var code = new Random().Next(100000, 999999).ToString();
            _codes[Normalize(email)] = (code, DateTime.UtcNow.Add(CodeExpiry));
            return code;
        }

        /// <summary>Kod doğruysa tek kullanımlık reset token döner.</summary>
        public bool VerifyCode(string email, string code, out string resetToken)
        {
            resetToken = string.Empty;
            var key = Normalize(email);
            if (!_codes.TryGetValue(key, out var entry)) return false;
            if (entry.Expiry < DateTime.UtcNow) { _codes.TryRemove(key, out _); return false; }
            if (entry.Code != code.Trim()) return false;
            _codes.TryRemove(key, out _);
            resetToken = IssueToken(key);
            return true;
        }

        /// <summary>Şifre sıfırlama aşamasında token geçerliyse tüketir ve true döner.</summary>
        public bool ConsumeToken(string email, string token)
        {
            var key = Normalize(email);
            if (!_tokens.TryGetValue(key, out var entry)) return false;
            if (entry.Expiry < DateTime.UtcNow) { _tokens.TryRemove(key, out _); return false; }
            if (entry.Token != token) return false;
            _tokens.TryRemove(key, out _);
            return true;
        }

        private string IssueToken(string normalizedEmail)
        {
            var token = Guid.NewGuid().ToString("N");
            _tokens[normalizedEmail] = (token, DateTime.UtcNow.Add(TokenExpiry));
            return token;
        }

        private static string Normalize(string email) => email.Trim().ToLowerInvariant();

        private void Cleanup()
        {
            var now = DateTime.UtcNow;
            foreach (var k in _codes.Keys.ToList())
                if (_codes.TryGetValue(k, out var v) && v.Expiry < now) _codes.TryRemove(k, out _);
            foreach (var k in _tokens.Keys.ToList())
                if (_tokens.TryGetValue(k, out var v) && v.Expiry < now) _tokens.TryRemove(k, out _);
        }
    }
}
