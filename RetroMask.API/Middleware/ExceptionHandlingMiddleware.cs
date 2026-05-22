using System.Net;
using System.Text.Json;
using RetroMask.Application.Common;
using RetroMask.Application.Common.Exceptions;

namespace RetroMask.API.Middleware;

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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            NotFoundException nfe => (HttpStatusCode.NotFound, ApiResponse.Fail(nfe.Message)),
            ValidationException ve => (HttpStatusCode.BadRequest, ApiResponse.Fail("Validation failed", ve.Errors)),
            ForbiddenException fe => (HttpStatusCode.Forbidden, ApiResponse.Fail(fe.Message)),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ApiResponse.Fail("Unauthorized")),
            _ => (HttpStatusCode.InternalServerError, ApiResponse.Fail("An unexpected error occurred."))
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(json);
    }
}
