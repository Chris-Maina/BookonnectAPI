using BookonnectCore.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookonnectAPI.Filters;

public sealed class CoreExceptionFilter : IExceptionFilter
{
    private readonly ILogger<CoreExceptionFilter> _logger;

    public CoreExceptionFilter(ILogger<CoreExceptionFilter> logger)
    {
        _logger = logger;
    }

    public static bool CanHandle(Exception exception) =>
        exception is AccountLinkRequiredException or NotFoundException or AlreadyExistsException;

    public void OnException(ExceptionContext context)
    {
        if (!CanHandle(context.Exception))
        {
            return;
        }

        var statusCode = context.Exception is NotFoundException
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status409Conflict;

        _logger.LogWarning(context.Exception, "Core request failed with status {StatusCode}", statusCode);
        context.Result = new ObjectResult(new { Message = context.Exception.Message })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}
