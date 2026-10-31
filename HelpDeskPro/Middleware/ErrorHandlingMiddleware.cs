using System.Text.Json;

namespace HelpDeskPro.Middleware
{
    // 1) Gives every request a correlation ID (also sent back in the X-Correlation-Id header)
    // 2) Catches unexpected errors, writes them to the log and shows a SAFE message
    //    (the stack trace is never sent to the browser)
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            string correlationId = Guid.NewGuid().ToString("N");
            context.TraceIdentifier = correlationId;
            context.Response.Headers["X-Correlation-Id"] = correlationId;

            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error. CorrelationId={CorrelationId}", correlationId);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/problem+json";

                    var problem = new
                    {
                        title = "An unexpected error occurred.",
                        status = 500,
                        correlationId = correlationId
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
                }
                else
                {
                    context.Response.Redirect("/Home/Error?id=" + correlationId);
                }
            }
        }
    }
}
