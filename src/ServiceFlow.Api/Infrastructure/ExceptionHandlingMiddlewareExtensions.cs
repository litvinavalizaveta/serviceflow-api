using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceFlow.Application.Common;
using ServiceFlow.Domain.Common;

namespace ServiceFlow.Api.Infrastructure;

public static class ExceptionHandlingMiddlewareExtensions
{
    public static WebApplication UseServiceFlowExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var problemDetails = CreateProblemDetails(context, exception, app.Environment);

                context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(
                    problemDetails,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: context.RequestAborted);
            });
        });

        return app;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext context,
        Exception? exception,
        IWebHostEnvironment environment)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Resource not found",
                notFound.Message),
            ForbiddenOperationException forbidden => (
                StatusCodes.Status403Forbidden,
                "Forbidden operation",
                forbidden.Message),
            DomainException domain => (
                StatusCodes.Status400BadRequest,
                "Invalid request",
                domain.Message),
            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Concurrency conflict",
                "The resource was modified by another operation."),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected error",
                environment.IsDevelopment()
                    ? exception?.Message ?? "An unexpected error occurred."
                    : "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        problemDetails.Extensions["path"] = context.Request.Path.Value;

        return problemDetails;
    }
}
