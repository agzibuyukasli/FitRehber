using System;

namespace DietitianClinic.Business.Exceptions
{
    public class BusinessException : Exception
    {
        public string? Code { get; set; }

        public BusinessException(string message, string? code = null) : base(message)
        {
            Code = code ?? GetType().Name;
        }

        public BusinessException(string message, Exception innerException, string? code = null)
            : base(message, innerException)
        {
            Code = code ?? GetType().Name;
        }
    }

    public class NotFoundException : BusinessException
    {
        public NotFoundException(string message, string code = "NOT_FOUND") : base(message, code) { }
    }

    public class ValidationException : BusinessException
    {
        public Dictionary<string, string[]> Errors { get; set; }

        public ValidationException(string message, string code = "VALIDATION_ERROR")
            : base(message, code)
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(Dictionary<string, string[]> errors)
            : base("Doğrulama hatası oluştu.", "VALIDATION_ERROR")
        {
            Errors = errors;
        }
    }

    public class UnauthorizedException : BusinessException
    {
        public UnauthorizedException(string message, string code = "UNAUTHORIZED")
            : base(message, code) { }
    }

    public class ForbiddenException : BusinessException
    {
        public ForbiddenException(string message, string code = "FORBIDDEN")
            : base(message, code) { }
    }
}
