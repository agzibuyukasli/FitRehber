using System.Net;
using Microsoft.AspNetCore.Http;
using DietitianClinic.API.Models.Response;
using DietitianClinic.Business.Exceptions;

namespace DietitianClinic.API.Middleware
{
    /// <summary>
    /// Global Exception Handler Middleware
    /// Tüm exception'ları yakalayıp standart format'ta response döner
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiResponse();

            switch (exception)
            {
                case ValidationException validationEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response = new ApiResponse
                    {
                        Success = false,
                        Message = validationEx.Message,
                        Errors = validationEx.Errors?.SelectMany(x => x.Value).ToList() ?? new List<string>(),
                        ErrorCode = 400
                    };
                    break;

                case NotFoundException notFoundEx:
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    response = new ApiResponse
                    {
                        Success = false,
                        Message = notFoundEx.Message,
                        ErrorCode = 404
                    };
                    break;

                case UnauthorizedException unauthorizedEx:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response = new ApiResponse
                    {
                        Success = false,
                        Message = unauthorizedEx.Message,
                        ErrorCode = 401
                    };
                    break;

                case ForbiddenException forbiddenEx:
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    response = new ApiResponse
                    {
                        Success = false,
                        Message = forbiddenEx.Message,
                        ErrorCode = 403
                    };
                    break;

                case BusinessException businessEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response = new ApiResponse
                    {
                        Success = false,
                        Message = businessEx.Message,
                        ErrorCode = 400
                    };
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response = new ApiResponse
                    {
                        Success = false,
                        Message = exception.Message + (exception.InnerException != null ? " >> " + exception.InnerException.Message : ""),
                        ErrorCode = 500
                    };
                    break;
            }

            return context.Response.WriteAsJsonAsync(response);
        }
    }
}
