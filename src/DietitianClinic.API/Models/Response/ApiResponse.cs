namespace DietitianClinic.API.Models.Response
{
    /// <summary>
    /// Standart API Response Model
    /// </summary>
    /// <typeparam name="T">Response data tipi</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public int? ErrorCode { get; set; }

        public ApiResponse()
        {
        }

        public ApiResponse(T data, string message = "Başarılı", bool success = true)
        {
            Data = data;
            Message = message;
            Success = success;
        }

        public ApiResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }

        public static ApiResponse<T> SuccessResponse(T data, string message = "Başarılı")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResponse(string message, List<string> errors = null, int? errorCode = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                ErrorCode = errorCode
            };
        }

        public static ApiResponse<T> FailureResponse(string message)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message
            };
        }
    }

    /// <summary>
    /// Non-generic API Response
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public int? ErrorCode { get; set; }

        public static ApiResponse SuccessResponse(string message = "Başarılı")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        public static ApiResponse ErrorResponse(string message, List<string> errors = null, int? errorCode = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Paginated Response Model
    /// </summary>
    /// <typeparam name="T">Item tipi</typeparam>
    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((decimal)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
