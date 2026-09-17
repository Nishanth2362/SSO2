using SSO.Application.Exceptions;
using SSO.Common.Wrapper;
using System.Text.Json;

namespace SSO.WebApplication.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var requestId = context.TraceIdentifier;
                _logger.LogError(error, "Unhandled exception for {Method} {Path}. RequestId: {RequestId}", context.Request.Method, context.Request.Path, requestId);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                if (!WantsJsonResponse(context.Request))
                {
                    throw;
                }

                var response = context.Response;
                response.ContentType = "application/json";
                response.Headers["X-Request-ID"] = requestId;

                var safeMessage = error switch
                {
                    ApiException => error.Message,
                    KeyNotFoundException => error.Message,
                    BadHttpRequestException => "The request could not be completed. Please review the submitted details and try again.",
                    UnauthorizedAccessException => "You do not have permission to complete this action.",
                    _ => "Something went wrong while processing your request. Please try again."
                };

                var responseModel = await Result<string>.FailAsync(safeMessage);
                responseModel.Messages.Add($"Reference ID: {requestId}");

                response.StatusCode = error switch
                {
                    ApiException => StatusCodes.Status400BadRequest,
                    KeyNotFoundException => StatusCodes.Status404NotFound,
                    BadHttpRequestException => StatusCodes.Status400BadRequest,
                    UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                    _ => StatusCodes.Status500InternalServerError
                };

                var result = JsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);
            }
        }

        private static bool WantsJsonResponse(HttpRequest request)
        {
            if (request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments("/v1", StringComparison.OrdinalIgnoreCase) ||
                request.Path.StartsWithSegments("/connect", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (request.Headers.TryGetValue("X-Requested-With", out var requestedWith) &&
                string.Equals(requestedWith.ToString(), "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return request.GetTypedHeaders().Accept?.Any(header =>
                header.MediaType.HasValue &&
                (header.MediaType.Value.Contains("json", StringComparison.OrdinalIgnoreCase) ||
                 header.MediaType.Value.Contains("problem+json", StringComparison.OrdinalIgnoreCase))) == true;
        }
    }
}
