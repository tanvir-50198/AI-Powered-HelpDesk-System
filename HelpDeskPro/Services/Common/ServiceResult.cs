namespace HelpDeskPro.Services.Common
{
    // The kind of problem a service can report
    public enum ErrorType
    {
        None = 0,
        Validation = 1,    // -> HTTP 400
        NotFound = 2,      // -> HTTP 404
        Forbidden = 3,     // -> HTTP 403
        Conflict = 4,      // -> HTTP 409
        Unavailable = 5    // -> HTTP 503 (for example the AI provider is down)
    }

    // Result of a service call that returns no data
    public class ServiceResult
    {
        public bool Success { get; set; }
        public ErrorType ErrorType { get; set; }
        public string Message { get; set; }

        public static ServiceResult Ok()
        {
            ServiceResult result = new ServiceResult();
            result.Success = true;
            return result;
        }

        public static ServiceResult Fail(ErrorType errorType, string message)
        {
            ServiceResult result = new ServiceResult();
            result.Success = false;
            result.ErrorType = errorType;
            result.Message = message;
            return result;
        }
    }

    // Result of a service call that returns data
    public class ServiceResult<T> : ServiceResult
    {
        public T Data { get; set; }

        public static ServiceResult<T> Ok(T data)
        {
            ServiceResult<T> result = new ServiceResult<T>();
            result.Success = true;
            result.Data = data;
            return result;
        }

        public new static ServiceResult<T> Fail(ErrorType errorType, string message)
        {
            ServiceResult<T> result = new ServiceResult<T>();
            result.Success = false;
            result.ErrorType = errorType;
            result.Message = message;
            return result;
        }
    }
}
