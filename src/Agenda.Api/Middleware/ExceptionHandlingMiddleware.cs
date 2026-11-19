using System.Text.Json;
using Agenda.Api.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Agenda.Api.Middleware;

/// <summary>
/// Converte exceções em respostas ProblemDetails (RFC 7807) com o status HTTP adequado.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private const string ProblemContentType = "application/problem+json";

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
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => ToCamelCase(e.PropertyName))
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos.",
                Detail = "Um ou mais campos são inválidos.",
                Instance = context.Request.Path
            };

            await WriteAsync(context, problem);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, CreateProblem(context, StatusCodes.Status404NotFound, "Recurso não encontrado.", ex.Message));
        }
        catch (ConflictException ex)
        {
            await WriteAsync(context, CreateProblem(context, StatusCodes.Status409Conflict, "Conflito.", ex.Message));
        }
        catch (InvalidCredentialsException ex)
        {
            await WriteAsync(context, CreateProblem(context, StatusCodes.Status401Unauthorized, "Não autorizado.", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar {Method} {Path}", context.Request.Method, context.Request.Path);

            await WriteAsync(context, CreateProblem(
                context,
                StatusCodes.Status500InternalServerError,
                "Erro interno.",
                "Ocorreu um erro inesperado. Tente novamente mais tarde."));
        }
    }

    private static ProblemDetails CreateProblem(HttpContext context, int status, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
    }

    private static Task WriteAsync<T>(HttpContext context, T problem) where T : ProblemDetails
    {
        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;

        return context.Response.WriteAsJsonAsync(problem, (JsonSerializerOptions?)null, ProblemContentType);
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}
