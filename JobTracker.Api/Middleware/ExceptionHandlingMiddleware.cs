using Microsoft.AspNetCore.Mvc;

namespace JobTracker.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Exception after the response started for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                throw;
            }

            var (statusCode, title) = ex switch
            {
                KeyNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
                ArgumentException    => (StatusCodes.Status400BadRequest, "Invalid request"),
                _                    => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("Handled {ExceptionType} for {Method} {Path}: {Message}",
                    ex.GetType().Name, context.Request.Method, context.Request.Path, ex.Message);

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode == StatusCodes.Status500InternalServerError && !_environment.IsDevelopment()
                    ? null
                    : ex.Message,
                Instance = context.Request.Path
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }
    }
}